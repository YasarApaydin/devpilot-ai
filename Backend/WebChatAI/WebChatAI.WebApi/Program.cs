using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using WebChatAI.Application.DependencyInjection;
using WebChatAI.Infrastructure.DependencyInjection;
using WebChatAI.Infrastructure.Options;
using WebChatAI.Persistence.DependencyInjection;
using WebChatAI.WebApi.Middleware;

var builder = WebApplication.CreateBuilder(args);

// 1. Dependency Injection layers
builder.Services.AddApplication();
builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHttpContextAccessor();

// Configure Forwarded Headers for reverse proxy / Docker / Azure
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // Do not clear KnownNetworks and KnownProxies blindly to reject untrusted proxy headers
    var knownProxiesConfig = builder.Configuration.GetSection("ReverseProxy:KnownProxies").Get<string[]>();
    if (knownProxiesConfig != null)
    {
        foreach (var p in knownProxiesConfig)
        {
            if (IPAddress.TryParse(p, out var ip)) options.KnownProxies.Add(ip);
        }
    }
});

// 2. JWT & Vault Secret Validation at Startup (Reject missing or static default secrets in Production/Staging)
var hostEnvName = builder.Environment.EnvironmentName;
var isDevelopmentEnv = WebChatAI.Application.Common.AppEnvironment.IsDevelopment(builder.Configuration, hostEnvName);

var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);
var jwtOptions = jwtSection.Get<JwtOptions>() ?? new JwtOptions();
jwtOptions.SecretKey = WebChatAI.Application.Common.AppEnvironment.ValidateAndResolveJwtSecret(builder.Configuration, hostEnvName);
_ = WebChatAI.Application.Common.AppEnvironment.ValidateAndResolveEncryptionSecret(builder.Configuration, hostEnvName);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var authHeader = context.Request.Headers.Authorization.ToString();
            if (!string.IsNullOrWhiteSpace(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return Task.CompletedTask;
            }

            if (context.Request.Cookies.TryGetValue("accessToken", out var cookieToken) && !string.IsNullOrWhiteSpace(cookieToken))
            {
                context.Token = cookieToken;
            }
            return Task.CompletedTask;
        },
        OnTokenValidated = async context =>
        {
            var userIdClaim = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(userIdClaim, out var userId))
            {
                var dbContext = context.HttpContext.RequestServices.GetRequiredService<WebChatAI.Persistence.Context.AppDbContext>();
                var hasActiveSession = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AnyAsync(
                    dbContext.RefreshTokens,
                    r => r.UserId == userId && r.RevokedAt == null && !r.IsDeleted && r.ExpiresAt > DateTime.UtcNow
                );

                if (!hasActiveSession)
                {
                    context.Fail("All active sessions for this account have been terminated.");
                }
            }
        }
    };

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateIssuerSigningKey = true,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero,
        ValidIssuer = jwtOptions.Issuer,
        ValidAudience = jwtOptions.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey))
    };
});

builder.Services.AddAuthorization();

// 3. Strict CORS Policy
var rawOrigins = builder.Configuration["Frontend:AllowedOrigins"] ?? builder.Configuration["FRONTEND_ALLOWED_ORIGINS"];
var configuredOrigins = !string.IsNullOrWhiteSpace(rawOrigins)
    ? rawOrigins.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    : builder.Configuration.GetSection("Frontend:AllowedOrigins").Get<string[]>();

if (configuredOrigins == null || configuredOrigins.Length == 0)
{
    if (!isDevelopmentEnv)
    {
        throw new InvalidOperationException("Fatal Security Error: 'Frontend:AllowedOrigins' or 'FRONTEND_ALLOWED_ORIGINS' must be configured in Production environment.");
    }
    configuredOrigins = new[]
    {
        "http://localhost:5173",
        "http://127.0.0.1:5173",
        "http://localhost:3000",
        "http://127.0.0.1:3000",
        "http://127.0.0.1:7170"
    };
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(configuredOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()
              .WithExposedHeaders("X-Remaining-Credits", "X-Model-Used", "X-Fallback-Reason", "X-Original-Model", "X-Stream-Error", "Retry-After");
    });
});

