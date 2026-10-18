namespace WebChatAI.Application.Interfaces;

public interface ILoginAttemptService
{
    bool IsBlocked(string ipAddress, out TimeSpan? remainingTime);
    void RecordFailedAttempt(string ipAddress, string? email = null);
    void ResetAttempts(string ipAddress, string? email = null);
    int GetFailedAttempts(string ipAddress);
}
