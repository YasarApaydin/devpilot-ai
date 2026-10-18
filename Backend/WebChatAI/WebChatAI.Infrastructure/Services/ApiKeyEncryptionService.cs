using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WebChatAI.Application.Interfaces;

namespace WebChatAI.Infrastructure.Services;

public class ApiKeyEncryptionService : IApiKeyEncryptionService
{
    private readonly byte[] _aesKey;
    private readonly byte[] _hmacKey;
    private readonly ILogger<ApiKeyEncryptionService> _logger;

    // Header prefix to identify AES-256-GCM payload version
    private const byte PayloadVersionGcm = 0x01;
    private const int NonceSize = 12; // 96-bit standard GCM nonce
    private const int TagSize = 16;   // 128-bit authentication tag

    public ApiKeyEncryptionService(IConfiguration configuration, ILogger<ApiKeyEncryptionService> logger)
    {
        _logger = logger;

        var secret = WebChatAI.Application.Common.AppEnvironment.ValidateAndResolveEncryptionSecret(configuration);

        // Derive 32-byte (256-bit) encryption key and 32-byte legacy HMAC key using SHA-256
        using var sha = SHA256.Create();
        _aesKey = sha.ComputeHash(Encoding.UTF8.GetBytes(secret + ":aes_key"));
        _hmacKey = sha.ComputeHash(Encoding.UTF8.GetBytes(secret + ":hmac_key"));
    }

    public string EncryptApiKey(string plainTextKey)
    {
        if (string.IsNullOrWhiteSpace(plainTextKey))
            throw new ArgumentException("API key cannot be empty.", nameof(plainTextKey));

        var plainBytes = Encoding.UTF8.GetBytes(plainTextKey.Trim());

        // 12-byte random Nonce
        var nonce = new byte[NonceSize];
        RandomNumberGenerator.Fill(nonce);

        var tag = new byte[TagSize];
        var cipherBytes = new byte[plainBytes.Length];

        using (var aesGcm = new AesGcm(_aesKey, TagSize))
        {
            aesGcm.Encrypt(nonce, plainBytes, cipherBytes, tag);
        }

        // Format: [1 byte Version (0x01)] + [12 bytes Nonce] + [16 bytes Tag] + [N bytes CipherBytes]
        var totalPayload = new byte[1 + NonceSize + TagSize + cipherBytes.Length];
        totalPayload[0] = PayloadVersionGcm;
        Buffer.BlockCopy(nonce, 0, totalPayload, 1, NonceSize);
        Buffer.BlockCopy(tag, 0, totalPayload, 1 + NonceSize, TagSize);
        Buffer.BlockCopy(cipherBytes, 0, totalPayload, 1 + NonceSize + TagSize, cipherBytes.Length);

        return Convert.ToBase64String(totalPayload);
    }

    public string DecryptApiKey(string cipherText)
    {
        if (string.IsNullOrWhiteSpace(cipherText))
            throw new ArgumentException("Ciphertext cannot be empty.", nameof(cipherText));

        byte[] totalPayload;
        try
        {
            totalPayload = Convert.FromBase64String(cipherText);
        }
        catch (FormatException)
        {
            throw new CryptographicException("Invalid ciphertext encoding.");
        }

        // 1. Check if payload matches AES-256-GCM format (version 0x01)
        if (totalPayload.Length >= 1 + NonceSize + TagSize + 1 && totalPayload[0] == PayloadVersionGcm)
        {
            try
            {
                var nonce = new byte[NonceSize];
                Buffer.BlockCopy(totalPayload, 1, nonce, 0, NonceSize);

                var tag = new byte[TagSize];
                Buffer.BlockCopy(totalPayload, 1 + NonceSize, tag, 0, TagSize);

                var cipherLen = totalPayload.Length - (1 + NonceSize + TagSize);
                var cipherBytes = new byte[cipherLen];
                Buffer.BlockCopy(totalPayload, 1 + NonceSize + TagSize, cipherBytes, 0, cipherLen);

                var plainBytes = new byte[cipherLen];
                using (var aesGcm = new AesGcm(_aesKey, TagSize))
                {
                    aesGcm.Decrypt(nonce, cipherBytes, tag, plainBytes);
                }

                return Encoding.UTF8.GetString(plainBytes);
            }
            catch (CryptographicException ex)
            {
                // Integrity check failed on GCM
                throw new CryptographicException("API key ciphertext integrity verification failed (authentication tag mismatch).", ex);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to decrypt GCM payload, attempting legacy fallback.");
            }
        }

        // 2. Backward compatibility fallback: Decrypt legacy AES-CBC + HMAC-SHA256 format
        return DecryptLegacyCbcHmac(totalPayload);
    }

    private string DecryptLegacyCbcHmac(byte[] totalPayload)
    {
        if (totalPayload.Length < 16 + 32 + 1)
            throw new CryptographicException("Ciphertext payload is truncated or invalid.");

        var iv = new byte[16];
        Buffer.BlockCopy(totalPayload, 0, iv, 0, 16);

        var expectedHmac = new byte[32];
        Buffer.BlockCopy(totalPayload, 16, expectedHmac, 0, 32);

        var cipherLen = totalPayload.Length - 48;
        var cipherBytes = new byte[cipherLen];
        Buffer.BlockCopy(totalPayload, 48, cipherBytes, 0, cipherLen);

        // Verify HMAC
        using (var hmac = new HMACSHA256(_hmacKey))
        {
            var dataToAuth = new byte[iv.Length + cipherBytes.Length];
            Buffer.BlockCopy(iv, 0, dataToAuth, 0, iv.Length);
            Buffer.BlockCopy(cipherBytes, 0, dataToAuth, iv.Length, cipherBytes.Length);
            var computedHmac = hmac.ComputeHash(dataToAuth);

            if (!CryptographicOperations.FixedTimeEquals(expectedHmac, computedHmac))
            {
                throw new CryptographicException("API key ciphertext integrity verification failed (HMAC mismatch).");
            }
        }

        // Decrypt
        using var aes = Aes.Create();
        aes.Key = _aesKey;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var decryptor = aes.CreateDecryptor();
        var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
        return Encoding.UTF8.GetString(plainBytes);
    }

    public string MaskApiKey(string plainTextKey, string provider)
    {
        if (string.IsNullOrWhiteSpace(plainTextKey))
            return string.Empty;

        var trimmed = plainTextKey.Trim();
        if (trimmed.Length <= 8)
            return "••••••••";

        // Show max first 4 characters and last 4 characters
        var prefix = trimmed[..Math.Min(4, trimmed.Length)];
        var suffix = trimmed[^Math.Min(4, trimmed.Length)..];

        return $"{prefix}...{suffix}";
    }
}
