using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using WebChatAI.Infrastructure.Services;
using Xunit;

namespace WebChatAI.Tests;

public class ApiKeyEncryptionServiceTests
{
    private readonly ApiKeyEncryptionService _service;

    public ApiKeyEncryptionServiceTests()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Encryption:ApiKeySecret"] = "test_super_secret_encryption_key_at_least_32_chars_long!"
            })
            .Build();

        _service = new ApiKeyEncryptionService(config, NullLogger<ApiKeyEncryptionService>.Instance);
    }

    [Theory]
    [InlineData("sk-proj-1234567890abcdef")]
    [InlineData("AIzaSyB123456789abcdefg")]
    [InlineData("gsk_12345678901234567890")]
    [InlineData("xai-1234567890abcdefghij")]
    [InlineData("sk-ant-api03-123456789abcdef")]
    public void EncryptAndDecrypt_RoundTrip_ReturnsOriginalKey(string plainKey)
    {
        var encrypted = _service.EncryptApiKey(plainKey);
        Assert.NotNull(encrypted);
        Assert.NotEqual(plainKey, encrypted);

        var decrypted = _service.DecryptApiKey(encrypted);
        Assert.Equal(plainKey, decrypted);
    }

    [Fact]
    public void EncryptApiKey_WithEmptyString_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => _service.EncryptApiKey(""));
        Assert.Throws<ArgumentException>(() => _service.EncryptApiKey("   "));
    }

    [Fact]
    public void DecryptApiKey_WithTamperedCiphertext_ThrowsCryptographicException()
    {
        var plainKey = "sk-proj-my-secret-test-key";
        var encrypted = _service.EncryptApiKey(plainKey);

        // Tamper with bytes
        var bytes = Convert.FromBase64String(encrypted);
        bytes[^1] ^= 0xFF; // flip last byte
        var tampered = Convert.ToBase64String(bytes);

        Assert.Throws<CryptographicException>(() => _service.DecryptApiKey(tampered));
    }

    [Theory]
    [InlineData("sk-proj-1234567890abcdef", "openai", "sk-p...cdef")]
    [InlineData("AIzaSyB9876543210zyxwvuts", "gemini", "AIza...vuts")]
    [InlineData("gsk_1234567890abcdef1234", "groq", "gsk_...1234")]
    [InlineData("12345", "test", "••••••••")]
    public void MaskApiKey_ReturnsExpectedMaskedHint(string plainKey, string provider, string expected)
    {
        var masked = _service.MaskApiKey(plainKey, provider);
        Assert.Equal(expected, masked);
    }

    [Fact]
    public void DecryptApiKey_WithLegacyCbcHmacPayload_SuccessfullyDecryptsKey()
    {
        // Recreate legacy CBC+HMAC encrypted payload manually using the same secret
        var secret = "test_super_secret_encryption_key_at_least_32_chars_long!";
        using var sha = SHA256.Create();
        var aesKey = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(secret + ":aes_key"));
        var hmacKey = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(secret + ":hmac_key"));

        var originalKey = "sk-legacy-test-api-key-12345";
        var plainBytes = System.Text.Encoding.UTF8.GetBytes(originalKey);
        var iv = new byte[16];
        RandomNumberGenerator.Fill(iv);

        byte[] cipherBytes;
        using (var aes = Aes.Create())
        {
            aes.Key = aesKey;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            using var encryptor = aes.CreateEncryptor();
            cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
        }

        byte[] hmacBytes;
        using (var hmac = new HMACSHA256(hmacKey))
        {
            var dataToAuth = new byte[iv.Length + cipherBytes.Length];
            Buffer.BlockCopy(iv, 0, dataToAuth, 0, iv.Length);
            Buffer.BlockCopy(cipherBytes, 0, dataToAuth, iv.Length, cipherBytes.Length);
            hmacBytes = hmac.ComputeHash(dataToAuth);
        }

        var legacyPayload = new byte[iv.Length + hmacBytes.Length + cipherBytes.Length];
        Buffer.BlockCopy(iv, 0, legacyPayload, 0, iv.Length);
        Buffer.BlockCopy(hmacBytes, 0, legacyPayload, iv.Length, hmacBytes.Length);
        Buffer.BlockCopy(cipherBytes, 0, legacyPayload, iv.Length + hmacBytes.Length, cipherBytes.Length);

        var legacyBase64 = Convert.ToBase64String(legacyPayload);

        // Decrypt with current service using backward-compatibility fallback
        var decrypted = _service.DecryptApiKey(legacyBase64);
        Assert.Equal(originalKey, decrypted);
    }
}

