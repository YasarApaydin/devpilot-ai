namespace WebChatAI.Infrastructure.Options;

public class StrixOptions
{
    public const string SectionName = "Strix";

    public string Executable { get; set; } = "embedded";
    public string ProjectPath { get; set; } = "embedded";
    public string WorkingDirectory { get; set; } = "embedded";
    public int TimeoutMinutes { get; set; } = 15;
    public string DefaultScanMode { get; set; } = "quick";
    public string? LlmModel { get; set; } = "qwen2.5-coder:latest";
    public string? LlmApiKey { get; set; } = "ollama";
    public string? LlmApiBase { get; set; } = "http://host.docker.internal:11434";
    public string? OllamaEndpoint { get; set; } = "http://host.docker.internal:11434";
    public bool EnableDiagnosticsFallback { get; set; } = true;
}
