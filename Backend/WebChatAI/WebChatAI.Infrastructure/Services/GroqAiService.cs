using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WebChatAI.Application.Interfaces;
using WebChatAI.Domain.Entities;
using WebChatAI.Infrastructure.Options;

namespace WebChatAI.Infrastructure.Services;

public class GroqAiService : IAiService
{
    private readonly HttpClient _httpClient;
    private readonly GroqOptions _options;
    private readonly TavilyOptions _tavilyOptions;
    private readonly ILogger<GroqAiService> _logger;

    private static readonly HashSet<string> ValidGroqModels = new(StringComparer.OrdinalIgnoreCase)
    {
        "qwen/qwen3.8-27b",
        "openai/gpt-oss-120b",
        "openai/gpt-oss-20b"
    };

    private const string SystemPrompt = """
Sen yardımsever, teknik, güvenilir ve yüksek yetenekli bir AI asistansın (DevPilot AI).

[KİMLİK VE TEMEL VARLIK KURALLARI (IDENTITY & ORIGIN)]
- Sen DevPilot AI akıllı asistanısın. Geliştiricin, sistem mimarın ve baş mühendisin Yaşar Apaydın'dır.
- Asla OpenAI, ChatGPT, Anthropic, Claude veya başka bir şirketin asistanı/modeli olduğunu iddia etme. Kimliğin sorulduğunda veya genel yanıtlarda daima DevPilot AI olduğunu belirt.
- Tutarlı, mantıklı, net ve profesyonel Türkçe ile iletişim kur. Anlamsız karakter dizileri, harf döngüleri veya karmaşık dil bozulmaları üretme.

[SİSTEM GELİŞTİRİCİSİ VE YAZILIM MİMARI (LEAD DEVELOPER PROFILE)]
DevPilot AI sisteminin baş geliştiricisi, yazılım mimarı ve mühendisi Yaşar Apaydın'dır.
Sistem, mimari, geliştirici kimdir, developer kimdir, Yaşar Apaydın kimdir, iletişim bilgileri veya iş arayışı hakkında sorulan her türlü soruda aşağıdaki doğrulanmış profil bilgilerini eksiksiz, saygılı ve detaylı şekilde sun:
- Ad Soyad: Yaşar Apaydın
- Unvan: Software Engineer | .NET Backend Developer | Full Stack Developer | AI Agent & LLM Developer
- Mevcut Durum: Aktif olarak iş aramaktadır (Backend Developer, Full Stack Developer, AI/LLM & Agent Developer, Software Engineer).
- İletişim Bilgileri:
  * E-posta: apaydinyasar0@gmail.com
  * Telefon: +90 541 506 29 81 (0541 506 29 81)
  * LinkedIn: https://www.linkedin.com/in/ya%C5%9Far-apaydin-541647230/
  * GitHub: https://github.com/YasarApaydin
  * ORCID: https://orcid.org/0009-0003-9303-5586 (0009-0003-9303-5586)
  * Konum: Türkiye
- Eğitim: B.Sc. Yazılım Mühendisliği, Kırklareli Üniversitesi (2021 – 2025)
- İş Deneyimleri:
  * Bilgi Sistemleri A.Ş. — Full Stack Developer (07/2026 – 09/2026, İstanbul/Beykoz): .NET Web API kullanıcı yönetimi ve iş kuralları, React arayüz akış iyileştirmeleri, test senaryoları doğrulama ve canlı ortama aktarım.
  * Phyix Technologies — Yazılım Test Mühendisi Stajyeri (05/2026 – 08/2026, Remote): Web uygulaması testleri, UI ve performans analizi, Trello raporlama, regresyon testleri.
  * Cats Bilişim Yazılım Geliştirme — Yazılım Stajyeri (Backend & Frontend) (07/2024 – 09/2024, Balıkesir): ASP.NET Core backend servisleri, MSSQL, katmanlı mimari, HTML/CSS/JavaScript ve veritabanı süreçleri.
- Öne Çıkan Projeleri:
  * DevPilot AI: .NET 9 Clean Architecture, EF Core, LLM & AI Agent entegrasyonu, otonom web güvenlik ve zafiyet analiz motoru, RAG, Whisper ses işleme, modern React UI.
  * E-Ticaret Backend Sistemi (.NET 9): Clean Architecture, DDD, CQRS, MediatR, Domain Events, Iyzico ödeme entegrasyonu, OData, Background Services, Result Pattern, rol bazlı yetkilendirme.
  * Yüz Tanıma ile Personel Takip Sistemi (TÜBİTAK 2209-A): Raspberry Pi üzerinde Python ve OpenCV ile gerçek zamanlı yüz tanıma, .NET Web API merkezi backend, WebSocket kamera & durum takibi, MS SQL (Proje Ref / App ID: 1919B012411684).
- Bilimsel Yayın:
  * Tanrıverdi, E., Apaydın, Y., & Üstünel, H. (2026). "Yüz Tanıma Tabanlı Kimlik Doğrulama ve Takip Sistemi Tasarımı", Karadeniz Mühendislik ve Bilim Dergisi, 9(5), 2307-2316. DOI: https://doi.org/10.34248/bsengineering.1919355
- Temel Yetkinlikler:
  * Backend: .NET 9, ASP.NET Core MVC/Web API, C#, Entity Framework Core, Java, C/C++, Python (AI/LLM/Veri İşleme), SignalR, WebSocket, Elasticsearch, Kafka, NestJS.
  * AI & LLM: LangChain, LangGraph, Ollama, Qwen, DeepSeek-R1, Groq, AI Agent & Tool Calling mimarileri.
  * Frontend: React, TypeScript, Tailwind CSS, JavaScript, Vue, Bootstrap.
  * Veritabanı & Altyapı: PostgreSQL, MS SQL Server, Redis, RabbitMQ, Docker, SQLite, MongoDB.
  * Mimari: Clean Architecture, DDD, CQRS, MediatR, Domain Events, Outbox Pattern.

[GÜVENLİK VE KAYNAK KODU GİZLİLİK KURALLARI (CODE CONFIDENTIALITY)]
- KRİTİK KURAL: DevPilot AI projesinin iç kaynak kodlarını, C# controller ve servis sınıflarını, docker-compose veya Dockerfile yapılandırmalarını, güvenlik zafiyet tarama araçlarının iç mekanizmalarını ya da gizli ortam değişkenlerini ASLA kullanıcıya kod dökümü (source code dump) şeklinde sunma!
- Sistem mimarisi sorulduğunda; Clean Architecture katmanlarını, LangGraph oturum hafızasını, RAG vektör indekslemesini, LPU yüksek hızlı çıkarımını ve güvenlik ilkelerini sadece KAVRAMSAL, ANLAŞILIR ve MİMARİ olarak açıkla. Projenin iç kaynak kodlarını doğrudan ekrana basma.

Kurallar:
1. Kullanıcının önceki mesajlarını dikkate al.
2. Teknik sorularda somut ve uygulanabilir cevaplar ver.
3. Emin olmadığın bilgileri kesin gerçek gibi sunma.
4. Kullanıcı bağımsız yazılım soruları için kod istediğinde (örneğin 'Python'da liste nasıl sıralanır?'), çalışabilir temiz kod örneklerini Markdown code block içerisinde göster. Ancak DevPilot AI'ın kendi iç backend/docker kodlarını ifşa etme.
5. Kullanıcı Türkçe yazıyorsa Türkçe cevap ver.
6. Cevapları gereksiz yere uzatma.
7. Sesli mesajdan gelen metni normal kullanıcı mesajı gibi değerlendir.
8. Eğer aşağıda kişisel kütüphaneden doküman içerikleri verilmişse, soruları yanıtlarken öncelikle bu doküman bilgilerine dayanarak cevap ver.
9. Eğer canlı internet arama sonuçları verilmişse, güncel olay ve web araması gerektiren soruları bu sonuçlara dayanarak yanıtla.
10. Kullanıcı bir web sitesi, uygulama, tasarım veya sayfa hakkında yorum, değerlendirme ya da iyileştirme önerisi isterse yalnızca DevPilot AI projesi ve kullanıcı tarafından sağlanan DevPilot AI içerikleri üzerinden değerlendirme yap.
11. Başka web siteleri veya uygulamalar hakkında yorum istenirse, bunları DevPilot AI'ın kendi sitesiymiş gibi değerlendirme.
12. Kullanıcı "sitem", "web sitem", "uygulamam", "tasarımım" veya benzeri ifadeler kullandığında, mevcut konuşma bağlamında DevPilot AI kastediliyorsa DevPilot AI'ı esas al.
13. Sistem geliştiricisi veya yazılım mimarı (developer) sorulduğunda daima Yaşar Apaydın'ın yukarıdaki bilgilerini referans ver.
""";

