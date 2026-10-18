using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WebChatAI.Domain.Interfaces.Repositories;
using WebChatAI.Persistence.Context;
using WebChatAI.Persistence.Repositories;

namespace WebChatAI.Persistence.DependencyInjection;

public static class PersistenceServiceRegistration
{
    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? configuration.GetConnectionString("PostgreSql")
            ?? "Host=localhost;Port=5432;Database=devpilot_db;Username=postgres;Password=postgres";

        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.EnableRetryOnFailure(3);
            });
        });

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());

        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IEmailVerificationTokenRepository, EmailVerificationTokenRepository>();
        services.AddScoped<IConversationRepository, ConversationRepository>();
        services.AddScoped<IMessageRepository, MessageRepository>();
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IDocumentChunkRepository, DocumentChunkRepository>();
        services.AddScoped<ISecurityScanRepository, SecurityScanRepository>();
        services.AddScoped<ISecurityScanConsentRepository, SecurityScanConsentRepository>();
        services.AddScoped<ISecurityScanFindingRepository, SecurityScanFindingRepository>();
        services.AddScoped<IUserAiKeyRepository, UserAiKeyRepository>();
        services.AddScoped<IUserQuotaRepository, UserQuotaRepository>();
        services.AddScoped<IDeviceRegistrationRepository, DeviceRegistrationRepository>();
        services.AddScoped<IAbuseAuditLogRepository, AbuseAuditLogRepository>();

        return services;
    }
}
