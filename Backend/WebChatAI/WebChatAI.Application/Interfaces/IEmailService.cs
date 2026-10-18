namespace WebChatAI.Application.Interfaces;

public interface IEmailService
{
    Task SendVerificationEmailAsync(string toEmail, string fullName, string code, CancellationToken cancellationToken = default);
    Task SendPasswordResetEmailAsync(string toEmail, string token, string? frontendBaseUrl = null, CancellationToken cancellationToken = default);
    Task SendPasswordChangedNotificationEmailAsync(string toEmail, string fullName, string? frontendBaseUrl = null, CancellationToken cancellationToken = default);
    Task SendWelcomeEmailAsync(string toEmail, string fullName, string? frontendBaseUrl = null, CancellationToken cancellationToken = default);
    Task SendFeedbackEmailAsync(
        string senderEmail,
        string senderName,
        string category,
        string message,
        string? attachmentFileName = null,
        byte[]? attachmentBytes = null,
        string? attachmentContentType = null,
        CancellationToken cancellationToken = default);
    Task SendNewUserRegisteredNotificationAsync(
        string firstName,
        string lastName,
        string email,
        CancellationToken cancellationToken = default);
}
