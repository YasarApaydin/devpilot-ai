namespace WebChatAI.Application.Interfaces;

public interface IDisposableEmailValidator
{
    bool IsDisposableEmail(string email);
    string? ExtractDomain(string email);
}
