using System.Globalization;
using System.Text;
using WebChatAI.Application.Interfaces;
using WebChatAI.Domain.Entities;

namespace WebChatAI.Infrastructure.Services;

public class SecurityScanPdfService : ISecurityScanPdfService
{
    public byte[] GenerateScanReportPdf(SecurityScan scan, List<SecurityScanFinding> findings, SecurityScanConsent? consent)
    {
        var doc = new SimplePdfDocument();
        doc.AddHeader("DevPilot AI — Pasif Dış Güvenlik ve Konfigürasyon Raporu");

        // 1. Executive Summary Block
        doc.AddSectionTitle("1. YÜRÜTME ÖZETİ (EXECUTIVE SUMMARY)");
        doc.AddKeyValue("Rapor ID", scan.Id.ToString());
        doc.AddKeyValue("Hedef URL", scan.TargetUrl);
        doc.AddKeyValue("Normalize URL", scan.NormalizedTargetUrl);
        doc.AddKeyValue("Tarama Modu", FormatPassiveScanMode(scan.ScanMode));
        doc.AddKeyValue("Tarama Durumu", scan.Status);
        doc.AddKeyValue("Başlangıç Zamanı", scan.StartedAt?.ToString("yyyy-MM-dd HH:mm:ss 'UTC'") ?? scan.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss 'UTC'"));
        doc.AddKeyValue("Tamamlanma Zamanı", scan.CompletedAt?.ToString("yyyy-MM-dd HH:mm:ss 'UTC'") ?? "Devam Ediyor / Tamamlandı");
        doc.AddKeyValue("Seçilen Test Kapsamı", FormatSelectedCategories(scan.SelectedCategories));
        if (!string.IsNullOrWhiteSpace(scan.CustomInstructions))
        {
            doc.AddKeyValue("Özel Talimatlar", scan.CustomInstructions);
        }

        doc.AddSpacer();

        // 2. Findings Severity Breakdown
        doc.AddSectionTitle("2. BULGU DAĞILIMI VE RİSK SEVİYELERİ");
        doc.AddParagraph($"Toplam Tespit Edilen Bulgu Sayısı: {scan.TotalFindingsCount}");
        doc.AddSeverityBadgeRow(scan.CriticalCount, scan.HighCount, scan.MediumCount, scan.LowCount, scan.InfoCount);

        doc.AddSpacer();

        // 3. Detailed Findings
        doc.AddSectionTitle("3. AYRINTILI BULGULAR VE TEKNİK ANALİZ");
        if (findings.Count == 0)
        {
            doc.AddParagraph("Bu tarama kapsamında seçilen test derinliğinde kritik veya doğrulanmış bir güvenlik açığı tespit edilmemiştir.");
        }
        else
        {
            int index = 1;
            foreach (var finding in findings)
            {
                doc.AddSubTitle($"Bulgu #{index++}: {finding.Title} [{finding.Severity.ToUpperInvariant()}]");
                if (!string.IsNullOrWhiteSpace(finding.Cwe))
                {
                    doc.AddKeyValue("CWE Referansı", finding.Cwe);
                }
                if (finding.CvssScore.HasValue)
                {
                    doc.AddKeyValue("CVSS v3.1 Skoru", $"{finding.CvssScore:F1} ({finding.Severity})");
                }
                var confText = string.Equals(finding.Confidence, "high", StringComparison.OrdinalIgnoreCase)
                    ? "HIGH (Gerçek Zamanlı HTTP/TLS/DOM Deterministik Kontrolü)"
                    : $"{(string.IsNullOrWhiteSpace(finding.Confidence) ? "MEDIUM" : finding.Confidence.ToUpperInvariant())} (Yapay Zeka Pasif Konfigürasyon Hipotezi)";
                doc.AddKeyValue("Güven Seviyesi", confText);
                if (!string.IsNullOrWhiteSpace(finding.Endpoint))
                {
                    doc.AddKeyValue("Etkilenen Endpoint", finding.Endpoint);
                }
                doc.AddParagraph($"Açıklama: {finding.Description}");
                if (!string.IsNullOrWhiteSpace(finding.Impact))
                {
                    doc.AddParagraph($"Olası Etki: {finding.Impact}");
                }
                if (!string.IsNullOrWhiteSpace(finding.TechnicalAnalysis))
                {
                    doc.AddParagraph($"Teknik Analiz: {finding.TechnicalAnalysis}");
                }
                if (!string.IsNullOrWhiteSpace(finding.Evidence))
                {
                    doc.AddParagraph($"Kanıt: {finding.Evidence}");
                }
                if (!string.IsNullOrWhiteSpace(finding.Remediation))
                {
                    doc.AddParagraph($"Çözüm Önerisi: {finding.Remediation}");
                }
                doc.AddSeparator();
            }
        }

        // 4. Scope & Limitations
        doc.AddSectionTitle("4. TEST KAPSAMI, SINIRLAMALAR VE HUKUKİ DAYANAK");
        doc.AddParagraph("Bu rapor, DevPilot AI güvenlik motoru aracılığıyla gerçekleştirilen gerçek zamanlı harici kontroller ve yapılandırma değerlendirmesi sonuçlarını içermektedir. Testler, kullanıcı tarafından onaylanan hedef URL sınırlarında, pasif HTTP başlıkları, SSL/TLS yapılandırması, çerez/CORS politikaları ve kamuya açık kaynak analiziyle sınırlı olarak yürütülmüştür; aktif veya yıkıcı sızma testi yapılmamıştır.");

        if (consent != null)
        {
            doc.AddSpacer();
            doc.AddSectionTitle("5. YASAL ONAY VE TEST YETKİ KAYDI (AUDIT RECORD)");
            doc.AddKeyValue("Onay Kimliği", consent.Id.ToString());
            doc.AddKeyValue("Onay Zamanı", consent.ConsentedAt.ToString("yyyy-MM-dd HH:mm:ss 'UTC'"));
            doc.AddKeyValue("Hedef Üzerinde Yetki Onayı", consent.HasOwnershipAuthorization ? "ONAYLANDI (Kullanıcı yetkili olduğunu beyan etti)" : "ONAYSIZ");
            doc.AddKeyValue("Risk ve Şartlar Kabulü", consent.HasRiskAcknowledged ? "KABUL EDİLDİ" : "KABUL EDİLMEDİ");
            doc.AddKeyValue("Bilgilendirme Metni Sürümü", consent.DisclaimerVersion);
            if (!string.IsNullOrWhiteSpace(consent.ClientIp))
            {
                doc.AddKeyValue("İstemci IP Adresi", consent.ClientIp);
            }
        }

        return doc.Build();
    }

    public byte[] GenerateConsentReportPdf(SecurityScan scan, SecurityScanConsent consent)
    {
        var doc = new SimplePdfDocument();
        doc.AddHeader("DevPilot AI — Güvenlik Testi Yasal Yetki ve Onay Kaydı Belgesi");

        doc.AddSectionTitle("1. TEST TALEP EDEN VE HEDEF BİLGİLERİ");
        doc.AddKeyValue("Onay Belge No", consent.Id.ToString());
        doc.AddKeyValue("İlişkili Tarama ID", scan.Id.ToString());
        doc.AddKeyValue("Kullanıcı ID", consent.UserId.ToString());
        doc.AddKeyValue("Test Hedefi", consent.TargetUrl);
        doc.AddKeyValue("Test Derinliği", FormatPassiveScanMode(scan.ScanMode));
        doc.AddKeyValue("Seçilen Test Kategorileri", FormatSelectedCategories(scan.SelectedCategories));
        doc.AddKeyValue("Onay Zamanı (UTC)", consent.ConsentedAt.ToString("yyyy-MM-dd HH:mm:ss 'UTC'"));
        if (!string.IsNullOrWhiteSpace(consent.ClientIp))
        {
            doc.AddKeyValue("Kayıt IP Adresi", consent.ClientIp);
        }
        if (!string.IsNullOrWhiteSpace(consent.UserAgent))
        {
            doc.AddKeyValue("Kullanıcı Arayüzü / Cihaz", consent.UserAgent);
        }

        doc.AddSpacer();
        doc.AddSectionTitle("2. ONAYLANAN HUKUKİ VE TEKNİK TAAHHÜT");
        doc.AddParagraph("Aşağıdaki taahhüt ve onay maddeleri, test işlemi başlatılmadan önce kullanıcı tarafından elektronik ortamda eksiksiz şekilde onaylanmıştır:");
        doc.AddParagraph("1. Test Yetkisi Beyanı: Kullanıcı, test edilecek hedef sistem (URL/domain/IP) üzerinde gerekli yasal mülkiyet, denetim veya yazılı yetkiye sahip olduğunu taahhüt etmiştir.");
        doc.AddParagraph("2. Risk Kabulü: Kullanıcı; pasif HTTP/TLS ve kamuya açık meta veri kontrollerinin hedef sistem üzerinde oluşturabileceği standart HTTP trafiğini bilerek kapsamı onaylamıştır.");
        doc.AddParagraph("3. Bulut Yapay Zeka Veri Aktarımı ve Maskeleme: Yapay zeka destekli pasif güvenlik ve yapılandırma değerlendirmesi kapsamında hedef URL, HTTP durum kodları, özel odak talimatları ve hassas başlıkları (Authorization, Cookie, Set-Cookie, X-Api-Key vb. ile token kalıpları) otomatik maskelenmiş/redakte edilmiş yanıt başlıkları analiz amacıyla bulut AI sağlayıcısına (ör. Groq) veya yerel LLM motoruna aktarılabilir.");
        doc.AddParagraph("4. Sorumluluk Çerçevesi: İşbu belge, DevPilot AI sisteminde başlatılan güvenlik testinin yetkilendirilmiş bir kullanıcı talebi olduğunu ve onay alınmadan hiçbir testin başlatılamayacağını doğrulamaktadır.");

        doc.AddSpacer();
        doc.AddSectionTitle("3. DİJİTAL DOĞRULAMA VE GÜVENLİK İMZASI");
        doc.AddKeyValue("Belge Durumu", "RESMİ SİSTEM LOG KAYDI İLE DOĞRULANMIŞTIR");
        doc.AddKeyValue("Doğrulama Sağlayıcı", "DevPilot AI Security Audit Engine");
        doc.AddKeyValue("Metin Versiyonu", consent.DisclaimerVersion);

        return doc.Build();
    }

    private static string FormatPassiveScanMode(string? mode)
    {
        return (mode ?? "quick").Trim().ToLowerInvariant() switch
        {
            "quick" => "QUICK (Temel Kök HTTP/TLS, Çerez/CORS & Güvenlik Başlığı Kontrolü)",
            "standard" => "STANDARD (Başlıklar, Çerez/CORS, Kamu Meta Dosyaları & AI Konfigürasyon Analizi)",
            "deep" => "DEEP (Genişletilmiş Başlık, İfşa Uç Noktaları, DOM/SRI & AI Mimari Analizi)",
            _ => (mode ?? "QUICK").ToUpperInvariant()
        };
    }

    private static string FormatSelectedCategories(string? rawCategories)
    {
        if (string.IsNullOrWhiteSpace(rawCategories)) return "Varsayılan Kapsam";
        var items = rawCategories.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var labels = items.Select(id => id.ToLowerInvariant() switch
        {
            "security_headers" => "HTTP Güvenlik Başlıkları",
            "ssl_tls" => "SSL/TLS & HSTS Taşıma Güvenliği",
            "cookie_cors_security" => "Çerez & CORS Güvenliği",
            "info_disclosure" => "Hassas Dosya & İfşa Testi",
            "robots_wellknown" => "Robots.txt & Security.txt Analizi",
            "form_client_security" => "İstemci Script (SRI) & Form Güvenliği",
            "recon_discovery" => "Sunucu & Teknoloji Parmak İzi",
            "ai_config_audit" => "Yapay Zeka Konfigürasyon Analizi",
            _ => id
        });
        return string.Join(", ", labels);
    }

    /// <summary>
    /// Lightweight, standards-compliant, zero-dependency PDF 1.4 document builder
    /// </summary>
    private sealed class SimplePdfDocument
    {
        private readonly List<string> _lines = new();

        public void AddHeader(string title)
        {
            _lines.Add($"HEADER:{title}");
        }

        public void AddSectionTitle(string text)
        {
            _lines.Add($"SEC:{text}");
        }

        public void AddSubTitle(string text)
        {
            _lines.Add($"SUB:{text}");
        }

        public void AddKeyValue(string key, string value)
        {
            _lines.Add($"KV:{key}:::{value}");
        }

        public void AddParagraph(string text)
        {
            _lines.Add($"P:{text}");
        }

        public void AddSeverityBadgeRow(int crit, int high, int med, int low, int info)
        {
            _lines.Add($"SEV:{crit}:{high}:{med}:{low}:{info}");
        }

        public void AddSeparator()
        {
            _lines.Add("SEP:");
        }

        public void AddSpacer()
        {
            _lines.Add("SPACE:");
        }

        public byte[] Build()
        {
            // Standard A4: 595 x 842 points
            const double pageWidth = 595.28;
            const double pageHeight = 841.89;
            const double margin = 40.0;
            const double usableWidth = pageWidth - (2 * margin);

            var pages = new List<string>();
            var currentContent = new StringBuilder();

            double cursorY = pageHeight - margin;

            void StartNewPage()
            {
                if (currentContent.Length > 0)
                {
                    pages.Add(currentContent.ToString());
                    currentContent.Clear();
                }
                cursorY = pageHeight - margin;
            }

            void EnsureSpace(double requiredHeight)
            {
                if (cursorY - requiredHeight < margin)
                {
                    StartNewPage();
                }
            }

            foreach (var raw in _lines)
            {
                if (raw.StartsWith("HEADER:"))
                {
                    var title = CleanAscii(raw["HEADER:".Length..]);
                    EnsureSpace(45);
                    // Blue banner
                    currentContent.AppendLine("0.1 0.3 0.7 rg");
                    currentContent.AppendLine($"{margin} {cursorY - 28} {usableWidth} 28 re f");
                    currentContent.AppendLine("1 1 1 rg");
                    currentContent.AppendLine("BT /F2 13 Tf");
                    currentContent.AppendLine($"{margin + 8} {cursorY - 18} Td");
                    currentContent.AppendLine($"({EscapePdf(title)}) Tj ET");
                    cursorY -= 36;
                }
                else if (raw.StartsWith("SEC:"))
                {
                    var title = CleanAscii(raw["SEC:".Length..]);
                    EnsureSpace(28);
                    currentContent.AppendLine("0.15 0.2 0.3 rg");
                    currentContent.AppendLine("BT /F2 11 Tf");
                    currentContent.AppendLine($"{margin} {cursorY - 14} Td");
                    currentContent.AppendLine($"({EscapePdf(title)}) Tj ET");
                    // underline bar
                    currentContent.AppendLine("0.2 0.4 0.8 RG 1 w");
                    currentContent.AppendLine($"{margin} {cursorY - 18} m {margin + usableWidth} {cursorY - 18} l S");
                    cursorY -= 26;
                }
                else if (raw.StartsWith("SUB:"))
                {
                    var title = CleanAscii(raw["SUB:".Length..]);
                    EnsureSpace(20);
                    currentContent.AppendLine("0.85 0.2 0.2 rg");
                    currentContent.AppendLine("BT /F2 10 Tf");
                    currentContent.AppendLine($"{margin + 4} {cursorY - 12} Td");
                    currentContent.AppendLine($"({EscapePdf(title)}) Tj ET");
                    cursorY -= 18;
                }
                else if (raw.StartsWith("KV:"))
                {
                    var content = raw["KV:".Length..];
                    var parts = content.Split(":::", 2);
                    var k = CleanAscii(parts[0]);
                    var v = CleanAscii(parts.Length > 1 ? parts[1] : string.Empty);

                    var wrappedVal = WrapText(v, 75);
                    EnsureSpace(14 * Math.Max(1, wrappedVal.Count));

                    currentContent.AppendLine("0.2 0.2 0.25 rg");
                    currentContent.AppendLine("BT /F2 9 Tf");
                    currentContent.AppendLine($"{margin + 6} {cursorY - 10} Td");
                    currentContent.AppendLine($"({EscapePdf(k)}:) Tj ET");

                    for (int i = 0; i < wrappedVal.Count; i++)
                    {
                        currentContent.AppendLine("0.1 0.1 0.1 rg");
                        currentContent.AppendLine("BT /F1 9 Tf");
                        currentContent.AppendLine($"{margin + 160} {cursorY - 10 - (i * 12)} Td");
                        currentContent.AppendLine($"({EscapePdf(wrappedVal[i])}) Tj ET");
                    }
                    cursorY -= 12 * Math.Max(1, wrappedVal.Count) + 2;
                }
                else if (raw.StartsWith("P:"))
                {
                    var pText = CleanAscii(raw["P:".Length..]);
                    var lines = WrapText(pText, 95);
                    EnsureSpace(12 * lines.Count);

                    currentContent.AppendLine("0.15 0.15 0.15 rg");
                    foreach (var line in lines)
                    {
                        currentContent.AppendLine("BT /F1 8.5 Tf");
                        currentContent.AppendLine($"{margin + 6} {cursorY - 9} Td");
                        currentContent.AppendLine($"({EscapePdf(line)}) Tj ET");
                        cursorY -= 11;
                    }
                    cursorY -= 2;
                }
                else if (raw.StartsWith("SEV:"))
                {
                    var parts = raw["SEV:".Length..].Split(':');
                    int crit = int.Parse(parts[0], CultureInfo.InvariantCulture);
                    int high = int.Parse(parts[1], CultureInfo.InvariantCulture);
                    int med = int.Parse(parts[2], CultureInfo.InvariantCulture);
                    int low = int.Parse(parts[3], CultureInfo.InvariantCulture);
                    int info = int.Parse(parts[4], CultureInfo.InvariantCulture);

                    EnsureSpace(32);
                    double boxWidth = usableWidth / 5.0;

                    DrawBadge(currentContent, margin + (0 * boxWidth), cursorY, boxWidth - 4, "Kritik", crit, "0.85 0.15 0.15");
                    DrawBadge(currentContent, margin + (1 * boxWidth), cursorY, boxWidth - 4, "Yuksek", high, "0.92 0.35 0.05");
                    DrawBadge(currentContent, margin + (2 * boxWidth), cursorY, boxWidth - 4, "Orta", med, "0.85 0.65 0.05");
                    DrawBadge(currentContent, margin + (3 * boxWidth), cursorY, boxWidth - 4, "Dusuk", low, "0.2 0.5 0.85");
                    DrawBadge(currentContent, margin + (4 * boxWidth), cursorY, boxWidth - 4, "Bilgi", info, "0.4 0.45 0.55");

                    cursorY -= 32;
                }
                else if (raw == "SEP:")
                {
                    EnsureSpace(8);
                    currentContent.AppendLine("0.85 0.85 0.9 RG 0.5 w");
                    currentContent.AppendLine($"{margin} {cursorY - 4} m {margin + usableWidth} {cursorY - 4} l S");
                    cursorY -= 10;
                }
                else if (raw == "SPACE:")
                {
                    cursorY -= 8;
                }
            }

            if (currentContent.Length > 0)
            {
                pages.Add(currentContent.ToString());
            }

            if (pages.Count == 0)
            {
                pages.Add("BT /F1 10 Tf 50 750 Td (Rapor olusturuldu) Tj ET");
            }

            return AssemblePdf(pages, pageWidth, pageHeight);
        }

        private static void DrawBadge(StringBuilder sb, double x, double y, double w, string label, int count, string rgb)
        {
            sb.AppendLine($"{rgb} rg");
            sb.AppendLine($"{x} {y - 24} {w} 24 re f");
            sb.AppendLine("1 1 1 rg");
            sb.AppendLine("BT /F2 8 Tf");
            sb.AppendLine($"{x + 6} {y - 10} Td");
            sb.AppendLine($"({label}) Tj ET");
            sb.AppendLine("BT /F2 11 Tf");
            sb.AppendLine($"{x + w - 18} {y - 18} Td");
            sb.AppendLine($"({count}) Tj ET");
        }

        private static List<string> WrapText(string text, int maxChars)
        {
            var result = new List<string>();
            if (string.IsNullOrWhiteSpace(text)) return result;

            var words = text.Split(' ');
            var current = new StringBuilder();

            foreach (var word in words)
            {
                if (current.Length + word.Length + 1 > maxChars)
                {
                    if (current.Length > 0)
                    {
                        result.Add(current.ToString());
                        current.Clear();
                    }
                }
                if (current.Length > 0) current.Append(' ');
                current.Append(word);
            }

            if (current.Length > 0)
            {
                result.Add(current.ToString());
            }

            return result;
        }

        private static string CleanAscii(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            return input
                .Replace('ğ', 'g').Replace('Ğ', 'G')
                .Replace('ş', 's').Replace('Ş', 'S')
                .Replace('ı', 'i').Replace('İ', 'I')
                .Replace('ç', 'c').Replace('Ç', 'C')
                .Replace('ö', 'o').Replace('Ö', 'O')
                .Replace('ü', 'u').Replace('Ü', 'U')
                .Replace('â', 'a').Replace('Â', 'A')
                .Replace('î', 'i').Replace('Î', 'I')
                .Replace('û', 'u').Replace('Û', 'U')
                .Replace('’', '\'').Replace('‘', '\'')
                .Replace('“', '"').Replace('”', '"')
                .Replace('—', '-').Replace('–', '-').Replace('‑', '-').Replace('−', '-')
                .Replace('…', '.').Replace('•', '-')
                .Replace('\r', ' ')
                .Replace('\n', ' ');
        }

        private static string EscapePdf(string input)
        {
            return input
                .Replace("\\", "\\\\")
                .Replace("(", "\\(")
                .Replace(")", "\\)");
        }

        private static byte[] AssemblePdf(List<string> pageStreams, double width, double height)
        {
            var ms = new MemoryStream();
            using var writer = new StreamWriter(ms, Encoding.ASCII, leaveOpen: true);

            var offsets = new List<long>();

            void WriteLine(string line)
            {
                writer.Write(line + "\n");
            }

            WriteLine("%PDF-1.4");

            // Obj 1: Catalog
            offsets.Add(ms.Position);
            WriteLine("1 0 obj");
            WriteLine("<< /Type /Catalog /Pages 2 0 R >>");
            WriteLine("endobj");

            // Obj 2: Pages
            offsets.Add(ms.Position);
            WriteLine("2 0 obj");
            var kids = string.Join(" ", Enumerable.Range(0, pageStreams.Count).Select(i => $"{3 + (i * 2)} 0 R"));
            WriteLine($"<< /Type /Pages /Kids [{kids}] /Count {pageStreams.Count} >>");
            WriteLine("endobj");

            // Fonts: Font 1 = Helvetica, Font 2 = Helvetica-Bold
            int font1ObjId = 3 + (pageStreams.Count * 2);
            int font2ObjId = font1ObjId + 1;

            for (int i = 0; i < pageStreams.Count; i++)
            {
                int pageObjId = 3 + (i * 2);
                int streamObjId = pageObjId + 1;

                // Page obj
                offsets.Add(ms.Position);
                WriteLine($"{pageObjId} 0 obj");
                WriteLine($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {width} {height}] /Contents {streamObjId} 0 R /Resources << /Font << /F1 {font1ObjId} 0 R /F2 {font2ObjId} 0 R >> >> >>");
                WriteLine("endobj");

                // Stream obj
                var streamBytes = Encoding.ASCII.GetBytes(pageStreams[i]);
                offsets.Add(ms.Position);
                WriteLine($"{streamObjId} 0 obj");
                WriteLine($"<< /Length {streamBytes.Length} >>");
                WriteLine("stream");
                writer.Flush();
                ms.Write(streamBytes);
                writer.Write("\nendstream\nendobj\n");
            }

            // Fonts
            offsets.Add(ms.Position);
            WriteLine($"{font1ObjId} 0 obj");
            WriteLine("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");
            WriteLine("endobj");

            offsets.Add(ms.Position);
            WriteLine($"{font2ObjId} 0 obj");
            WriteLine("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold >>");
            WriteLine("endobj");

            long xrefOffset = ms.Position;
            int totalObjects = font2ObjId + 1;

            WriteLine("xref");
            WriteLine($"0 {totalObjects}");
            WriteLine("0000000000 65535 f ");
            foreach (var off in offsets)
            {
                WriteLine($"{off:D10} 00000 n ");
            }

            WriteLine("trailer");
            WriteLine($"<< /Size {totalObjects} /Root 1 0 R >>");
            WriteLine("startxref");
            WriteLine($"{xrefOffset}");
            WriteLine("%%EOF");

            writer.Flush();
            return ms.ToArray();
        }
    }
}
