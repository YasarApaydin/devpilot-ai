import React, { useEffect } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import {
  ShieldCheck,
  Lock,
  ArrowLeft,
  Sliders,
  FileText,
  UserCheck,
  Database,
  Cpu,
  KeyRound,
  ExternalLink,
} from 'lucide-react';
import { useCookieConsent } from '../context/CookieConsentContext';
import { Button } from '../components/ui/Button';

export const PrivacyPolicyPage: React.FC = () => {
  const navigate = useNavigate();
  const { openPreferences } = useCookieConsent();

  useEffect(() => {
    document.title = 'Gizlilik Politikası | DevPilot AI';
    window.scrollTo(0, 0);
  }, []);

  return (
    <div className="min-h-screen bg-[#f8fafc] text-slate-800 flex flex-col selection:bg-blue-600 selection:text-white">
      {/* Top Navigation */}
      <header className="border-b border-slate-200 bg-white/90 backdrop-blur-md sticky top-0 z-40">
        <div className="max-w-5xl mx-auto px-4 sm:px-6 h-16 flex items-center justify-between">
          <div className="flex items-center gap-3">
            <Link to="/" className="flex items-center gap-2.5 group">
              <img
                src="/app-logo.png"
                alt="DevPilot AI"
                className="w-8 h-8 rounded-full object-cover border border-slate-200 shadow-2xs group-hover:scale-105 transition-transform"
              />
              <div className="flex items-center font-bold text-slate-900 text-base">
                <span>DevPilot</span>
                <span className="text-blue-600 font-light ml-0.5">AI</span>
              </div>
            </Link>
            <span className="text-slate-300 hidden sm:inline">|</span>
            <span className="text-xs font-semibold text-slate-600 hidden sm:inline">
              Gizlilik Politikası ve Aydınlatma Metni
            </span>
          </div>

          <div className="flex items-center gap-2">
            <button
              type="button"
              onClick={openPreferences}
              className="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-xs font-medium text-slate-700 bg-slate-100 hover:bg-slate-200 transition-colors cursor-pointer"
            >
              <Sliders className="w-3.5 h-3.5 text-slate-500" />
              <span className="hidden sm:inline">Çerez Tercihlerini Düzenle</span>
              <span className="sm:hidden">Çerezler</span>
            </button>
            <Link to="/">
              <Button variant="outline" size="sm" leftIcon={<ArrowLeft className="w-3.5 h-3.5" />}>
                Ana Sayfa
              </Button>
            </Link>
          </div>
        </div>
      </header>

      {/* Main Content Area */}
      <main className="flex-1 py-8 sm:py-12 px-4 sm:px-6">
        <div className="max-w-4xl mx-auto bg-white border border-slate-200 rounded-2xl shadow-sm p-6 sm:p-10 space-y-8">
          {/* Header Banner */}
          <div className="border-b border-slate-100 pb-6 space-y-2">
            <div className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-semibold bg-blue-50 text-blue-700 border border-blue-200">
              <ShieldCheck className="w-3.5 h-3.5" /> 6698 Sayılı KVKK ve GDPR Uyum Bildirimi
            </div>
            <h1 className="text-2xl sm:text-3xl font-extrabold text-slate-900 tracking-tight">
              DevPilot AI Gizlilik Politikası ve Kişisel Verilerin Korunması
            </h1>
            <p className="text-xs sm:text-sm text-slate-500">
              Son Güncelleme Tarihi: <strong>16 Ekim 2026</strong> • Versiyon: <strong>1.0</strong>
            </p>
          </div>

          {/* 1. Veri Sorumlusu */}
          <section className="space-y-3">
            <h2 className="text-base sm:text-lg font-bold text-slate-900 flex items-center gap-2">
              <span className="w-6 h-6 rounded-lg bg-blue-100 text-blue-700 flex items-center justify-center text-xs font-bold">1</span>
              Veri Sorumlusu ve Sistem Bilgisi
            </h2>
            <p className="text-xs sm:text-sm text-slate-600 leading-relaxed">
              Bu aydınlatma metni, 6698 sayılı Kişisel Verilerin Korunması Kanunu (“KVKK”) ve Genel Veri Koruma Tüzüğü (“GDPR”) uyarınca, <strong>DevPilot AI</strong> platformunun kullanılması sırasında işlenen kişisel verilere ilişkin kullanıcılarımızı şeffaf bir şekilde bilgilendirmek amacıyla hazırlanmıştır.
            </p>
            <div className="p-3.5 rounded-xl bg-slate-50 border border-slate-200/80 text-xs text-slate-700 space-y-1">
              <div><strong>Geliştirici &amp; Sistem Mimarı:</strong> Yaşar Apaydın</div>
              <div><strong>Platform:</strong> DevPilot AI Enterprise AI Agent &amp; Security Workspace</div>
              <div><strong>Resmi İletişim:</strong> platform içi geri bildirim mekanizması ve doğrulanmış geliştirici kanalları.</div>
            </div>
          </section>

          {/* 2. Toplanan Kişisel Veriler */}
          <section className="space-y-3">
            <h2 className="text-base sm:text-lg font-bold text-slate-900 flex items-center gap-2">
              <span className="w-6 h-6 rounded-lg bg-blue-100 text-blue-700 flex items-center justify-center text-xs font-bold">2</span>
              Toplanan Kişisel Veriler ve Kategorileri
            </h2>
            <p className="text-xs sm:text-sm text-slate-600 leading-relaxed">
              Platformumuzda yalnızca hizmetin verilebilmesi, hesap güvenliğinin sağlanması ve yapay zekâ asistan işlevlerinin icrası için zorunlu olan asgari veriler toplanmaktadır:
            </p>
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 text-xs">
              <div className="p-3.5 rounded-xl border border-slate-200 bg-slate-50/50">
                <div className="font-semibold text-slate-900 flex items-center gap-1.5 mb-1">
                  <UserCheck className="w-4 h-4 text-blue-600" /> Kimlik &amp; İletişim Verileri
                </div>
                <p className="text-slate-600">Ad, Soyad, E-posta adresi, kriptografik olarak tuzlanmış parola hash değerleri (PBKDF2/BCrypt).</p>
              </div>

              <div className="p-3.5 rounded-xl border border-slate-200 bg-slate-50/50">
                <div className="font-semibold text-slate-900 flex items-center gap-1.5 mb-1">
                  <Lock className="w-4 h-4 text-emerald-600" /> Cihaz &amp; Güvenlik Verileri
                </div>
                <p className="text-slate-600">IP adresi, istek zaman damgası, anonim cihaz kimliği (<code className="font-mono text-slate-800">dp_device_id</code>), tarayıcı bilgisi.</p>
              </div>

              <div className="p-3.5 rounded-xl border border-slate-200 bg-slate-50/50">
                <div className="font-semibold text-slate-900 flex items-center gap-1.5 mb-1">
                  <Database className="w-4 h-4 text-purple-600" /> Doküman &amp; Sohbet İçerikleri
                </div>
                <p className="text-slate-600">Yapay zekâya ilettiğiniz komutlar (prompt), sohbet geçmişi ve kitaplığınıza yüklediğiniz dokümanların indeksleri.</p>
              </div>

              <div className="p-3.5 rounded-xl border border-slate-200 bg-slate-50/50">
                <div className="font-semibold text-slate-900 flex items-center gap-1.5 mb-1">
                  <KeyRound className="w-4 h-4 text-amber-600" /> Şifrelenmiş API Anahtarları (BYOK)
                </div>
                <p className="text-slate-600">Kendi girdiğiniz OpenAI, Gemini, Groq, xAI, Anthropic anahtarları veritabanında <strong>kimlik doğrulamalı güvenli kasa mimarisi</strong> ile şifreli tutulur.</p>
              </div>
            </div>
          </section>

          {/* 3. Veri İşleme Amaçları */}
          <section className="space-y-3">
            <h2 className="text-base sm:text-lg font-bold text-slate-900 flex items-center gap-2">
              <span className="w-6 h-6 rounded-lg bg-blue-100 text-blue-700 flex items-center justify-center text-xs font-bold">3</span>
              Kişisel Verilerin İşlenme Amaçları ve Hukuki Sebepleri
            </h2>
            <p className="text-xs sm:text-sm text-slate-600 leading-relaxed">
              Kişisel verileriniz KVKK Madde 5/2-c (Sözleşmenin kurulması ve ifası), 5/2-ç (Veri sorumlusunun hukuki yükümlülüğü) ve 5/2-f (Meşru menfaat) hukuki sebeplerine dayalı olarak şu amaçlarla işlenir:
            </p>
            <ul className="list-disc list-inside space-y-1.5 text-xs sm:text-sm text-slate-600 pl-2">
              <li>Kullanıcı kaydının açılması, oturum yönetimi ve JWT kimlik doğrulamasının yapılması,</li>
              <li>Yapay zekâ modelleri ile gerçek zamanlı akış (streaming) sohbetinin gerçekleştirilmesi,</li>
              <li>Yüklediğiniz dokümanların kişisel RAG (Retrieval-Augmented Generation) hafızanıza indekslenmesi,</li>
              <li>Yetkisiz oturum açma, hesap ele geçirme veya siber saldırıların engellenmesi,</li>
              <li>Çerez tercihlerinizin ve arayüz kişiselleştirmelerinizin hafızada tutulması.</li>
            </ul>
          </section>

          {/* 4. BYOK ve Üçüncü Taraf Yapay Zekâ Sağlayıcıları */}
          <section className="space-y-3">
            <h2 className="text-base sm:text-lg font-bold text-slate-900 flex items-center gap-2">
              <span className="w-6 h-6 rounded-lg bg-blue-100 text-blue-700 flex items-center justify-center text-xs font-bold">4</span>
              BYOK (Kendi Anahtarını Getir) ve AI Sağlayıcıları ile İletişim
            </h2>
            <p className="text-xs sm:text-sm text-slate-600 leading-relaxed">
              DevPilot AI, kullanıcılarına kendi API anahtarlarını ekleme (Bring Your Own Key - BYOK) imkânı sunar. Bir yapay zekâ modeline (örn. Google Gemini, GroqCloud, OpenAI, xAI Grok veya Anthropic Claude) soru sorduğunuzda:
            </p>
            <div className="p-4 rounded-xl bg-blue-50/60 border border-blue-200 text-xs text-blue-900 space-y-2">
              <p>
                <strong>Önemli Güvenlik Prensibi:</strong> API anahtarınız sunucumuzda en yüksek güvenlik standartlarına sahip donanım düzeyinde güvenli şifreli kasada saklanır. İstek anında çözülerek yalnızca sizin adınıza ilgili sağlayıcının resmi HTTPS uç noktasına iletilir. Verileriniz hiçbir reklam firmasına, veri komisyoncusuna veya üçüncü şahsa satılmaz.
              </p>
            </div>
          </section>

          {/* 5. Veri Saklama ve Güvenlik Tedbirleri */}
          <section className="space-y-3">
            <h2 className="text-base sm:text-lg font-bold text-slate-900 flex items-center gap-2">
              <span className="w-6 h-6 rounded-lg bg-blue-100 text-blue-700 flex items-center justify-center text-xs font-bold">5</span>
              Veri Saklama Süresi ve Güvenlik Tedbirleri
            </h2>
            <p className="text-xs sm:text-sm text-slate-600 leading-relaxed">
              Verileriniz PostgreSQL ilişkisel veritabanında, modern güvenlik standartları (TLS 1.3 şifreleme, parametreli sorgular, katı CSP ve CORS kuralları) ile korunmaktadır. Hesabınız aktif olduğu sürece verileriniz saklanır; hesabınızı sildiğinizde ise tüm sohbetleriniz, dosyalarınız ve kimlik verileriniz veritabanımızdan kalıcı olarak silinir.
            </p>
          </section>

          {/* 6. Kullanıcı Hakları (KVKK m. 11 & GDPR) */}
          <section className="space-y-3">
            <h2 className="text-base sm:text-lg font-bold text-slate-900 flex items-center gap-2">
              <span className="w-6 h-6 rounded-lg bg-blue-100 text-blue-700 flex items-center justify-center text-xs font-bold">6</span>
              Kullanıcı Olarak Haklarınız (KVKK m. 11)
            </h2>
            <p className="text-xs sm:text-sm text-slate-600 leading-relaxed">
              KVKK'nın 11. maddesi ve GDPR uyarınca aşağıdaki haklara sahipsiniz:
            </p>
            <ul className="list-disc list-inside space-y-1.5 text-xs sm:text-sm text-slate-600 pl-2">
              <li>Kişisel verilerinizin işlenip işlenmediğini öğrenme,</li>
              <li>İşlenmişse buna ilişkin bilgi talep etme,</li>
              <li>İşlenme amacını ve amaca uygun kullanılıp kullanılmadığını öğrenme,</li>
              <li>Eksik veya yanlış işlenmişse düzeltilmesini isteme (Profil sayfasından güncelleyebilirsiniz),</li>
              <li>Kişisel verilerinizin silinmesini isteme: <strong>Profil sayfasında yer alan "Hesabımı Sil" butonuyla tüm verilerinizi tek tıkla kalıcı olarak silebilirsiniz.</strong></li>
            </ul>
          </section>

          {/* Bottom Navigation & Actions */}
          <div className="pt-6 border-t border-slate-200 flex flex-col sm:flex-row items-center justify-between gap-4">
            <div className="flex items-center gap-2">
              <button
                type="button"
                onClick={openPreferences}
                className="px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-xl text-xs font-semibold shadow-xs flex items-center gap-1.5 transition-colors cursor-pointer"
              >
                <Sliders className="w-3.5 h-3.5" />
                <span>Çerez Tercihlerini Düzenle</span>
              </button>
              <Link to="/cookies">
                <Button variant="outline" size="sm">
                  Çerez Politikası Tablosu
                </Button>
              </Link>
            </div>

            <Link to="/app" className="text-xs text-blue-600 hover:underline font-semibold">
              DevPilot AI Çalışma Alanına Dön ➔
            </Link>
          </div>
        </div>
      </main>

      {/* Simple Clean Footer */}
      <footer className="border-t border-slate-200 py-6 px-4 text-center text-xs text-slate-500 bg-slate-50">
        <div className="max-w-4xl mx-auto flex flex-col sm:flex-row items-center justify-between gap-3">
          <div>© {new Date().getFullYear()} DevPilot AI — Tüm hakları saklıdır.</div>
          <div className="flex items-center gap-4">
            <Link to="/privacy" className="hover:text-blue-600 underline">Gizlilik Politikası</Link>
            <Link to="/cookies" className="hover:text-blue-600 underline">Çerez Politikası</Link>
          </div>
        </div>
      </footer>
    </div>
  );
};
