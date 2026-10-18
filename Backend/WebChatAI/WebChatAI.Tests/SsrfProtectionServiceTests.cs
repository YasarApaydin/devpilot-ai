using WebChatAI.Infrastructure.Services;
using Xunit;

namespace WebChatAI.Tests;

public class SsrfProtectionServiceTests
{
    private readonly SsrfProtectionService _service = new();

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-url")]
    [InlineData("ftp://example.com")]
    [InlineData("file:///etc/passwd")]
    [InlineData("gopher://127.0.0.1:70")]
    public async Task ValidateTargetUrl_InvalidOrDisallowedSchemes_ReturnsInvalid(string inputUrl)
    {
        var result = await _service.ValidateTargetUrlAsync(inputUrl);
        Assert.False(result.IsValid);
        Assert.NotNull(result.ErrorMessage);
    }

    [Theory]
    [InlineData("http://localhost")]
    [InlineData("http://localhost:5000")]
    [InlineData("http://127.0.0.1")]
    [InlineData("http://127.0.0.1:8080")]
    [InlineData("http://127.127.127.127")]
    [InlineData("http://[::1]")]
    [InlineData("http://metadata.google.internal")]
    [InlineData("http://app.local")]
    [InlineData("http://service.internal")]
    public async Task ValidateTargetUrl_LocalhostAndLocalDomains_ReturnsBlocked(string localUrl)
    {
        var result = await _service.ValidateTargetUrlAsync(localUrl);
        Assert.False(result.IsValid);
        Assert.Contains("SSRF Koruması", result.ErrorMessage);
    }

    [Theory]
    [InlineData("http://10.0.0.1")]
    [InlineData("http://10.255.255.254")]
    [InlineData("http://172.16.0.5")]
    [InlineData("http://172.31.255.254")]
    [InlineData("http://192.168.1.1")]
    [InlineData("http://192.168.0.100")]
    [InlineData("http://169.254.169.254")] // Cloud metadata / link-local
    public async Task ValidateTargetUrl_PrivateAndMetadataIPs_ReturnsBlocked(string privateIpUrl)
    {
        var result = await _service.ValidateTargetUrlAsync(privateIpUrl);
        Assert.False(result.IsValid);
        Assert.Contains("SSRF Koruması", result.ErrorMessage);
    }

    [Theory]
    [InlineData("https://example.com")]
    [InlineData("http://example.com")]
    [InlineData("example.com")]
    public async Task ValidateTargetUrl_ValidPublicTarget_ReturnsValid(string publicUrl)
    {
        var result = await _service.ValidateTargetUrlAsync(publicUrl);
        Assert.True(result.IsValid);
        Assert.Null(result.ErrorMessage);
        Assert.NotNull(result.NormalizedUrl);
        Assert.StartsWith("http", result.NormalizedUrl);
    }
}
