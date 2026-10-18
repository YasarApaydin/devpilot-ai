using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WebChatAI.Application.Interfaces;
using WebChatAI.Infrastructure.Options;
using WebChatAI.Infrastructure.Services;
using WebChatAI.Infrastructure.Services.AiProviders;

namespace WebChatAI.Infrastructure.DependencyInjection;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<GroqOptions>(configuration.GetSection(GroqOptions.SectionName));
        services.Configure<TavilyOptions>(configuration.GetSection(TavilyOptions.SectionName));
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));
        services.Configure<StrixOptions>(configuration.GetSection(StrixOptions.SectionName));

        services.AddSingleton<ILoginAttemptService, LoginAttemptService>();
        services.AddSingleton<ISecurityScanRunnerService, SecurityScanRunnerService>();

        services.AddScoped<ISsrfProtectionService, SsrfProtectionService>();
        services.AddScoped<ISecurityScanPdfService, SecurityScanPdfService>();
        services.AddScoped<ISecurityScanService, SecurityScanService>();
    
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IEncryptionService, EncryptionService>();
        services.AddScoped<IApiKeyEncryptionService, ApiKeyEncryptionService>();
        services.AddScoped<IDocumentProcessingService, DocumentProcessingService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddSingleton<IDisposableEmailValidator, DisposableEmailValidator>();
        services.AddScoped<IAbuseDetectionService, AbuseDetectionService>();
        services.AddScoped<IQuotaService, QuotaService>();

        services.AddHttpClient<IAiService, GroqAiService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(60);
        });

        services.AddHttpClient<IVoiceService, WhisperVoiceService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(60);
        });

        // BYOK AI Provider Adapters
        services.AddHttpClient<OpenAiProviderAdapter>(client => client.Timeout = TimeSpan.FromSeconds(60));
        services.AddHttpClient<GeminiProviderAdapter>(client => client.Timeout = TimeSpan.FromSeconds(120));
        services.AddHttpClient<GroqProviderAdapter>(client => client.Timeout = TimeSpan.FromSeconds(60));
        services.AddHttpClient<XAiProviderAdapter>(client => client.Timeout = TimeSpan.FromSeconds(60));
        services.AddHttpClient<AnthropicProviderAdapter>(client => client.Timeout = TimeSpan.FromSeconds(60));
        services.AddHttpClient<DeepSeekProviderAdapter>(client => client.Timeout = TimeSpan.FromSeconds(90));
        services.AddHttpClient<OpenRouterProviderAdapter>(client => client.Timeout = TimeSpan.FromSeconds(90));
        services.AddHttpClient<MistralProviderAdapter>(client => client.Timeout = TimeSpan.FromSeconds(60));
        services.AddScoped<SystemDefaultAiProviderAdapter>();

        services.AddScoped<IAiProviderAdapter>(sp => sp.GetRequiredService<SystemDefaultAiProviderAdapter>());
        services.AddScoped<IAiProviderAdapter>(sp => sp.GetRequiredService<OpenAiProviderAdapter>());
        services.AddScoped<IAiProviderAdapter>(sp => sp.GetRequiredService<GeminiProviderAdapter>());
        services.AddScoped<IAiProviderAdapter>(sp => sp.GetRequiredService<GroqProviderAdapter>());
        services.AddScoped<IAiProviderAdapter>(sp => sp.GetRequiredService<XAiProviderAdapter>());
        services.AddScoped<IAiProviderAdapter>(sp => sp.GetRequiredService<AnthropicProviderAdapter>());
        services.AddScoped<IAiProviderAdapter>(sp => sp.GetRequiredService<DeepSeekProviderAdapter>());
        services.AddScoped<IAiProviderAdapter>(sp => sp.GetRequiredService<OpenRouterProviderAdapter>());
        services.AddScoped<IAiProviderAdapter>(sp => sp.GetRequiredService<MistralProviderAdapter>());

        services.AddScoped<IAiProviderRegistry, AiProviderRegistry>();

        return services;
    }
}
