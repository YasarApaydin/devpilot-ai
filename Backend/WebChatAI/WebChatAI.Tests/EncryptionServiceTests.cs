using WebChatAI.Infrastructure.Services;
using Xunit;

namespace WebChatAI.Tests;

public class EncryptionServiceTests
{
    private readonly EncryptionService _service = new();

    [Fact]
    public void HashPassword_GeneratesValidArgon2idFormat()
    {
        var password = "SuperSecurePassword123!@#";
        var hash = _service.HashPassword(password);

        Assert.NotNull(hash);
        Assert.StartsWith("$argon2id$", hash);
        Assert.Contains("$v=19$m=19456,t=2,p=1$", hash);
    }

    [Fact]
    public void VerifyPassword_WithCorrectPassword_ReturnsTrue()
    {
        var password = "SuperSecurePassword123!@#";
        var hash = _service.HashPassword(password);

        var isValid = _service.VerifyPassword(password, hash);
        Assert.True(isValid);
    }

    [Fact]
    public void VerifyPassword_WithWrongPassword_ReturnsFalse()
    {
        var password = "SuperSecurePassword123!@#";
        var hash = _service.HashPassword(password);

        var isValid = _service.VerifyPassword("WrongPassword123!", hash);
        Assert.False(isValid);
    }

    [Fact]
    public void VerifyPassword_WithLegacyBcryptHash_ReturnsTrue()
    {
        // Legacy BCrypt hash generated previously
        var password = "LegacyUserPassword456$";
        var legacyBcryptHash = BCrypt.Net.BCrypt.EnhancedHashPassword(password, workFactor: 11);

        Assert.StartsWith("$2", legacyBcryptHash);

        var isValid = _service.VerifyPassword(password, legacyBcryptHash);
        Assert.True(isValid);

        var isInvalid = _service.VerifyPassword("WrongPassword", legacyBcryptHash);
        Assert.False(isInvalid);
    }
}
