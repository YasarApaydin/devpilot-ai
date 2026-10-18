using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using UglyToad.PdfPig;
using WebChatAI.Application.Interfaces;

namespace WebChatAI.Infrastructure.Services;

public class DocumentProcessingService : IDocumentProcessingService
{
    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".webp", ".gif", ".bmp"
    };

    public async Task<DocumentProcessingResult> ProcessDocumentAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        string extractedText = string.Empty;
        var ext = Path.GetExtension(fileName).ToLowerInvariant();

        var isPdf = ext == ".pdf" || contentType.Contains("pdf", StringComparison.OrdinalIgnoreCase);
        var isDocx = ext == ".docx";
        var isDoc = ext == ".doc";
        var isImage = ImageExtensions.Contains(ext) || contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);

        if (isPdf)
        {
            using var ms = new MemoryStream();
            await fileStream.CopyToAsync(ms, cancellationToken);
            ms.Position = 0;

            try
            {
                using var document = PdfDocument.Open(ms);
                var sb = new StringBuilder();
                foreach (var page in document.GetPages())
                {
                    if (!string.IsNullOrWhiteSpace(page.Text))
                    {
                        sb.AppendLine(page.Text);
                    }
                }
                var text = sb.ToString();
                if (string.IsNullOrWhiteSpace(text))
                {
                    return new DocumentProcessingResult(
                        ExtractedText: string.Empty,
                        Summary: "PDF belgesinden metin çıkarılamadı (belge yalnızca taranmış görsel veya boş sayfalardan oluşuyor olabilir).",
                        TotalTokens: 0,
                        Chunks: new List<string>(),
                        Success: false,
                        Status: "Failed",
                        ErrorMessage: "PDF belgesinden metin çıkarılamadı.");
                }
                extractedText = text;
            }
            catch (Exception)
            {
                return new DocumentProcessingResult(
                    ExtractedText: string.Empty,
                    Summary: "PDF belgesi okunamadı veya dosya formatı bozuk.",
                    TotalTokens: 0,
                    Chunks: new List<string>(),
                    Success: false,
                    Status: "Failed",
                    ErrorMessage: "PDF belgesi okunamadı veya dosya bozuk.");
            }
        }
        else if (isDocx)
        {
            try
            {
                using var ms = new MemoryStream();
                await fileStream.CopyToAsync(ms, cancellationToken);
                ms.Position = 0;

                using var zip = new ZipArchive(ms, ZipArchiveMode.Read);
                var entry = zip.GetEntry("word/document.xml");
                if (entry != null)
                {
                    await using var entryStream = entry.Open();
                    var xdoc = await XDocument.LoadAsync(entryStream, LoadOptions.None, cancellationToken);
                    var textSb = new StringBuilder();
                    foreach (var element in xdoc.Descendants())
                    {
                        if (element.Name.LocalName == "t")
                        {
                            textSb.Append(element.Value);
                        }
                        else if (element.Name.LocalName == "p")
                        {
                            textSb.AppendLine();
                        }
                    }
                    var text = textSb.ToString();
                    if (string.IsNullOrWhiteSpace(text))
                    {
                        return new DocumentProcessingResult(
                            ExtractedText: string.Empty,
                            Summary: "Word (.docx) belgesi boş veya metin içeriği içermiyor.",
                            TotalTokens: 0,
                            Chunks: new List<string>(),
                            Success: false,
                            Status: "Failed",
                            ErrorMessage: "Word belgesinden metin çıkarılamadı.");
                    }
                    extractedText = text;
                }
                else
                {
                    return new DocumentProcessingResult(
                        ExtractedText: string.Empty,
                        Summary: "Word (.docx) belgesi yapısı geçersiz (word/document.xml bulunamadı).",
                        TotalTokens: 0,
                        Chunks: new List<string>(),
                        Success: false,
                        Status: "Failed",
                        ErrorMessage: "Word belgesi yapısı geçersiz.");
                }
            }
            catch (Exception)
            {
                return new DocumentProcessingResult(
                    ExtractedText: string.Empty,
                    Summary: "Word (.docx) belgesi okunamadı veya dosya formatı bozuk.",
                    TotalTokens: 0,
                    Chunks: new List<string>(),
                    Success: false,
                    Status: "Failed",
                    ErrorMessage: "Word belgesi okunamadı veya dosya bozuk.");
            }
        }
        else if (isDoc)
        {
            return new DocumentProcessingResult(
                ExtractedText: string.Empty,
                Summary: "Eski Word biçimi (.doc) ikili formatından metin çıkarma desteklenmemektedir. Lütfen belgeyi modern .docx formatına dönüştürüp tekrar yükleyin.",
                TotalTokens: 0,
                Chunks: new List<string>(),
                Success: false,
                Status: "Failed",
                ErrorMessage: "Eski Word (.doc) ikili formatı desteklenmemektedir.");
        }
        else if (isImage)
        {
            return new DocumentProcessingResult(
                ExtractedText: string.Empty,
                Summary: "Görsel dosyalardan metin çıkarma (OCR) şu anda desteklenmemektedir. Metin analizi için PDF, Word (.docx) veya metin dosyaları (.txt, .md, kod dosyaları) yükleyebilirsiniz.",
                TotalTokens: 0,
                Chunks: new List<string>(),
                Success: false,
                Status: "Failed",
                ErrorMessage: "Görsellerden metin çıkarma (OCR) desteklenmemektedir.");
        }
        else
        {
            try
            {
                using var reader = new StreamReader(fileStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
                var text = await reader.ReadToEndAsync(cancellationToken);
                if (string.IsNullOrWhiteSpace(text))
                {
                    return new DocumentProcessingResult(
                        ExtractedText: string.Empty,
                        Summary: "Dosya içeriğinde okunabilir metin bulunamadı veya dosya boş.",
                        TotalTokens: 0,
                        Chunks: new List<string>(),
                        Success: false,
                        Status: "Failed",
                        ErrorMessage: "Dosya içeriği boş.");
                }
                extractedText = text;
            }
            catch (Exception)
            {
                return new DocumentProcessingResult(
                    ExtractedText: string.Empty,
                    Summary: "Dosya içeriği okunamadı veya karakter kodlaması geçersiz.",
                    TotalTokens: 0,
                    Chunks: new List<string>(),
                    Success: false,
                    Status: "Failed",
                    ErrorMessage: "Dosya okuma hatası.");
            }
        }

        // Clean extracted text and strictly strip NUL bytes to prevent Postgres 22021 error
        var cleanedText = extractedText
            .Replace("\0", string.Empty)
            .Replace("\r\n", "\n")
            .Trim();

        if (string.IsNullOrWhiteSpace(cleanedText))
        {
            return new DocumentProcessingResult(
                ExtractedText: string.Empty,
                Summary: "Dosya içeriğinde geçerli metin bulunamadı.",
                TotalTokens: 0,
                Chunks: new List<string>(),
                Success: false,
                Status: "Failed",
                ErrorMessage: "Geçerli metin bulunamadı.");
        }

        // Generate brief summary
        var summary = cleanedText.Length > 250
            ? cleanedText[..250].Trim() + "..."
            : cleanedText;
        summary = summary.Replace("\0", string.Empty);

        // Create semantic chunks (~350 words each with 50 words overlap)
        var words = cleanedText.Split(new[] { ' ', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        var chunks = new List<string>();

        const int chunkSize = 350;
        const int overlap = 50;

        if (words.Length <= chunkSize)
        {
            chunks.Add(cleanedText);
        }
        else
        {
            int index = 0;
            while (index < words.Length)
            {
                var chunkWords = words.Skip(index).Take(chunkSize).ToArray();
                chunks.Add(string.Join(" ", chunkWords).Replace("\0", string.Empty));
                index += (chunkSize - overlap);
            }
        }

        var totalTokens = words.Length;

        return new DocumentProcessingResult(cleanedText, summary, totalTokens, chunks, Success: true, Status: "Processed");
    }
}