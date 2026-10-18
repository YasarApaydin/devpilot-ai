using WebChatAI.Application.DTOs;

namespace WebChatAI.Application.Interfaces;

public interface ISsrfProtectionService
{
    Task<ValidateTargetResponseDto> ValidateTargetUrlAsync(string targetUrl, CancellationToken cancellationToken = default);
}
