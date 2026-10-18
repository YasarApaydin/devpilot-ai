using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebChatAI.Application.Features.Library;
using WebChatAI.WebApi.Abstractions;

namespace WebChatAI.WebApi.Controllers;

[Authorize]
public class LibraryController : ApiController
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".doc", ".docx", ".txt", ".md", ".json", ".csv",
        ".cs", ".js", ".ts", ".py", ".html", ".css", ".xml", ".yaml", ".yml", ".sql", ".sh",
        ".png", ".jpg", ".jpeg", ".webp"
    };

    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB limit (Y3 fix)

    public LibraryController(IMediator mediator) : base(mediator)
    {
    }

    [HttpGet]
    public async Task<IActionResult> GetDocuments([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default)
    {
        var result = await Mediator.Send(new GetDocumentsQuery(search, page, pageSize), cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetDocumentDetail(Guid id, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetDocumentDetailQuery(id), cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPost("upload")]
    [RequestSizeLimit(15 * 1024 * 1024)]
    public async Task<IActionResult> UploadDocument([FromForm] IFormFile? file, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { success = false, message = "Lutfen yuklenecek bir dosya seciniz." });
        }

        if (file.Length > MaxFileSizeBytes)
        {
            return BadRequest(new { success = false, message = "Dosya boyutu 10 MB sinirini asamaz." });
        }

        var ext = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(ext) || !AllowedExtensions.Contains(ext))
        {
            return BadRequest(new { success = false, message = "Yalnizca PDF, Word (DOC/DOCX), Kod/Metin (TXT, MD, JSON, CSV, PY, JS vb.) ve Gorsel (PNG, JPG, WEBP) dosyalari desteklenmektedir." });
        }

        await using var stream = file.OpenReadStream();
        var command = new UploadDocumentCommand(stream, file.FileName, file.Length, file.ContentType);
        var result = await Mediator.Send(command, cancellationToken);

        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteDocument(Guid id, CancellationToken cancellationToken)
    {
        var command = new DeleteDocumentCommand(id);
        var result = await Mediator.Send(command, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }
}