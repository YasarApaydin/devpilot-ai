import React, { useEffect } from 'react';
import { Link } from 'react-router-dom';
import {
  ShieldCheck,
  Lock,
  ArrowLeft,
  Sliders,
  ExternalLink,
  Info,
  CheckCircle2,
  HardDrive,
  Settings,
} from 'lucide-react';
import { useCookieConsent } from '../context/CookieConsentContext';
import { DEVPILOT_COOKIES } from '../types/cookieConsent';
import { Button } from '../components/ui/Button';

export const CookiePolicyPage: React.FC = () => {
  const { openPreferences } = useCookieConsent();

  useEffect(() => {
    document.title = 'Çerez Politikası | DevPilot AI';
    window.scrollTo(0, 0);
  }, []);

  return (
    <div className="min-h-screen bg-[#f8fafc] text-slate-800 flex flex-col selection:bg-blue-600 selection:text-white">
      {/* Top Header */}
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
              Çerez (Cookie) ve Yerel Depolama Politikası
            </span>
          </div>

          <div className="flex items-center gap-2">
            <button
              type="button"
              onClick={openPreferences}
              className="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-xs font-semibold text-white bg-blue-600 hover:bg-blue-700 shadow-xs transition-colors cursor-pointer"
            >
              <Sliders className="w-3.5 h-3.5" />
              <span>Tercihleri Düzenle</span>
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
            <div className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-semibold bg-emerald-50 text-emerald-700 border border-emerald-200">
              <CheckCircle2 className="w-3.5 h-3.5" /> Şeffaf ve Güvenli Veri Depolama
            </div>
            <h1 className="text-2xl sm:text-3xl font-extrabold text-slate-900 tracking-tight">
              DevPilot AI Çerez (Cookie) ve Yerel Depolama Politikası
            </h1>
            <p className="text-xs sm:text-sm text-slate-500">
              Son Güncelleme Tarihi: <strong>16 Ekim 2026</strong> • Versiyon: <strong>1.0</strong>
            </p>
          </div>

          {/* 1. Çerez Nedir */}
          <section className="space-y-3">
            <h2 className="text-base sm:text-lg font-bold text-slate-900 flex items-center gap-2">
              <span className="w-6 h-6 rounded-lg bg-blue-100 text-blue-700 flex items-center justify-center text-xs font-bold">1</span>
              Çerezler ve Yerel Depolama Nedir?
            </h2>
            <p className="text-xs sm:text-sm text-slate-600 leading-relaxed">
              Çerezler (Cookies), bir web sitesini ziyaret ettiğinizde tarayıcınız aracılığıyla cihazınıza kaydedilen küçük metin dosyalarıdır. Benzer şekilde, modern web tarayıcılarında bulunan <strong>LocalStorage</strong> ve <strong>SessionStorage</strong> teknolojileri de arayüz tercihlerinizi ve oturum durumunuzu güvenli şekilde hafızada tutmaya yarar.
            </p>
            <p className="text-xs sm:text-sm text-slate-600 leading-relaxed">
              DevPilot AI olarak, çerezleri kullanıcı deneyimini iyileştirmek, hesap güvenliğini sağlamak ve tercih ettiğiniz yapay zekâ modelini hatırlamak amacıyla kullanmaktayız.
            </p>
          </section>

          {/* 2. Kategoriler */}
          <section className="space-y-3">
            <h2 className="text-base sm:text-lg font-bold text-slate-900 flex items-center gap-2">
              <span className="w-6 h-6 rounded-lg bg-blue-100 text-blue-700 flex items-center justify-center text-xs font-bold">2</span>
              Kullanılan Çerez Kategorileri
            </h2>
            <div className="space-y-3 text-xs sm:text-sm text-slate-600">
              <div className="p-3.5 rounded-xl border border-slate-200 bg-slate-50/50">
                <strong className="text-slate-900 block mb-1">A. Zorunlu ve Güvenlik Çerezleri (Strictly Necessary):</strong>
                Platformun temel işlevlerini yerine getirebilmesi, güvenli oturum açılması (JWT doğrulama), CSRF saldırılarının önlenmesi ve izin tercihlerinizin hatırlanması için zorunludur. Bu çerezler kapatılamaz.
              </div>

              <div className="p-3.5 rounded-xl border border-slate-200 bg-slate-50/50">
                <strong className="text-slate-900 block mb-1">B. İşlevsel Çerezler ve Yerel Tercihler (Functional):</strong>
                Sohbet ekranında son seçtiğiniz yapay zekâ modelini (Gemini, Groq, Claude) veya giriş ekranındaki "Beni Hatırla" tercihini hafızada tutar. İsteğe bağlıdır.
              </div>

              <div className="p-3.5 rounded-xl border border-slate-200 bg-slate-50/50">
                <strong className="text-slate-900 block mb-1">C. Analitik ve Performans (Analytics):</strong>
                Sistemin yanıt sürelerini ve olası API hatalarını anonim olarak analiz etmek için kullanılır. İsteğe bağlıdır.
              </div>

              <div className="p-3.5 rounded-xl border border-slate-200 bg-slate-50/50">
                <strong className="text-slate-900 block mb-1">D. Pazarlama ve Reklam Çerezleri (Marketing):</strong>
                DevPilot AI platformunda hiçbir reklam ağı, çapraz site takipçisi veya veri komisyoncusu yer almamaktadır.
              </div>
            </div>
          </section>

          {/* 3. Tam Çerez Tablosu */}
          <section className="space-y-3">
            <h2 className="text-base sm:text-lg font-bold text-slate-900 flex items-center gap-2">
              <span className="w-6 h-6 rounded-lg bg-blue-100 text-blue-700 flex items-center justify-center text-xs font-bold">3</span>
              DevPilot AI Tarafından Kullanılan Çerez ve Depolama Envanteri
            </h2>
            <p className="text-xs text-slate-500">
              Aşağıdaki tabloda projemizde kullanılan gerçek çerezler, depolama anahtarları, türleri ve süreleri şeffaf bir şekilde listelenmiştir:
            </p>

            <div className="border border-slate-200 rounded-xl overflow-x-auto shadow-2xs">
              <table className="w-full text-left text-xs border-collapse">
                <thead>
                  <tr className="bg-slate-50 border-b border-slate-200 text-slate-700 font-semibold">
                    <th className="p-3">Çerez / Anahtar Adı</th>
                    <th className="p-3">Kategori</th>
                    <th className="p-3">Sağlayıcı / Konum</th>
                    <th className="p-3">Saklama Türü</th>
                    <th className="p-3">Kullanım Amacı</th>
                    <th className="p-3">Geçerlilik Süresi</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100 text-slate-600">
                  {DEVPILOT_COOKIES.map((c) => (
                    <tr key={c.name} className="hover:bg-slate-50/80 transition-colors">
                      <td className="p-3 font-mono font-semibold text-slate-900 whitespace-nowrap">
                        {c.name}
                      </td>
                      <td className="p-3 whitespace-nowrap">
                        {c.category === 'necessary' ? (
                          <span className="px-2 py-0.5 rounded-full text-[10px] font-bold bg-emerald-100 text-emerald-800">
                            Zorunlu
                          </span>
                        ) : (
                          <span className="px-2 py-0.5 rounded-full text-[10px] font-medium bg-blue-100 text-blue-800">
                            İşlevsel
                          </span>
                        )}
                      </td>
                      <td className="p-3 text-[11px] whitespace-nowrap">{c.provider}</td>
                      <td className="p-3 font-mono text-[11px] whitespace-nowrap">{c.type}</td>
                      <td className="p-3 text-[11px] leading-relaxed max-w-xs">{c.purpose}</td>
                      <td className="p-3 whitespace-nowrap text-[11px]">{c.duration}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </section>

          {/* 4. Tercihleri Nasıl Değiştirirsiniz */}
          <section className="space-y-3">
            <h2 className="text-base sm:text-lg font-bold text-slate-900 flex items-center gap-2">
              <span className="w-6 h-6 rounded-lg bg-blue-100 text-blue-700 flex items-center justify-center text-xs font-bold">4</span>
              Çerez Tercihlerini Nasıl Yönetebilir veya Değiştirebilirsiniz?
            </h2>
            <p className="text-xs sm:text-sm text-slate-600 leading-relaxed">
              İstediğiniz zaman çerez tercihlerinizi değiştirebilirsiniz. Aşağıdaki butona tıklayarak tercih panelini açabilir veya sayfa altındaki (footer) <strong>"Çerez Tercihleri"</strong> bağlantısını kullanabilirsiniz:
            </p>
            <div className="p-4 rounded-xl bg-slate-50 border border-slate-200 flex flex-col sm:flex-row items-center justify-between gap-3">
              <div className="text-xs text-slate-600">
                Tercihlerinizi özelleştirmek veya önceden verdiğiniz onayları değiştirmek için tıklayın:
              </div>
              <button
                type="button"
                onClick={openPreferences}
                className="px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-xl text-xs font-semibold shadow-xs flex items-center gap-1.5 transition-colors cursor-pointer shrink-0"
              >
                <Sliders className="w-3.5 h-3.5" />
                <span>Çerez Tercihlerini Aç</span>
              </button>
            </div>
          </section>

          {/* 5. Tarayıcı Ayarlarından Çerez Silme */}
          <section className="space-y-3">
            <h2 className="text-base sm:text-lg font-bold text-slate-900 flex items-center gap-2">
              <span className="w-6 h-6 rounded-lg bg-blue-100 text-blue-700 flex items-center justify-center text-xs font-bold">5</span>
              Tarayıcı Ayarlarından Çerezleri Temizleme
            </h2>
            <p className="text-xs sm:text-sm text-slate-600 leading-relaxed">
              Dilerseniz tarayıcınızın ayarlar menüsünden tüm çerezleri silebilir veya engelleyebilirsiniz:
            </p>
            <ul className="list-disc list-inside space-y-1 text-xs text-slate-600 pl-2">
              <li><strong>Google Chrome:</strong> Ayarlar ➔ Gizlilik ve Güvenlik ➔ Üçüncü taraf çerezler</li>
              <li><strong>Mozilla Firefox:</strong> Seçenekler ➔ Gizlilik ve Güvenlik ➔ Çerezler ve Site Verileri</li>
              <li><strong>Apple Safari:</strong> Tercihler ➔ Gizlilik ➔ Tüm Web Sitesi Verilerini Yönet</li>
              <li><strong>Microsoft Edge:</strong> Ayarlar ➔ Çerezler ve Site İzinleri ➔ Çerezleri ve Site Verilerini Yönet</li>
            </ul>
            <p className="text-[11px] text-amber-700 bg-amber-50 p-2.5 rounded-lg border border-amber-200">
              Not: Zorunlu oturum çerezlerini tarayıcınızdan engellerseniz, DevPilot AI hesabınıza giriş yapamaz ve korumalı özellikleri kullanamazsınız.
            </p>
          </section>

          {/* Bottom Actions */}
          <div className="pt-6 border-t border-slate-200 flex flex-col sm:flex-row items-center justify-between gap-4">
            <div className="flex items-center gap-3">
              <button
                type="button"
                onClick={openPreferences}
                className="px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-xl text-xs font-semibold shadow-xs flex items-center gap-1.5 transition-colors cursor-pointer"
              >
                <Sliders className="w-3.5 h-3.5" />
                <span>Tercihleri Düzenle</span>
              </button>
              <Link to="/privacy">
                <Button variant="outline" size="sm">
                  Gizlilik Politikası
                </Button>
              </Link>
            </div>

            <Link to="/app" className="text-xs text-blue-600 hover:underline font-semibold">
              DevPilot AI Çalışma Alanına Dön ➔
            </Link>
          </div>
        </div>
      </main>

      {/* Footer */}
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
