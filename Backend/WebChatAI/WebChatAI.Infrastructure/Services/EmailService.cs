using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WebChatAI.Application.Interfaces;
using WebChatAI.Infrastructure.Options;

namespace WebChatAI.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly EmailOptions _options;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<EmailOptions> options, ILogger<EmailService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendVerificationEmailAsync(string toEmail, string fullName, string code, CancellationToken cancellationToken = default)
    {
        var greetingName = string.IsNullOrWhiteSpace(fullName) ? "Kullanıcımız" : fullName.Trim();

        var htmlBody = $"""
<!DOCTYPE html>
<html lang="tr">
<head>
  <meta charset="UTF-8">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <title>DevPilot AI - Doğrulama Kodu</title>
</head>
<body style="margin: 0; padding: 0; background-color: #f1f5f9; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; color: #0f172a; -webkit-font-smoothing: antialiased;">
  <table role="presentation" width="100%" border="0" cellspacing="0" cellpadding="0" style="background-color: #f1f5f9; padding: 48px 16px;">
    <tr>
      <td align="center">
        <!-- Ana Kart -->
        <table role="presentation" width="100%" border="0" cellspacing="0" cellpadding="0" style="max-width: 500px; background-color: #ffffff; border-radius: 16px; border: 1px solid #e2e8f0; box-shadow: 0 10px 25px -5px rgba(15, 23, 42, 0.08), 0 8px 10px -6px rgba(15, 23, 42, 0.04); overflow: hidden;">
          
          <!-- Üst Gradient Vurgusu -->
          <tr>
            <td height="5" style="background: linear-gradient(90deg, #2563eb 0%, #3b82f6 50%, #60a5fa 100%); font-size: 0; line-height: 0;">&nbsp;</td>
          </tr>

          <!-- İçerik Alanı -->
          <tr>
            <td style="padding: 40px 36px 32px 36px;">
              
              <!-- Logo & Badge -->
              <table role="presentation" width="100%" border="0" cellspacing="0" cellpadding="0" style="margin-bottom: 28px;">
                <tr>
                  <td>
                    <div style="font-size: 22px; font-weight: 800; color: #0f172a; letter-spacing: -0.5px;">
                      DevPilot <span style="color: #2563eb;">AI</span>
                    </div>
                  </td>
                  <td align="right">
                    <span style="font-size: 11px; font-weight: 700; color: #1d4ed8; background-color: #eff6ff; padding: 5px 12px; border-radius: 9999px; border: 1px solid #bfdbfe; letter-spacing: 0.5px; text-transform: uppercase;">Doğrulama</span>
                  </td>
                </tr>
              </table>

              <!-- Hitap -->
              <h2 style="margin: 0 0 14px 0; font-size: 17px; font-weight: 700; color: #0f172a; line-height: 1.4;">
                Sayın {greetingName},
              </h2>

              <!-- Açıklama -->
              <p style="margin: 0 0 24px 0; font-size: 14px; color: #475569; line-height: 1.6;">
                DevPilot AI hesabınızı güvenle onaylamak için aşağıdaki 6 haneli tek kullanımlık doğrulama kodunu kullanabilirsiniz:
              </p>

              <!-- Kod Kutusu (Yüksek Kontrast, Estetik, Modern) -->
              <table role="presentation" width="100%" border="0" cellspacing="0" cellpadding="0" style="margin: 0 0 24px 0;">
                <tr>
                  <td style="background-color: #0f172a; border-radius: 12px; padding: 24px 16px; text-align: center; box-shadow: inset 0 2px 4px rgba(0, 0, 0, 0.2);">
                    <div style="font-size: 11px; font-weight: 700; color: #94a3b8; text-transform: uppercase; letter-spacing: 2px; margin-bottom: 8px;">
                      GÜVENLİK KODUNUZ
                    </div>
                    <div style="font-size: 38px; font-weight: 800; color: #ffffff; letter-spacing: 12px; font-family: 'Consolas', 'Courier New', monospace; padding-left: 12px;">
                      {code}
                    </div>
                  </td>
                </tr>
              </table>

              <!-- Geçerlilik Süresi -->
              <table role="presentation" width="100%" border="0" cellspacing="0" cellpadding="0" style="margin-bottom: 24px;">
                <tr>
                  <td align="center">
                    <span style="display: inline-block; font-size: 12px; font-weight: 600; color: #b45309; background-color: #fef3c7; border: 1px solid #fde68a; padding: 6px 14px; border-radius: 8px;">
                      ⏱️ Bu kod <strong>30 dakika</strong> boyunca geçerlidir.
                    </span>
                  </td>
                </tr>
              </table>

              <!-- Güvenlik Uyarısı (Bağlantı vs yoktur) -->
              <div style="background-color: #f8fafc; border-left: 3px solid #cbd5e1; border-radius: 0 8px 8px 0; padding: 12px 14px; margin-bottom: 28px;">
                <p style="margin: 0; font-size: 12px; color: #64748b; line-height: 1.5;">
                  Bu işlem bilginiz dahilinde değilse, lütfen bu e-postayı dikkate almayınız. Güvenliğiniz için doğrulama kodunu kimseyle paylaşmayınız.
                </p>
              </div>

              <!-- İmza -->
              <p style="margin: 0; font-size: 13px; color: #475569; line-height: 1.6;">
                Saygılarımızla,<br/>
                <strong style="color: #0f172a;">DevPilot AI Ekibi</strong>
              </p>

            </td>
          </tr>

          <!-- Alt Bilgi -->
          <tr>
            <td style="background-color: #f8fafc; border-top: 1px solid #f1f5f9; padding: 16px 36px; text-align: center;">
              <p style="margin: 0; font-size: 11px; color: #94a3b8;">
                © 2026 DevPilot AI Workspace. Tüm hakları saklıdır.
              </p>
            </td>
          </tr>

        </table>
      </td>
    </tr>
  </table>
</body>
</html>
""";

        _logger.LogInformation("================== EMAIL VERIFICATION ==================");
        _logger.LogInformation("To: {ToEmail}", toEmail);
        _logger.LogInformation("Name: {Name}", greetingName);
        _logger.LogInformation("Code: [REDACTED]");
        _logger.LogInformation("========================================================");

        await TrySendSmtpAsync(toEmail, "DevPilot AI - Güvenlik Doğrulama Kodu", htmlBody, isHtml: true, cancellationToken);
    }

    public async Task SendPasswordResetEmailAsync(string toEmail, string token, string? frontendBaseUrl = null, CancellationToken cancellationToken = default)
    {
        var baseUrl = !string.IsNullOrWhiteSpace(frontendBaseUrl)
            ? frontendBaseUrl.TrimEnd('/')
            : (!string.IsNullOrWhiteSpace(_options.FrontendBaseUrl) ? _options.FrontendBaseUrl.TrimEnd('/') : "http://localhost:3000");

        var resetLink = $"{baseUrl}/reset-password?token={token}";
        var htmlBody = $"""
<!DOCTYPE html>
<html lang="tr">
<head>
  <meta charset="UTF-8">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <title>DevPilot AI - Şifre Sıfırlama</title>
</head>
<body style="margin: 0; padding: 0; background-color: #f1f5f9; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; color: #0f172a;">
  <table role="presentation" width="100%" border="0" cellspacing="0" cellpadding="0" style="background-color: #f1f5f9; padding: 48px 16px;">
    <tr>
      <td align="center">
        <table role="presentation" width="100%" border="0" cellspacing="0" cellpadding="0" style="max-width: 500px; background-color: #ffffff; border-radius: 16px; border: 1px solid #e2e8f0; box-shadow: 0 10px 25px -5px rgba(15, 23, 42, 0.08); overflow: hidden;">
          <tr>
            <td height="5" style="background: linear-gradient(90deg, #2563eb 0%, #3b82f6 50%, #60a5fa 100%); font-size: 0; line-height: 0;">&nbsp;</td>
          </tr>
          <tr>
            <td style="padding: 40px 36px;">
              <div style="font-size: 22px; font-weight: 800; color: #0f172a; margin-bottom: 24px;">
                DevPilot <span style="color: #2563eb;">AI</span>
              </div>
              <h2 style="margin: 0 0 14px 0; font-size: 17px; font-weight: 700; color: #0f172a;">
                Sayın Kullanıcımız,
              </h2>
              <p style="margin: 0 0 24px 0; font-size: 14px; color: #475569; line-height: 1.6;">
                DevPilot AI hesabınız için şifre sıfırlama talebinde bulunuldu. Şifrenizi yenilemek için aşağıdaki butona tıklayabilirsiniz:
              </p>
              <div style="text-align: center; margin: 0 0 24px 0;">
                <a href="{resetLink}" style="display: inline-block; background-color: #2563eb; color: #ffffff; font-size: 14px; font-weight: 600; text-decoration: none; padding: 13px 32px; border-radius: 8px;">Şifremi Sıfırla</a>
              </div>
              <p style="margin: 0 0 20px 0; font-size: 13px; color: #64748b;">
                ⏱️ Bu bağlantı <strong>2 saat</strong> boyunca geçerlidir.
              </p>
              <p style="margin: 0; font-size: 13px; color: #475569; line-height: 1.6;">
                Saygılarımızla,<br/>
                <strong style="color: #0f172a;">DevPilot AI Ekibi</strong>
              </p>
            </td>
          </tr>
        </table>
      </td>
    </tr>
  </table>
</body>
</html>
""";

        _logger.LogInformation("================== PASSWORD RESET ==================");
        _logger.LogInformation("To: {ToEmail}", toEmail);
        _logger.LogInformation("Reset Link: {Link}", $"{baseUrl}/reset-password?token=[REDACTED]");
        _logger.LogInformation("====================================================");

        await TrySendSmtpAsync(toEmail, "DevPilot AI - Şifre Sıfırlama Bağlantısı", htmlBody, isHtml: true, cancellationToken);
    }

    public async Task SendPasswordChangedNotificationEmailAsync(string toEmail, string fullName, string? frontendBaseUrl = null, CancellationToken cancellationToken = default)
    {
        var greetingName = string.IsNullOrWhiteSpace(fullName) ? "Kullanıcımız" : fullName.Trim();
        var baseUrl = !string.IsNullOrWhiteSpace(frontendBaseUrl)
            ? frontendBaseUrl.TrimEnd('/')
            : (!string.IsNullOrWhiteSpace(_options.FrontendBaseUrl) ? _options.FrontendBaseUrl.TrimEnd('/') : "http://localhost:3000");

        var loginLink = $"{baseUrl}/login";
        var resetLink = $"{baseUrl}/forgot-password";
        var changedAtUtc = DateTime.UtcNow.ToString("dd.MM.yyyy HH:mm:ss 'UTC'");
        const string supportEmail = "apaydinyasar0@gmail.com";

        var htmlBody = $$"""
<!DOCTYPE html>
<html lang="tr">
<head>
  <meta charset="UTF-8">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <title>DevPilot AI - Şifreniz Başarıyla Değiştirildi</title>
  <style>
    @media only screen and (max-width: 600px) {
      .email-container { width: 100% !important; border-radius: 0 !important; }
      .content-padding { padding: 28px 20px !important; }
      .button-cta { display: block !important; width: 100% !important; box-sizing: border-box !important; text-align: center !important; }
    }
  </style>
</head>
<body style="margin: 0; padding: 0; background-color: #f1f5f9; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; color: #0f172a; -webkit-font-smoothing: antialiased;">
  <table role="presentation" width="100%" border="0" cellspacing="0" cellpadding="0" style="background-color: #f1f5f9; padding: 36px 12px;">
    <tr>
      <td align="center">
        <table role="presentation" class="email-container" width="100%" border="0" cellspacing="0" cellpadding="0" style="max-width: 540px; background-color: #ffffff; border-radius: 16px; border: 1px solid #e2e8f0; box-shadow: 0 10px 25px -5px rgba(15, 23, 42, 0.08); overflow: hidden;">
          
          <!-- Üst Gradient Vurgusu -->
          <tr>
            <td height="5" style="background: linear-gradient(90deg, #10b981 0%, #2563eb 50%, #6366f1 100%); font-size: 0; line-height: 0;">&nbsp;</td>
          </tr>

          <!-- İçerik Alanı -->
          <tr>
            <td class="content-padding" style="padding: 36px 32px 28px 32px;">
              
              <!-- Logo & Badge -->
              <table role="presentation" width="100%" border="0" cellspacing="0" cellpadding="0" style="margin-bottom: 24px;">
                <tr>
                  <td>
                    <div style="font-size: 22px; font-weight: 800; color: #0f172a; letter-spacing: -0.5px;">
                      DevPilot <span style="color: #2563eb;">AI</span>
                    </div>
                  </td>
                  <td align="right">
                    <span style="font-size: 11px; font-weight: 700; color: #065f46; background-color: #d1fae5; padding: 5px 12px; border-radius: 9999px; border: 1px solid #a7f3d0; letter-spacing: 0.5px; text-transform: uppercase;">Güvenlik Bildirimi</span>
                  </td>
                </tr>
              </table>

              <!-- Başlık İkonu & Hitap -->
              <div style="background-color: #ecfdf5; border-radius: 12px; padding: 16px; border: 1px solid #d1fae5; margin-bottom: 24px;">
                <table role="presentation" width="100%" border="0" cellspacing="0" cellpadding="0">
                  <tr>
                    <td width="36" valign="top">
                      <div style="width: 32px; height: 32px; border-radius: 8px; background-color: #10b981; color: #ffffff; font-size: 18px; line-height: 32px; text-align: center; font-weight: bold;">
                        ✓
                      </div>
                    </td>
                    <td style="padding-left: 12px;">
                      <h2 style="margin: 0; font-size: 16px; font-weight: 700; color: #065f46;">
                        Şifreniz Başarıyla Değiştirildi
                      </h2>
                      <p style="margin: 4px 0 0 0; font-size: 13px; color: #047857;">
                        DevPilot AI hesabınızın şifresi güncellendi.
                      </p>
                    </td>
                  </tr>
                </table>
              </div>

              <h3 style="margin: 0 0 12px 0; font-size: 15px; font-weight: 700; color: #0f172a;">
                Sayın {{WebUtility.HtmlEncode(greetingName)}},
              </h3>

              <p style="margin: 0 0 20px 0; font-size: 14px; color: #475569; line-height: 1.6;">
                DevPilot AI kullanıcı hesabınızın giriş şifresi başarıyla değiştirilmiştir. Hesabınızın güvenliğini en üst düzeyde tutmak amacıyla, diğer cihazlardaki açık oturumlar ve aktif oturum anahtarları otomatik olarak sonlandırılmıştır.
              </p>

              <!-- İşlem Detay Tablosu -->
              <table role="presentation" width="100%" border="0" cellspacing="0" cellpadding="0" style="background-color: #f8fafc; border: 1px solid #e2e8f0; border-radius: 10px; margin-bottom: 24px; padding: 14px 18px;">
                <tr>
                  <td style="font-size: 12px; font-weight: 600; color: #64748b; padding: 5px 0; width: 130px;">İşlem Türü:</td>
                  <td style="font-size: 13px; font-weight: 700; color: #0f172a; padding: 5px 0;">Şifre Değişikliği / Yenileme</td>
                </tr>
                <tr>
                  <td style="font-size: 12px; font-weight: 600; color: #64748b; padding: 5px 0;">İşlem Tarihi:</td>
                  <td style="font-size: 13px; font-weight: 600; color: #334155; padding: 5px 0;">{{changedAtUtc}}</td>
                </tr>
                <tr>
                  <td style="font-size: 12px; font-weight: 600; color: #64748b; padding: 5px 0;">İşlem Durumu:</td>
                  <td style="font-size: 13px; font-weight: 700; color: #059669; padding: 5px 0;">Başarılı & Oturumlar Yenilendi</td>
                </tr>
              </table>

              <!-- Buton -->
              <div style="text-align: center; margin: 0 0 24px 0;">
                <a href="{{loginLink}}" class="button-cta" style="display: inline-block; background-color: #2563eb; color: #ffffff; font-size: 14px; font-weight: 600; text-decoration: none; padding: 12px 28px; border-radius: 8px; box-shadow: 0 2px 4px rgba(37, 99, 235, 0.2);">
                  Hesabıma Giriş Yap →
                </a>
              </div>

              <!-- Güvenlik Uyarısı & Destek İletişim -->
              <div style="background-color: #fffbeb; border-left: 4px solid #f59e0b; border-radius: 0 8px 8px 0; padding: 14px 16px; margin-bottom: 24px;">
                <div style="font-size: 13px; font-weight: 700; color: #92400e; margin-bottom: 4px;">
                  ⚠️ Bu işlemi siz yapmadıysanız:
                </div>
                <p style="margin: 0; font-size: 12.5px; color: #b45309; line-height: 1.55;">
                  Hesabınız yetkisiz erişim riski altında olabilir. Lütfen derhal <a href="{{resetLink}}" style="color: #b45309; font-weight: 700; text-decoration: underline;">Şifre Sıfırlama</a> adımını uygulayın ve doğrudan Sistem Mimarı & Teknik Destek hattımızla iletişime geçin:
                  <br/>
                  📧 Destek : <a href="mailto:{{supportEmail}}" style="color: #1d4ed8; font-weight: 700; text-decoration: underline;">{{supportEmail}}</a>
                </p>
              </div>

              <!-- İmza -->
              <p style="margin: 0; font-size: 13px; color: #475569; line-height: 1.6;">
                Güvenli günler dileriz,<br/>
                <strong style="color: #0f172a;">DevPilot AI Güvenlik & Destek Ekibi</strong>
              </p>

            </td>
          </tr>

          <!-- Alt Bilgi -->
          <tr>
            <td style="background-color: #f8fafc; border-top: 1px solid #f1f5f9; padding: 16px 32px; text-align: center;">
              <p style="margin: 0 0 4px 0; font-size: 11px; color: #94a3b8;">
                © 2026 DevPilot AI Workspace • Geliştirici & Sistem Mimarı: Yaşar Apaydın
              </p>
              <p style="margin: 0; font-size: 11px; color: #64748b;">
                Destek: <a href="mailto:{{supportEmail}}" style="color: #2563eb; text-decoration: none;">{{supportEmail}}</a> • <a href="{{baseUrl}}" style="color: #2563eb; text-decoration: none;">{{baseUrl}}</a>
              </p>
            </td>
          </tr>

        </table>
      </td>
    </tr>
  </table>
</body>
</html>
""";

        _logger.LogInformation("================== PASSWORD CHANGED NOTIFICATION ==================");
        _logger.LogInformation("To: {ToEmail}", toEmail);
        _logger.LogInformation("Name: {Name}", greetingName);
        _logger.LogInformation("===================================================================");

        await TrySendSmtpAsync(toEmail, "DevPilot AI - Şifreniz Başarıyla Değiştirildi 🔒", htmlBody, isHtml: true, cancellationToken);
    }

    public async Task SendWelcomeEmailAsync(string toEmail, string fullName, string? frontendBaseUrl = null, CancellationToken cancellationToken = default)
    {
        var greetingName = string.IsNullOrWhiteSpace(fullName) ? "Değerli Kullanıcımız" : fullName.Trim();
        var baseUrl = !string.IsNullOrWhiteSpace(frontendBaseUrl)
            ? frontendBaseUrl.TrimEnd('/')
            : (!string.IsNullOrWhiteSpace(_options.FrontendBaseUrl) ? _options.FrontendBaseUrl.TrimEnd('/') : "http://localhost:3000");

        var appLink = $"{baseUrl}/app";
        const string founderEmail = "apaydinyasar0@gmail.com";

        var htmlBody = $$"""
<!DOCTYPE html>
<html lang="tr">
<head>
  <meta charset="UTF-8">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <title>DevPilot AI'a Hoş Geldiniz!</title>
  <style>
    body {
      margin: 0;
      padding: 0;
      background-color: #0b0f17;
      font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif;
      color: #0f172a;
      -webkit-font-smoothing: antialiased;
    }
    .email-wrapper {
      width: 100%;
      background-color: #0b0f17;
      padding: 32px 12px;
    }
    .container {
      max-width: 600px;
      margin: 0 auto;
      background-color: #ffffff;
      border-radius: 20px;
      overflow: hidden;
      box-shadow: 0 20px 40px rgba(0, 0, 0, 0.4);
    }
    @media only screen and (max-width: 600px) {
      .email-wrapper { padding: 12px 6px !important; }
      .container { width: 100% !important; border-radius: 12px !important; }
      .hero-padding { padding: 32px 20px 24px 20px !important; }
      .content-padding { padding: 24px 18px !important; }
      .step-col { display: block !important; width: 100% !important; padding: 0 0 10px 0 !important; }
      .cta-button { display: block !important; width: 100% !important; box-sizing: border-box !important; text-align: center !important; font-size: 15px !important; padding: 14px 20px !important; }
    }
  </style>
</head>
<body style="margin: 0; padding: 0; background-color: #0b0f17; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; color: #0f172a; -webkit-font-smoothing: antialiased;">
  <table role="presentation" class="email-wrapper" width="100%" border="0" cellspacing="0" cellpadding="0" style="background-color: #0b0f17; padding: 36px 12px;">
    <tr>
      <td align="center">
        <!-- Ana Kart -->
        <table role="presentation" class="container" width="100%" border="0" cellspacing="0" cellpadding="0" style="max-width: 600px; background-color: #ffffff; border-radius: 20px; overflow: hidden; box-shadow: 0 20px 40px rgba(0, 0, 0, 0.35);">
          
          <!-- Üst Gradient Header Bar -->
          <tr>
            <td height="6" style="background: linear-gradient(90deg, #2563eb 0%, #7c3aed 50%, #06b6d4 100%); font-size: 0; line-height: 0;">&nbsp;</td>
          </tr>

          <!-- Hero Alanı (Koyu Şık Tasarım) -->
          <tr>
            <td class="hero-padding" style="background-color: #0f172a; padding: 40px 36px 32px 36px; text-align: center;">
              
              <!-- Badge -->
              <div style="display: inline-block; margin-bottom: 16px;">
                <span style="font-size: 11px; font-weight: 700; color: #60a5fa; background-color: rgba(37, 99, 235, 0.2); border: 1px solid rgba(96, 165, 250, 0.4); padding: 6px 16px; border-radius: 9999px; letter-spacing: 1px; text-transform: uppercase;">
                  🚀 ARAMIZA HOŞ GELDİNİZ
                </span>
              </div>

              <!-- Logo & Başlık -->
              <h1 style="margin: 0 0 10px 0; font-size: 28px; font-weight: 800; color: #ffffff; letter-spacing: -0.5px;">
                DevPilot <span style="color: #3b82f6;">AI</span>
              </h1>
              
              <p style="margin: 0 auto; max-width: 460px; font-size: 15px; color: #94a3b8; line-height: 1.6;">
                Yapay zeka modelleri, derinlemesine doküman analizi ve web güvenlik taramasını bir araya getiren yeni nesil çalışma alanınıza hoş geldiniz.
              </p>
            </td>
          </tr>

          <!-- Ana İçerik Alanı -->
          <tr>
            <td class="content-padding" style="padding: 36px 36px 28px 36px;">
              
              <!-- Kişisel Hitap -->
              <h2 style="margin: 0 0 12px 0; font-size: 18px; font-weight: 700; color: #0f172a;">
                Merhaba {{WebUtility.HtmlEncode(greetingName)}} 👋
              </h2>

              <p style="margin: 0 0 24px 0; font-size: 14px; color: #475569; line-height: 1.65;">
                DevPilot AI hesabınız başarıyla oluşturuldu. Artık tek bir platform üzerinden OpenAI, Anthropic Claude, xAI Grok, Google Gemini ve Groq LPU gibi en gelişmiş yapay zeka modelleriyle çalışabilir, dosyalarınızı derinlemesine analiz edebilir ve web siteleriniz için kapsamlı güvenlik zafiyet testleri gerçekleştirebilirsiniz.
              </p>

              <!-- Ne Yapıyoruz? Yetenekler Bölümü -->
              <div style="margin-bottom: 28px;">
                <h3 style="margin: 0 0 14px 0; font-size: 14px; font-weight: 700; color: #1e293b; text-transform: uppercase; letter-spacing: 0.5px;">
                  ✨ DevPilot AI ile Neler Yapabilirsiniz?
                </h3>

                <!-- 1. Özellik: Çoklu Model -->
                <div style="background-color: #f8fafc; border: 1px solid #e2e8f0; border-radius: 12px; padding: 16px; margin-bottom: 12px;">
                  <table role="presentation" width="100%" border="0" cellspacing="0" cellpadding="0">
                    <tr>
                      <td width="36" valign="top">
                        <div style="width: 32px; height: 32px; border-radius: 8px; background-color: #eff6ff; color: #2563eb; font-size: 18px; line-height: 32px; text-align: center;">
                          🤖
                        </div>
                      </td>
                      <td style="padding-left: 12px;">
                        <div style="font-size: 14px; font-weight: 700; color: #0f172a; margin-bottom: 3px;">
                          Dünyanın En Güçlü AI Modelleri Bir Arada
                        </div>
                        <div style="font-size: 12.5px; color: #64748b; line-height: 1.5;">
                          OpenAI GPT-4o, o1, o3-mini, Anthropic Claude 3.5 Sonnet, xAI Grok 2, Google Gemini 2.0 Flash ve Groq LPU modellerini tek tıkla kullanın. İster sistem kredisini, ister kendi API anahtarınızı (BYOK) kullanın.
                        </div>
                      </td>
                    </tr>
                  </table>
                </div>

                <!-- 2. Özellik: Akıllı Doküman Analizi -->
                <div style="background-color: #f8fafc; border: 1px solid #e2e8f0; border-radius: 12px; padding: 16px; margin-bottom: 12px;">
                  <table role="presentation" width="100%" border="0" cellspacing="0" cellpadding="0">
                    <tr>
                      <td width="36" valign="top">
                        <div style="width: 32px; height: 32px; border-radius: 8px; background-color: #f5f3ff; color: #7c3aed; font-size: 18px; line-height: 32px; text-align: center;">
                          📑
                        </div>
                      </td>
                      <td style="padding-left: 12px;">
                        <div style="font-size: 14px; font-weight: 700; color: #0f172a; margin-bottom: 3px;">
                          Derinlemesine Doküman & Kod Analizi
                        </div>
                        <div style="font-size: 12.5px; color: #64748b; line-height: 1.5;">
                          PDF, Word (DOCX), metin ve kaynak kodlarınızı yükleyin. Yapay zeka dökümanı anında özetler, kritik maddeleri çıkarır ve dosya içeriğine dair sorularınıza referanslarla yanıt verir.
                        </div>
                      </td>
                    </tr>
                  </table>
                </div>

                <!-- 3. Özellik: Web Güvenlik Tarayıcısı -->
                <div style="background-color: #f8fafc; border: 1px solid #e2e8f0; border-radius: 12px; padding: 16px; margin-bottom: 12px;">
                  <table role="presentation" width="100%" border="0" cellspacing="0" cellpadding="0">
                    <tr>
                      <td width="36" valign="top">
                        <div style="width: 32px; height: 32px; border-radius: 8px; background-color: #ecfdf5; color: #059669; font-size: 18px; line-height: 32px; text-align: center;">
                          🛡️
                        </div>
                      </td>
                      <td style="padding-left: 12px;">
                        <div style="font-size: 14px; font-weight: 700; color: #0f172a; margin-bottom: 3px;">
                          Web Güvenlik & Zafiyet Tarayıcısı (DAST)
                        </div>
                        <div style="font-size: 12.5px; color: #64748b; line-height: 1.5;">
                          Web siteleriniz ve API uç noktalarınız için otomatik güvenlik taramaları başlatın. Güvenlik açıklarını, başlık eksikliklerini ve SSL risklerini keşfedip profesyonel PDF raporları indirin.
                        </div>
                      </td>
                    </tr>
                  </table>
                </div>

                <!-- 4. Özellik: BYOK & Üst Düzey Güvenlik -->
                <div style="background-color: #f8fafc; border: 1px solid #e2e8f0; border-radius: 12px; padding: 16px;">
                  <table role="presentation" width="100%" border="0" cellspacing="0" cellpadding="0">
                    <tr>
                      <td width="36" valign="top">
                        <div style="width: 32px; height: 32px; border-radius: 8px; background-color: #fffbeb; color: #d97706; font-size: 18px; line-height: 32px; text-align: center;">
                          🔑
                        </div>
                      </td>
                      <td style="padding-left: 12px;">
                        <div style="font-size: 14px; font-weight: 700; color: #0f172a; margin-bottom: 3px;">
                          BYOK Şifreleme
                        </div>
                        <div style="font-size: 12.5px; color: #64748b; line-height: 1.5;">
                          Kendi AI sağlayıcı anahtarlarınızı (OpenAI, Claude, Grok, Gemini, Groq) sisteme tanımlayarak sınırsız sorgulama yapın. Tüm anahtarlarınız Argon2id ve AES-256-GCM ile korunur.
                        </div>
                      </td>
                    </tr>
                  </table>
                </div>
              </div>

              <!-- Nasıl Kullanılır? 3 Kolay Adım -->
              <div style="background-color: #f1f5f9; border-radius: 14px; padding: 20px; margin-bottom: 28px;">
                <h3 style="margin: 0 0 14px 0; font-size: 13.5px; font-weight: 700; color: #0f172a; text-transform: uppercase; letter-spacing: 0.5px;">
                  💡 3 Kolay Adımda Başlayın:
                </h3>

                <table role="presentation" width="100%" border="0" cellspacing="0" cellpadding="0">
                  <tr>
                    <td class="step-col" width="33%" valign="top" style="padding-right: 6px;">
                      <div style="background-color: #ffffff; border-radius: 10px; padding: 12px; border: 1px solid #e2e8f0;">
                        <div style="font-size: 12px; font-weight: 800; color: #2563eb; margin-bottom: 4px;">1. GİRİŞ YAPIN</div>
                        <div style="font-size: 12px; color: #64748b; line-height: 1.4;">
                          Çalışma alanınıza erişerek sohbet ekranını açın.
                        </div>
                      </div>
                    </td>
                    <td class="step-col" width="33%" valign="top" style="padding: 0 3px;">
                      <div style="background-color: #ffffff; border-radius: 10px; padding: 12px; border: 1px solid #e2e8f0;">
                        <div style="font-size: 12px; font-weight: 800; color: #7c3aed; margin-bottom: 4px;">2. MODELİ SEÇİN</div>
                        <div style="font-size: 12px; color: #64748b; line-height: 1.4;">
                          İhtiyacınıza uygun modeli veya dokümanı belirleyin.
                        </div>
                      </div>
                    </td>
                    <td class="step-col" width="33%" valign="top" style="padding-left: 6px;">
                      <div style="background-color: #ffffff; border-radius: 10px; padding: 12px; border: 1px solid #e2e8f0;">
                        <div style="font-size: 12px; font-weight: 800; color: #059669; margin-bottom: 4px;">3. ÜRETİN</div>
                        <div style="font-size: 12px; color: #64748b; line-height: 1.4;">
                          Sorular sorun, kod yazın veya analiz başlatın.
                        </div>
                      </div>
                    </td>
                  </tr>
                </table>
              </div>

              <!-- Harekete Geçirici Buton (CTA) -->
              <div style="text-align: center; margin: 0 0 28px 0;">
                <a href="{{appLink}}" class="cta-button" style="display: inline-block; background: linear-gradient(135deg, #2563eb 0%, #1d4ed8 100%); color: #ffffff; font-size: 15px; font-weight: 700; text-decoration: none; padding: 14px 36px; border-radius: 12px; box-shadow: 0 4px 14px rgba(37, 99, 235, 0.35); letter-spacing: 0.3px;">
                  🚀 Çalışma Alanına Giriş Yap
                </a>
              </div>

              <!--  Destek İletişim Kutusu -->
              <div style="background-color: #eff6ff; border: 1px solid #bfdbfe; border-radius: 12px; padding: 18px 20px; margin-bottom: 24px;">
                <table role="presentation" width="100%" border="0" cellspacing="0" cellpadding="0">
                  <tr>
                    <td width="36" valign="top">
                      <div style="font-size: 24px; line-height: 1;">
                        💬
                      </div>
                    </td>
                    <td style="padding-left: 12px;">
                      <div style="font-size: 13.5px; font-weight: 700; color: #1e3a8a; margin-bottom: 4px;">
                        Doğrudan Teknik Destek
                      </div>
                      <p style="margin: 0 0 8px 0; font-size: 12.5px; color: #1e40af; line-height: 1.55;">
                        DevPilot AI kullanımı, özellikler veya önerilerinizle ilgili herhangi bir sorunuz olursa doğrudan sistem mimarına ulaşabilirsiniz:
                      </p>
                      <div style="font-size: 13px; font-weight: 700; color: #1d4ed8;">
                        ✉️ Destek & İletişim: <a href="mailto:{{founderEmail}}" style="color: #1d4ed8; text-decoration: underline;">{{founderEmail}}</a>
                      </div>
                    </td>
                  </tr>
                </table>
              </div>

              <!-- Sitenin Ana Sayfa Linki & Teşekkür -->
              <p style="margin: 0; font-size: 13px; color: #475569; line-height: 1.6;">
                Verimli ve başarılı çalışmalar dileriz,<br/>
                <strong style="color: #0f172a;">DevPilot AI Ekibi</strong> 
              </p>

            </td>
          </tr>

          <!-- Alt Bilgi (Footer) -->
          <tr>
            <td style="background-color: #0f172a; padding: 22px 32px; text-align: center;">
              <p style="margin: 0 0 6px 0; font-size: 12px; font-weight: 600; color: #cbd5e1;">
                DevPilot AI Workspace — Akıllı Geliştirici & Analiz Platformu
              </p>
              <p style="margin: 0 0 8px 0; font-size: 11px; color: #64748b;">
                 Sistem Mimarı: Yaşar Apaydın • Destek: <a href="mailto:{{founderEmail}}" style="color: #60a5fa; text-decoration: none;">{{founderEmail}}</a>
              </p>
              <p style="margin: 0; font-size: 11px; color: #475569;">
                © 2026 DevPilot AI. Tüm hakları saklıdır.
              </p>
            </td>
          </tr>

        </table>
      </td>
    </tr>
  </table>
</body>
</html>
""";

        _logger.LogInformation("================== WELCOME EMAIL DISPATCH ==================");
        _logger.LogInformation("To: {ToEmail}", toEmail);
        _logger.LogInformation("Name: {Name}", greetingName);
        _logger.LogInformation("Platform URL: {BaseUrl}", baseUrl);
        _logger.LogInformation("============================================================");

        await TrySendSmtpAsync(toEmail, "DevPilot AI'a Hoş Geldiniz! 🚀 Akıllı Çalışma Alanınız Hazır", htmlBody, isHtml: true, cancellationToken);
    }

    public async Task SendFeedbackEmailAsync(
        string senderEmail,
        string senderName,
        string category,
        string message,
        string? attachmentFileName = null,
        byte[]? attachmentBytes = null,
        string? attachmentContentType = null,
        CancellationToken cancellationToken = default)
    {
        var cleanSenderEmail = string.IsNullOrWhiteSpace(senderEmail) ? "anonim@devpilot.local" : senderEmail.Trim();
        var cleanSenderName = string.IsNullOrWhiteSpace(senderName) ? "DevPilot Kullanıcısı" : senderName.Trim();
        var cleanCategory = string.IsNullOrWhiteSpace(category) ? "Kullanıcı Görüşü" : category.Trim();
        var safeMessage = WebUtility.HtmlEncode(message ?? string.Empty).Replace("\n", "<br/>");
        const string recipientEmail = "apaydinyasar0@gmail.com";

        var (badgeColor, badgeBg) = cleanCategory.ToLowerInvariant() switch
        {
            var c when c.Contains("hata") || c.Contains("bug") => ("#b91c1c", "#fee2e2"),
            var c when c.Contains("öneri") || c.Contains("geliştirme") => ("#0369a1", "#e0f2fe"),
            var c when c.Contains("görüş") || c.Contains("fikir") => ("#047857", "#d1fae5"),
            _ => ("#4338ca", "#e0e7ff")
        };

        var htmlBody = $"""
<!DOCTYPE html>
<html lang="tr">
<head>
  <meta charset="UTF-8">
  <title>DevPilot AI - Kullanıcı Görüşü & Hata Bildirimi</title>
</head>
<body style="margin: 0; padding: 0; background-color: #f8fafc; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; color: #0f172a;">
  <table role="presentation" width="100%" border="0" cellspacing="0" cellpadding="0" style="padding: 32px 16px;">
    <tr>
      <td align="center">
        <table role="presentation" width="100%" border="0" cellspacing="0" cellpadding="0" style="max-width: 600px; background-color: #ffffff; border-radius: 16px; border: 1px solid #e2e8f0; box-shadow: 0 10px 25px -5px rgba(15, 23, 42, 0.08); overflow: hidden;">
          <tr>
            <td height="5" style="background: linear-gradient(90deg, #2563eb 0%, #3b82f6 50%, #60a5fa 100%);"></td>
          </tr>
          <tr>
            <td style="padding: 32px 32px 24px 32px;">
              <table role="presentation" width="100%" border="0" cellspacing="0" cellpadding="0" style="margin-bottom: 20px;">
                <tr>
                  <td>
                    <div style="font-size: 20px; font-weight: 800; color: #0f172a;">
                      DevPilot <span style="color: #2563eb;">AI</span>
                    </div>
                    <div style="font-size: 12px; color: #64748b; margin-top: 2px;">
                      Kullanıcı Geri Bildirimi & Destek Sistemi
                    </div>
                  </td>
                  <td align="right">
                    <span style="font-size: 11px; font-weight: 700; color: {badgeColor}; background-color: {badgeBg}; padding: 6px 14px; border-radius: 9999px; text-transform: uppercase;">
                      {WebUtility.HtmlEncode(cleanCategory)}
                    </span>
                  </td>
                </tr>
              </table>

              <div style="background-color: #f1f5f9; border-radius: 12px; padding: 16px; margin-bottom: 24px;">
                <table role="presentation" width="100%" border="0" cellspacing="0" cellpadding="4">
                  <tr>
                    <td width="90" style="font-size: 12px; font-weight: 700; color: #64748b;">Gönderen:</td>
                    <td style="font-size: 13px; font-weight: 600; color: #0f172a;">{WebUtility.HtmlEncode(cleanSenderName)}</td>
                  </tr>
                  <tr>
                    <td style="font-size: 12px; font-weight: 700; color: #64748b;">E-posta:</td>
                    <td style="font-size: 13px; font-weight: 600; color: #2563eb;"><a href="mailto:{WebUtility.HtmlEncode(cleanSenderEmail)}" style="color: #2563eb; text-decoration: none;">{WebUtility.HtmlEncode(cleanSenderEmail)}</a></td>
                  </tr>
                  <tr>
                    <td style="font-size: 12px; font-weight: 700; color: #64748b;">Tarih:</td>
                    <td style="font-size: 12px; color: #475569;">{DateTime.UtcNow:dd MMMM yyyy HH:mm} (UTC)</td>
                  </tr>
                </table>
              </div>

              <div style="margin-bottom: 24px;">
                <div style="font-size: 13px; font-weight: 700; color: #334155; margin-bottom: 8px; text-transform: uppercase; letter-spacing: 0.5px;">
                  Kullanıcı Mesajı:
                </div>
                <div style="background-color: #ffffff; border: 1px solid #cbd5e1; border-radius: 10px; padding: 18px; font-size: 14px; line-height: 1.6; color: #1e293b;">
                  {safeMessage}
                </div>
              </div>

              {(attachmentBytes != null && attachmentBytes.Length > 0 ? $"""
              <div style="background-color: #f8fafc; border: 1px dashed #cbd5e1; border-radius: 8px; padding: 12px 16px; margin-bottom: 20px;">
                <div style="font-size: 12px; font-weight: 700; color: #334155;">📎 Ekli Dosya:</div>
                <div style="font-size: 12px; color: #64748b; margin-top: 4px;">{WebUtility.HtmlEncode(attachmentFileName ?? "ek")} ({attachmentBytes.Length / 1024} KB) - E-posta ekindedir.</div>
              </div>
              """ : "")}

              <p style="margin: 0; font-size: 12px; color: #64748b; line-height: 1.5; border-top: 1px solid #f1f5f9; pt-4; padding-top: 16px;">
                💡 <em>Bu e-postayı doğrudan yanıtlayarak kullanıcıya (<a href="mailto:{WebUtility.HtmlEncode(cleanSenderEmail)}" style="color: #2563eb;">{WebUtility.HtmlEncode(cleanSenderEmail)}</a>) geri dönüş yapabilirsiniz.</em>
              </p>
            </td>
          </tr>
          <tr>
            <td style="background-color: #f8fafc; border-top: 1px solid #f1f5f9; padding: 14px 32px; text-align: center;">
              <span style="font-size: 11px; color: #94a3b8;">DevPilot AI Workspace • Geliştirici & Sistem Mimarı: Yaşar Apaydın</span>
            </td>
          </tr>
        </table>
      </td>
    </tr>
  </table>
</body>
</html>
""";

        _logger.LogInformation("================== USER FEEDBACK RECEIVED ==================");
        _logger.LogInformation("Category: {Category} | From: {Name} <{Email}>", cleanCategory, cleanSenderName, cleanSenderEmail);
        _logger.LogInformation("Attachment: {AttachmentName} ({Length} bytes)", attachmentFileName ?? "none", attachmentBytes?.Length ?? 0);
        _logger.LogInformation("============================================================");

        await TrySendFeedbackSmtpAsync(
            toEmail: recipientEmail,
            replyToEmail: cleanSenderEmail,
            replyToName: cleanSenderName,
            subject: $"[DevPilot AI] {cleanCategory} - {cleanSenderName}",
            htmlBody: htmlBody,
            attachmentFileName: attachmentFileName,
            attachmentBytes: attachmentBytes,
            attachmentContentType: attachmentContentType,
            cancellationToken: cancellationToken);
    }

    private async Task TrySendFeedbackSmtpAsync(
        string toEmail,
        string replyToEmail,
        string replyToName,
        string subject,
        string htmlBody,
        string? attachmentFileName,
        byte[]? attachmentBytes,
        string? attachmentContentType,
        CancellationToken cancellationToken)
    {
        var isDevelopment = string.Equals(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"), "Development", StringComparison.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(_options.SmtpHost) || string.IsNullOrWhiteSpace(_options.SmtpUser))
        {
            if (!isDevelopment)
            {
                throw new InvalidOperationException("Fatal SMTP Configuration Error: SMTP host and credentials are required in Production environment.");
            }
            _logger.LogWarning("SMTP not configured. Feedback email was not dispatched over SMTP.");
            return;
        }

        try
        {
            var user = _options.SmtpUser.Trim();
            var password = (_options.SmtpPassword ?? string.Empty).Replace(" ", "").Trim();

            using var client = new SmtpClient(_options.SmtpHost.Trim(), _options.SmtpPort)
            {
                EnableSsl = true,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(user, password),
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Timeout = 20000
            };

            var fromAddress = string.IsNullOrWhiteSpace(_options.FromEmail) ? user : _options.FromEmail.Trim();
            var fromDisplayName = "DevPilot AI Bildirim Sistemi";

            using var mail = new MailMessage(new MailAddress(fromAddress, fromDisplayName), new MailAddress(toEmail.Trim()))
            {
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };

            if (!string.IsNullOrWhiteSpace(replyToEmail))
            {
                mail.ReplyToList.Add(new MailAddress(replyToEmail.Trim(), replyToName));
            }

            if (attachmentBytes != null && attachmentBytes.Length > 0 && !string.IsNullOrWhiteSpace(attachmentFileName))
            {
                var stream = new MemoryStream(attachmentBytes);
                var attachment = new Attachment(stream, attachmentFileName, attachmentContentType ?? "application/octet-stream");
                mail.Attachments.Add(attachment);
            }

            await client.SendMailAsync(mail, cancellationToken);
            _logger.LogInformation("Feedback email successfully delivered to {ToEmail}.", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send feedback email to {ToEmail} via SMTP.", toEmail);
            throw;
        }
    }

    public async Task SendNewUserRegisteredNotificationAsync(
        string firstName,
        string lastName,
        string email,
        CancellationToken cancellationToken = default)
    {
        const string adminEmail = "apaydinyasar0@gmail.com";
        var fullName = $"{firstName} {lastName}".Trim();
        var registeredAtUtc = DateTime.UtcNow.ToString("dd.MM.yyyy HH:mm:ss 'UTC'");

        var subject = $"[DevPilot AI] Yeni Kullanıcı Kaydı: {fullName}";
        var htmlBody = $"""
<!DOCTYPE html>
<html lang="tr">
<head>
  <meta charset="UTF-8">
  <title>Yeni Kullanıcı Bildirimi</title>
</head>
<body style="margin: 0; padding: 24px; background-color: #f8fafc; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; color: #0f172a;">
  <table role="presentation" width="100%" border="0" cellspacing="0" cellpadding="0" style="max-width: 520px; margin: 0 auto; background-color: #ffffff; border-radius: 14px; border: 1px solid #e2e8f0; overflow: hidden; box-shadow: 0 4px 12px rgba(15, 23, 42, 0.05);">
    <tr>
      <td style="background: linear-gradient(90deg, #2563eb, #4f46e5); height: 6px;"></td>
    </tr>
    <tr>
      <td style="padding: 28px 32px;">
        <div style="font-size: 18px; font-weight: 800; color: #0f172a; margin-bottom: 6px;">
          DevPilot <span style="color: #2563eb;">AI</span> — Yeni Kullanıcı Kaydı
        </div>
        <p style="font-size: 14px; color: #64748b; margin-top: 0; margin-bottom: 20px;">
          Sisteme yeni bir kullanıcı başarıyla kaydoldu. Detaylar aşağıda yer almaktadır:
        </p>
        <table role="presentation" width="100%" border="0" cellspacing="0" cellpadding="0" style="background-color: #f1f5f9; border-radius: 10px; padding: 16px; margin-bottom: 20px;">
          <tr>
            <td style="font-size: 13px; font-weight: 600; color: #475569; padding: 6px 0; width: 120px;">Adı Soyadı:</td>
            <td style="font-size: 14px; font-weight: 700; color: #0f172a; padding: 6px 0;">{WebUtility.HtmlEncode(fullName)}</td>
          </tr>
          <tr>
            <td style="font-size: 13px; font-weight: 600; color: #475569; padding: 6px 0; width: 120px;">E-posta Adresi:</td>
            <td style="font-size: 14px; font-weight: 700; color: #2563eb; padding: 6px 0;">{WebUtility.HtmlEncode(email)}</td>
          </tr>
          <tr>
            <td style="font-size: 13px; font-weight: 600; color: #475569; padding: 6px 0; width: 120px;">Kayıt Zamanı:</td>
            <td style="font-size: 13px; color: #64748b; padding: 6px 0;">{registeredAtUtc}</td>
          </tr>
        </table>
        <p style="font-size: 12px; color: #94a3b8; margin: 0;">
          Bu bilgilendirme e-postası DevPilot AI sistem güvenlik ve kullanıcı izleme servisi tarafından otomatik olarak üretilmiştir.
        </p>
      </td>
    </tr>
  </table>
</body>
</html>
""";

        await TrySendSmtpAsync(adminEmail, subject, htmlBody, isHtml: true, cancellationToken);
    }

    private async Task TrySendSmtpAsync(string toEmail, string subject, string body, bool isHtml, CancellationToken cancellationToken)
    {
        var isDevelopment = string.Equals(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"), "Development", StringComparison.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(_options.SmtpHost) || string.IsNullOrWhiteSpace(_options.SmtpUser))
        {
            if (!isDevelopment)
            {
                throw new InvalidOperationException("Fatal SMTP Configuration Error: SMTP host and credentials are required in Production environment.");
            }
            // SMTP is not configured, logged to console for development
            _logger.LogWarning("SMTP not configured in Development. Email to {ToEmail} was not dispatched.", toEmail);
            return;
        }

        try
        {
            var user = _options.SmtpUser.Trim();
            var password = (_options.SmtpPassword ?? string.Empty).Replace(" ", "").Trim();

            using var client = new SmtpClient(_options.SmtpHost.Trim(), _options.SmtpPort)
            {
                EnableSsl = true,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(user, password),
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Timeout = 15000
            };

            var fromAddress = string.IsNullOrWhiteSpace(_options.FromEmail) ? user : _options.FromEmail.Trim();
            var fromDisplayName = string.IsNullOrWhiteSpace(_options.FromName) ? "DevPilot AI" : _options.FromName.Trim();

            using var mail = new MailMessage(new MailAddress(fromAddress, fromDisplayName), new MailAddress(toEmail.Trim()))
            {
                Subject = subject,
                Body = body,
                IsBodyHtml = isHtml
            };

            await client.SendMailAsync(mail, cancellationToken);
            _logger.LogInformation("Email successfully sent to {ToEmail}.", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {ToEmail} via SMTP. Verification details are logged above for development.", toEmail);
        }
    }
}