// 4. Rate Limiting (Global DDoS + Brute-force + Heavy workload protection)
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Global IP Rate Limiter: max 100 requests per minute per IP
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 100,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 5,
            AutoReplenishment = true
        });
    });

    // Auth Policy: max 5 requests per minute (Brute-force protection)
    options.AddPolicy("AuthPolicy", context =>
    {
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        });
    });

    // Heavy Policy: max 20 requests per minute (AI streaming / voice) partitioned by authenticated User ID (with IP fallback)
    options.AddPolicy("HeavyWorkloadPolicy", context =>
    {
        var userId = context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                     ?? context.User?.FindFirst("sub")?.Value;

        var partitionKey = !string.IsNullOrWhiteSpace(userId)
            ? $"user:{userId}"
            : $"ip:{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";

        return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 20,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 2,
            AutoReplenishment = true
        });
    });
});

// 5. Controllers & JSON configuration
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
    });

// 6. OpenAPI & Scalar Documentation
builder.Services.AddOpenApi();

var app = builder.Build();

// Automatic database migration & orphaned scan recovery check on startup
try
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<WebChatAI.Persistence.Context.AppDbContext>();
    try
    {
        if (Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.IsRelational(dbContext.Database))
        {
            Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.Migrate(dbContext.Database);
        }
    }
    catch (Exception migEx)
    {
        if (!isDevelopmentEnv)
        {
            throw;
        }
        var migLogger = app.Services.GetRequiredService<ILogger<Program>>();
        migLogger.LogWarning("Database migration check on startup (development): {Message}", migEx.Message);
    }

    // Recover orphaned Running or Pending security scans to Interrupted (Item 9)
    var orphanedScans = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
        dbContext.SecurityScans.Where(s => s.Status == "Running" || s.Status == "Pending")
    );
    if (orphanedScans.Count > 0)
    {
        foreach (var scan in orphanedScans)
        {
            scan.Status = "Interrupted";
            scan.CompletedAt = DateTime.UtcNow;
            scan.ErrorMessage = "Sunucu yeniden başlatıldığı veya servis kesintiye uğradığı için tarama tamamlanamadı (Interrupted).";
        }
        await dbContext.SaveChangesAsync();
    }

    // Always invalidate active refresh tokens on server/Docker startup (default: true)
    // so stopping and restarting Docker requires a fresh login as requested
    if (app.Configuration.GetValue<bool>("Security:ClearSessionsOnStartup", true))
    {
        var activeTokens = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
            dbContext.RefreshTokens.Where(r => r.RevokedAt == null && !r.IsDeleted)
        );
        if (activeTokens.Count > 0)
        {
            foreach (var token in activeTokens)
            {
                token.RevokedAt = DateTime.UtcNow;
            }
            await dbContext.SaveChangesAsync();
        }
    }
}
catch (Exception ex)
{
    if (!isDevelopmentEnv)
    {
        throw new InvalidOperationException($"Fatal Database Error: Failed to connect, migrate database, or recover scans in Production: {ex.Message}", ex);
    }
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogWarning("Database startup recovery check (development): {Message}", ex.Message);
}

// 7. Middleware Pipeline
app.UseForwardedHeaders();

if (isDevelopmentEnv)
{
    app.MapOpenApi();
    app.MapScalarApiReference();
    app.UseDeveloperExceptionPage();
}

app.UseCors("AllowFrontend");
app.UseMiddleware<OriginValidationMiddleware>();
if (!isDevelopmentEnv && app.Configuration.GetValue<bool>("Security:EnforceHttps", false))
{
    app.UseHttpsRedirection();
}

app.UseMiddleware<ExceptionMiddleware>();
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

// Anti-cache headers on sensitive routes
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value?.ToLower() ?? string.Empty;

    if (path.StartsWith("/api/auth") || path.StartsWith("/api/profile") || path.StartsWith("/api/conversations") || path.StartsWith("/api/security-scans"))
    {
        context.Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
        context.Response.Headers["Pragma"] = "no-cache";
        context.Response.Headers["Expires"] = "0";
    }

    await next();
});

app.MapControllers();

app.Run();