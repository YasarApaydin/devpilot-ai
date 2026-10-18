namespace WebChatAI.Infrastructure.Options;

public class JwtOptions
{
    public const string SectionName = "Jwt";
    public string Issuer { get; set; } = "DevPilotAI";
    public string Audience { get; set; } = "DevPilotAIUsers";
    public string SecretKey { get; set; } = string.Empty;
    public int AccessTokenExpirationMinutes { get; set; } = 60;
    public int RefreshTokenExpirationDays { get; set; } = 7;
}

public class GroqOptions
{
    public const string SectionName = "Groq";
    public string? ApiKey { get; set; }
    public string Model { get; set; } = "openai/gpt-oss-120b";
    public string WhisperModel { get; set; } = "whisper-large-v3-turbo";
    public string BaseUrl { get; set; } = "https://api.groq.com/openai/v1";
}

public class TavilyOptions
{
    public const string SectionName = "Tavily";
    public string? ApiKey { get; set; }
    public string BaseUrl { get; set; } = "https://api.tavily.com";
}

public class EmailOptions
{
    public const string SectionName = "EmailSettings";
    public string? SmtpHost { get; set; }
    public int SmtpPort { get; set; } = 587;
    public string? SmtpUser { get; set; }
    public string? SmtpPassword { get; set; }
    public string FromEmail { get; set; } = "no-reply@devpilot.ai";
    public string FromName { get; set; } = "DevPilot AI";
    public string FrontendBaseUrl { get; set; } = "http://localhost:3000";
}
