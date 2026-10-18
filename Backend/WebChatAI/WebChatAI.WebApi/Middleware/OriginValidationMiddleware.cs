using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace WebChatAI.WebApi.Middleware;

public class OriginValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly HashSet<string> _allowedOrigins;
    private readonly ILogger<OriginValidationMiddleware> _logger;
    private readonly bool _isDevelopment;

    public OriginValidationMiddleware(
        RequestDelegate next,
        IConfiguration configuration,
        IHostEnvironment env,
        ILogger<OriginValidationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
        _isDevelopment = env.IsDevelopment();

        _allowedOrigins = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var rawOrigins = configuration["Frontend:AllowedOrigins"] ?? configuration["FRONTEND_ALLOWED_ORIGINS"];
        if (!string.IsNullOrWhiteSpace(rawOrigins))
        {
            var parts = rawOrigins.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var p in parts)
            {
                _allowedOrigins.Add(NormalizeOrigin(p));
            }
        }

        var arrayConfigured = configuration.GetSection("Frontend:AllowedOrigins").Get<string[]>();
        if (arrayConfigured != null)
        {
            foreach (var p in arrayConfigured)
            {
                _allowedOrigins.Add(NormalizeOrigin(p));
            }
        }

        var frontendBaseUrl = configuration["EmailSettings:FrontendBaseUrl"] ?? configuration["FRONTEND_BASE_URL"];
        if (!string.IsNullOrWhiteSpace(frontendBaseUrl))
        {
            _allowedOrigins.Add(NormalizeOrigin(frontendBaseUrl));
        }

        // In Development, ensure standard localhost dev origins are available
        if (_isDevelopment)
        {
            _allowedOrigins.Add("http://localhost:5173");
            _allowedOrigins.Add("http://127.0.0.1:5173");
            _allowedOrigins.Add("http://localhost:3000");
            _allowedOrigins.Add("http://127.0.0.1:3000");
            _allowedOrigins.Add("http://127.0.0.1:7170");
        }
        else
        {
            if (_allowedOrigins.Count == 0)
            {
                throw new InvalidOperationException("Fatal Security Error: 'FRONTEND_ALLOWED_ORIGINS' configuration is required in Production environment. Localhost and default origins are strictly forbidden.");
            }
        }
    }

    private static string NormalizeOrigin(string origin)
    {
        if (Uri.TryCreate(origin.Trim(), UriKind.Absolute, out var uri))
        {
            return $"{uri.Scheme.ToLowerInvariant()}://{uri.Authority.ToLowerInvariant()}";
        }
        return origin.Trim().TrimEnd('/').ToLowerInvariant();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        // Skip non-API paths, Swagger/OpenAPI docs, and CORS preflight OPTIONS requests
        if (HttpMethods.IsOptions(context.Request.Method) || 
            !path.StartsWith("/api", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/scalar", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/openapi", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        var originHeader = context.Request.Headers.Origin.ToString();
        var refererHeader = context.Request.Headers.Referer.ToString();

        string? requestOrigin = null;

        if (!string.IsNullOrWhiteSpace(originHeader))
        {
            requestOrigin = NormalizeOrigin(originHeader);
        }
        else if (!string.IsNullOrWhiteSpace(refererHeader) && Uri.TryCreate(refererHeader, UriKind.Absolute, out var refererUri))
        {
            requestOrigin = $"{refererUri.Scheme.ToLowerInvariant()}://{refererUri.Authority.ToLowerInvariant()}";
        }

        var isStateChanging = HttpMethods.IsPost(context.Request.Method) ||
                              HttpMethods.IsPut(context.Request.Method) ||
                              HttpMethods.IsDelete(context.Request.Method) ||
                              HttpMethods.IsPatch(context.Request.Method);

        // When origin is provided, strictly validate that it matches allowed frontend
        if (!string.IsNullOrWhiteSpace(requestOrigin))
        {
            if (!_allowedOrigins.Contains(requestOrigin))
            {
                _logger.LogWarning("Forbidden request from untrusted origin: {Origin} to path {Path}", requestOrigin, path);

                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json; charset=utf-8";

                var errorResponse = new
                {
                    success = false,
                    message = "Yetkisiz erişim: İstek yalnızca izin verilen DevPilot AI frontend kaynağından kabul edilir."
                };

                await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse));
                return;
            }
        }
        else if (!_isDevelopment && isStateChanging)
        {
            // CSRF Defense: In Production, state-changing requests MUST have Origin or Referer header
            _logger.LogWarning("Forbidden state-changing request missing Origin and Referer headers to path {Path}", path);

            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json; charset=utf-8";

            var errorResponse = new
            {
                success = false,
                message = "Yetkisiz erişim: Durum değiştiren isteklerde geçerli kaynak (Origin/Referer) başlığı zorunludur."
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse));
            return;
        }

        await _next(context);
    }
}
