namespace WebChatAI.Application.Interfaces;

public interface IVoiceService
{
    Task<string> TranscribeAudioAsync(Stream audioStream, string fileName, CancellationToken cancellationToken = default);
}