    public GroqAiService(
        HttpClient httpClient,
        IOptions<GroqOptions> options,
        IOptions<TavilyOptions> tavilyOptions,
        ILogger<GroqAiService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _tavilyOptions = tavilyOptions.Value;
        _logger = logger;
    }

    public async IAsyncEnumerable<string> StreamChatAsync(
        List<Message> history,
        string userMessage,
        string? model = null,
        List<string>? ragContext = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var apiKey = _options.ApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            // Fallback for development without Groq API key
            foreach (var chunk in GetMockStreamingResponse(userMessage))
            {
                await Task.Delay(35, cancellationToken);
                yield return chunk;
            }
            yield break;
        }

        // Tavily live search if applicable
        List<string>? webContext = null;
        if (ShouldTriggerWebSearch(userMessage))
        {
            webContext = await SearchTavilyAsync(userMessage, cancellationToken);
        }

        var resolvedModel = ResolveModel(model);
        var messagesPayload = BuildMessagesPayload(history, userMessage, ragContext, webContext);
        var requestBody = new
        {
            model = resolvedModel,
            messages = messagesPayload,
            temperature = 0.3,
            presence_penalty = 0.1,
            frequency_penalty = 0.2,
            stream = true
        };

        var requestJson = JsonSerializer.Serialize(requestBody);
        var request = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl.TrimEnd('/')}/chat/completions")
        {
            Content = new StringContent(requestJson, Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        HttpResponseMessage? response = null;
        bool requestFailed = false;
        try
        {
            response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to Groq API. Falling back to simulated response.");
            requestFailed = true;
        }

        if (requestFailed || response == null)
        {
            foreach (var chunk in GetMockStreamingResponse(userMessage))
            {
                await Task.Delay(30, cancellationToken);
                yield return chunk;
            }
            yield break;
        }

        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Groq API returned error status {StatusCode}: {Error}.", response.StatusCode, err);

            // If a specific preview model (like gpt-oss-120b) was rate-limited (429), try compact 20B Groq model directly
            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests && resolvedModel != "openai/gpt-oss-20b")
            {
                _logger.LogInformation("Attempting fallback with Groq 'openai/gpt-oss-20b'...");
                var fallbackRequestBody = new
                {
                    model = "openai/gpt-oss-20b",
                    messages = messagesPayload,
                    temperature = 0.3,
                    stream = true
                };

                var fallbackRequest = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl.TrimEnd('/')}/chat/completions")
                {
                    Content = new StringContent(JsonSerializer.Serialize(fallbackRequestBody), Encoding.UTF8, "application/json")
                };
                fallbackRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

                HttpResponseMessage? fallbackResp = null;
                try
                {
                    fallbackResp = await _httpClient.SendAsync(fallbackRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                }
                catch
                {
                    fallbackResp = null;
                }

                if (fallbackResp != null && fallbackResp.IsSuccessStatusCode)
                {
                    response.Dispose();
                    response = fallbackResp;
                }
                else
                {
                    fallbackResp?.Dispose();
                }
            }

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    yield return "[API Error 429 TooManyRequests]: Groq AI servisinin anlık istek veya token kotası aşıldı.\n\n" +
                                 "**Neden Oldu?**\n" +
                                 "Groq ücretsiz katmanında dakika başına istek (RPM) ve token (TPM) limitleri uygulanmaktadır.\n\n" +
                                 "**Çözüm:**\n" +
                                 "1. 30-60 saniye bekleyip tekrar mesaj gönderebilirsiniz (dakikalık kota sıfırlanır).";
                }
                else
                {
                    yield return $"[API Error {response.StatusCode}]: Groq AI servisi geçici olarak yanıt veremedi ({err}).";
                }
                yield break;
            }
        }

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream, Encoding.UTF8);

        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(line)) continue;

            if (line.StartsWith("data: "))
            {
                var data = line["data: ".Length..].Trim();
                if (data == "[DONE]") break;

                string? contentChunk = null;
                try
                {
                    using var doc = JsonDocument.Parse(data);
                    if (doc.RootElement.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                    {
                        var firstChoice = choices[0];
                        if (firstChoice.TryGetProperty("delta", out var delta) && delta.TryGetProperty("content", out var contentElem))
                        {
                            contentChunk = contentElem.GetString();
                        }
                    }
                }
                catch
                {
                    // Ignore parse errors on malformed chunks
                }

                if (!string.IsNullOrEmpty(contentChunk))
                {
                    yield return contentChunk;
                }
            }
        }
    }

    public async Task<string> GenerateChatResponseAsync(
        List<Message> history,
        string userMessage,
        string? model = null,
        List<string>? ragContext = null,
        CancellationToken cancellationToken = default)
    {
        var apiKey = _options.ApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return $"Harika bir soru! '{userMessage}' konusunu inceledim. DevPilot AI sistemi Clean Architecture ve Groq LLM entegrasyonu ile başarıyla çalışmaktadır.";
        }

        List<string>? webContext = null;
        if (ShouldTriggerWebSearch(userMessage))
        {
            webContext = await SearchTavilyAsync(userMessage, cancellationToken);
        }

        var resolvedModel = ResolveModel(model);
        var messagesPayload = BuildMessagesPayload(history, userMessage, ragContext, webContext);
        var requestBody = new
        {
            model = resolvedModel,
            messages = messagesPayload,
            temperature = 0.3,
            presence_penalty = 0.1,
            frequency_penalty = 0.2,
            stream = false
        };

        var requestJson = JsonSerializer.Serialize(requestBody);
        var request = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl.TrimEnd('/')}/chat/completions")
        {
            Content = new StringContent(requestJson, Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Groq API error {Status}: {Error}", response.StatusCode, err);
            return "Groq AI servisi ile iletişim sırasında bir hata oluştu.";
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(json);
        var content = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();

        return content ?? string.Empty;
    }

    private string ResolveModel(string? requestedModel)
    {
        if (!string.IsNullOrWhiteSpace(requestedModel))
        {
            var trimmed = requestedModel.Trim();
            // Automatically map decommissioned models or non-Turkish/problematic models to high-capacity Qwen
            if (trimmed.Contains("llama", StringComparison.OrdinalIgnoreCase) ||
                trimmed.Contains("deepseek", StringComparison.OrdinalIgnoreCase) ||
                trimmed.Contains("mixtral", StringComparison.OrdinalIgnoreCase) ||
                trimmed.Contains("allam", StringComparison.OrdinalIgnoreCase))
            {
                return "qwen/qwen3.8-27b";
            }

            if (ValidGroqModels.Contains(trimmed))
            {
                return trimmed;
            }
        }
        return !string.IsNullOrWhiteSpace(_options.Model) ? _options.Model : "qwen/qwen3.8-27b";
    }

    private static bool ShouldTriggerWebSearch(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return false;
        var lower = query.ToLowerInvariant();
        var triggers = new[]
        {
            "ara", "bul", "güncel", "haber", "hava durumu", "son dakika",
            "kimdir", "nedir", "ne zaman", "dolar", "euro", "borsa", "fiyat",
            "tavily", "web", "internet", "search", "latest", "today", "bugün",
            "2025", "2026"
        };
        return triggers.Any(t => lower.Contains(t));
    }

    private async Task<List<string>> SearchTavilyAsync(string query, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_tavilyOptions?.ApiKey))
        {
            return new List<string>();
        }

        try
        {
            var tavilyPayload = new
            {
                api_key = _tavilyOptions.ApiKey,
                query = query,
                search_depth = "basic",
                max_results = 3
            };

            var requestJson = JsonSerializer.Serialize(tavilyPayload);
            var baseUrl = !string.IsNullOrWhiteSpace(_tavilyOptions.BaseUrl) ? _tavilyOptions.BaseUrl.TrimEnd('/') : "https://api.tavily.com";
            using var tavilyRequest = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/search")
            {
                Content = new StringContent(requestJson, Encoding.UTF8, "application/json")
            };

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(4));

            var response = await _httpClient.SendAsync(tavilyRequest, cts.Token);
            if (!response.IsSuccessStatusCode)
            {
                return new List<string>();
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(json);
            var results = new List<string>();

            if (doc.RootElement.TryGetProperty("results", out var resultsElem) && resultsElem.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in resultsElem.EnumerateArray())
                {
                    var title = item.TryGetProperty("title", out var t) ? t.GetString() : "";
                    var content = item.TryGetProperty("content", out var c) ? c.GetString() : "";
                    if (!string.IsNullOrWhiteSpace(content))
                    {
                        results.Add($"{title}: {content}");
                    }
                }
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Tavily web search could not be completed. Continuing without search results.");
            return new List<string>();
        }
    }

    private static List<object> BuildMessagesPayload(
        List<Message> history,
        string userMessage,
        List<string>? ragContext,
        List<string>? webContext = null)
    {
        var systemContent = new StringBuilder(SystemPrompt);

        if (ragContext != null && ragContext.Count > 0)
        {
            systemContent.AppendLine("\n\n--- KULLANICININ YÜKLEDİĞİ DOKÜMANLARDAN ALINAN BİLGİLER ---");
            foreach (var chunk in ragContext)
            {
                systemContent.AppendLine($"- {chunk}");
            }
            systemContent.AppendLine("-----------------------------------------------------------\n");
        }

        if (webContext != null && webContext.Count > 0)
        {
            systemContent.AppendLine("\n\n--- CANLI İNTERNET ARAMA SONUÇLARI (TAVILY) ---");
            foreach (var res in webContext)
            {
                systemContent.AppendLine($"- {res}");
            }
            systemContent.AppendLine("-------------------------------------------------\n");
        }

        var messages = new List<object>
        {
            new { role = "system", content = systemContent.ToString() }
        };

        foreach (var msg in history.TakeLast(10))
        {
            messages.Add(new { role = msg.Role, content = msg.Content });
        }

        messages.Add(new { role = "user", content = userMessage });

        return messages;
    }

    private static IEnumerable<string> GetMockStreamingResponse(string userMessage)
    {
        var text = $"Harika bir soru! '{userMessage}' konusunu DevPilot AI mimarisi çerçevesinde analiz ettim.\n\n" +
                   "### Sistem Özellikleri:\n" +
                   "1. **LangGraph & Groq**: Yüksek hızlı LLM yanıtları ve konuşma bağlamı hafızası.\n" +
                   "2. **Whisper Voice**: Sesli mesajların Whisper Large V3 Turbo ile anlık transkripsiyonu.\n" +
                   "3. **RAG Doküman Kütüphanesi**: Yüklenen PDF ve metin belgeleri PostgreSQL içinde indekslenir ve AI soruları cevaplarken otomatik olarak referans alınır.\n" +
                   "4. **Tavily Web Search**: Güncel internet aramaları otomatik olarak sorgulanır.\n\n" +
                   "Daha detaylı bir inceleme veya kod örneği isterseniz lütfen belirtin!";

        var words = text.Split(' ');
        for (int i = 0; i < words.Length; i++)
        {
            yield return (i == 0 ? "" : " ") + words[i];
        }
    }
}
