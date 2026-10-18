using System.Text;
using WebChatAI.Infrastructure.Services;
using Xunit;

namespace WebChatAI.Tests;

public class DocumentProcessingServiceTests
{
    private readonly DocumentProcessingService _service = new();

    [Fact]
    public async Task ProcessDocumentAsync_WhenLegacyDocFile_ReturnsFailedStatusWithoutFakeContent()
    {
        // Arrange
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("fake legacy binary content"));

        // Act
        var result = await _service.ProcessDocumentAsync(stream, "test_document.doc", "application/msword");

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Failed", result.Status);
        Assert.Empty(result.ExtractedText);
        Assert.Empty(result.Chunks);
        Assert.Equal(0, result.TotalTokens);
        Assert.Contains(".doc", result.Summary, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Icerik indekslendi", result.Summary);
    }

    [Theory]
    [InlineData("image.png", "image/png")]
    [InlineData("photo.jpg", "image/jpeg")]
    [InlineData("graphic.webp", "image/webp")]
    public async Task ProcessDocumentAsync_WhenImageFile_ReturnsFailedStatusWithoutFakeContent(string fileName, string contentType)
    {
        // Arrange
        using var stream = new MemoryStream(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });

        // Act
        var result = await _service.ProcessDocumentAsync(stream, fileName, contentType);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Failed", result.Status);
        Assert.Empty(result.ExtractedText);
        Assert.Empty(result.Chunks);
        Assert.Equal(0, result.TotalTokens);
        Assert.Contains("OCR", result.Summary, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Gorsel dosya kutuphaneye basariyla indekslendi", result.Summary);
    }

    [Fact]
    public async Task ProcessDocumentAsync_WhenCorruptedPdf_ReturnsFailedStatusWithoutFakeContent()
    {
        // Arrange: invalid PDF bytes
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("Not a valid PDF file content"));

        // Act
        var result = await _service.ProcessDocumentAsync(stream, "corrupted.pdf", "application/pdf");

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Failed", result.Status);
        Assert.Empty(result.ExtractedText);
        Assert.Empty(result.Chunks);
        Assert.Equal(0, result.TotalTokens);
        Assert.DoesNotContain("Icerik indekslendi", result.Summary);
    }

    [Fact]
    public async Task ProcessDocumentAsync_WhenCorruptedDocx_ReturnsFailedStatusWithoutFakeContent()
    {
        // Arrange: invalid docx zip
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("Not a valid zip docx"));

        // Act
        var result = await _service.ProcessDocumentAsync(stream, "corrupted.docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document");

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Failed", result.Status);
        Assert.Empty(result.ExtractedText);
        Assert.Empty(result.Chunks);
        Assert.Equal(0, result.TotalTokens);
        Assert.DoesNotContain("Word dosyasi basariyla kutuphaneye kaydedildi", result.Summary);
    }

    [Fact]
    public async Task ProcessDocumentAsync_WhenEmptyTextFile_ReturnsFailedStatus()
    {
        // Arrange
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("   "));

        // Act
        var result = await _service.ProcessDocumentAsync(stream, "empty.txt", "text/plain");

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Failed", result.Status);
        Assert.Empty(result.ExtractedText);
        Assert.Empty(result.Chunks);
        Assert.Equal(0, result.TotalTokens);
    }

    [Fact]
    public async Task ProcessDocumentAsync_WhenValidTextFile_ReturnsProcessedWithChunks()
    {
        // Arrange
        var content = "DevPilot AI is a next-generation developer platform supporting BYOK models and automated code security scanning.";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

        // Act
        var result = await _service.ProcessDocumentAsync(stream, "notes.txt", "text/plain");

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Processed", result.Status);
        Assert.Equal(content, result.ExtractedText);
        Assert.NotEmpty(result.Chunks);
        Assert.True(result.TotalTokens > 0);
    }
}
