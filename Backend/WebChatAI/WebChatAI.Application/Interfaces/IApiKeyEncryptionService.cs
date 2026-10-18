namespace WebChatAI.Application.Interfaces;

public interface IApiKeyEncryptionService
{
    string EncryptApiKey(string plainTextKey);
    string DecryptApiKey(string cipherText);
    string MaskApiKey(string plainTextKey, string provider);
}
