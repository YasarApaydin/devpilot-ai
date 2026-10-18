import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import {
  ShieldCheck,
  Lock,
  Sliders,
  BarChart3,
  Megaphone,
  Check,
  RefreshCw,
  ExternalLink,
  Trash2,
  HardDrive,
  Info,
} from 'lucide-react';
import { useCookieConsent } from '../../context/CookieConsentContext';
import { useToast } from '../../context/ToastContext';
import type { CookieCategoriesConsent } from '../../types/cookieConsent';

export const PrivacyPreferencesSection: React.FC = () => {
  const {
    consent,
    hasResponded,
    savePreferences,
    acceptAll,
    rejectAll,
    resetConsent,
  } = useCookieConsent();
  const { showToast } = useToast();

  const [categories, setCategories] = useState<CookieCategoriesConsent>({
    necessary: true,
    analytics: false,
    functional: false,
    marketing: false,
  });

  useEffect(() => {
    if (consent) {
      setCategories({
        necessary: true,
        analytics: consent.categories.analytics,
        functional: consent.categories.functional,
        marketing: consent.categories.marketing,
      });
    }
  }, [consent]);

  const handleToggle = (key: keyof CookieCategoriesConsent) => {
    if (key === 'necessary') return;
    setCategories((prev) => ({
      ...prev,
      [key]: !prev[key],
    }));
  };

  const handleSave = () => {
    savePreferences(categories);
    showToast('Çerez ve gizlilik tercihleriniz başarıyla güncellendi.', 'success');
  };

  const handleReset = () => {
    if (window.confirm('Çerez izin tercihleriniz sıfırlanacak ve bilgilendirme penceresi yeniden açılacaktır. Onaylıyor musunuz?')) {
      resetConsent();
      showToast('Çerez izinleriniz sıfırlandı.', 'info');
    }
  };

  const formattedConsentDate = consent?.consentedAt
    ? new Date(consent.consentedAt).toLocaleDateString('tr-TR', {
        day: '2-digit',
        month: '2-digit',
        year: 'numeric',
        hour: '2-digit',
        minute: '2-digit',
      })
    : 'Henüz Belirtilmedi';

  return (
    <div className="bg-white border border-slate-200 rounded-2xl p-5 sm:p-6 shadow-sm space-y-6 animate-scale-in">
      {/* Header Info */}
      <div className="border-b border-slate-100 pb-4">
        <div className="flex items-center gap-2.5">
          <div className="p-2 bg-gradient-to-tr from-blue-600 to-indigo-600 rounded-xl text-white shadow-xs">
            <ShieldCheck className="w-5 h-5" />
          </div>
          <div>
            <h2 className="text-base font-semibold text-slate-900">
              Gizlilik ve Çerez Tercihleri (KVKK &amp; GDPR)
            </h2>
            <p className="text-xs text-slate-500 mt-0.5">
              Tarayıcınızda saklanan çerezleri, yerel depolama tercihlerini ve veri işleme izinlerinizi buradan denetleyin.
            </p>
          </div>
        </div>
      </div>

      {/* Consent Status Card */}
      <div className="p-4 rounded-xl bg-slate-50/70 border border-slate-200 flex flex-col sm:flex-row sm:items-center justify-between gap-3 text-xs">
        <div className="space-y-1">
          <div className="flex items-center gap-2">
            <span className="font-semibold text-slate-800">Mevcut Onay Durumu:</span>
            {hasResponded ? (
              <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-[11px] font-semibold bg-emerald-50 text-emerald-700 border border-emerald-200">
                <Check className="w-3 h-3" /> Tercihler Kayıtlı (v{consent?.version || '1.0'})
              </span>
            ) : (
              <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-[11px] font-semibold bg-amber-50 text-amber-700 border border-amber-200">
                Varsayılan (Onay Bekleniyor)
              </span>
            )}
          </div>
          <div className="text-slate-500">
            Son Güncelleme Tarihi: <strong className="text-slate-700">{formattedConsentDate}</strong>
          </div>
        </div>

        <div className="flex items-center gap-2 shrink-0">
          <button
            type="button"
            onClick={handleReset}
            className="px-3 py-1.5 rounded-lg text-xs font-medium text-slate-600 hover:text-slate-900 bg-white border border-slate-300 hover:bg-slate-50 shadow-2xs transition-colors cursor-pointer flex items-center gap-1"
            title="Tüm çerez onaylarını sıfırlayıp bildirimi tekrar gösterir"
          >
            <RefreshCw className="w-3 h-3 text-slate-500" />
            <span>İzinleri Sıfırla</span>
          </button>
        </div>
      </div>

      {/* Category Toggles List */}
      <div className="space-y-3.5">
        {/* 1. Zorunlu Çerezler */}
        <div className="p-4 rounded-xl border border-slate-200 bg-slate-50/50">
          <div className="flex items-start justify-between gap-3">
            <div className="flex items-start gap-3">
              <div className="w-8 h-8 rounded-lg bg-emerald-100/80 text-emerald-700 flex items-center justify-center shrink-0 mt-0.5">
                <Lock className="w-4 h-4" />
              </div>
              <div className="space-y-1">
                <div className="flex items-center gap-2">
                  <h3 className="text-sm font-semibold text-slate-900">Zorunlu Çerezler (Oturum &amp; Güvenlik)</h3>
                  <span className="text-[10px] font-bold px-2 py-0.5 rounded-full bg-emerald-100 text-emerald-800">
                    Kilitli / Zorunlu
                  </span>
                </div>
                <p className="text-xs text-slate-600 leading-relaxed">
                  Hesabınıza güvenli oturum açma (JWT token'ları), CSRF koruması ve yetkisiz erişim önleme amacıyla kullanılır. Kapatılamaz.
                </p>
                <div className="text-[11px] text-slate-500 font-mono pt-1">
                  Kullanılan: accessToken, refreshToken, dp_device_id, devpilot_cookie_consent_v1
                </div>
              </div>
            </div>

            <div className="shrink-0 pt-0.5">
              <div className="w-11 h-6 bg-emerald-600 rounded-full relative opacity-90 cursor-not-allowed">
                <div className="w-5 h-5 bg-white rounded-full absolute top-0.5 right-0.5 shadow-sm flex items-center justify-center">
                  <Lock className="w-2.5 h-2.5 text-emerald-700" />
                </div>
              </div>
            </div>
          </div>
        </div>

        {/* 2. İşlevsel Çerezler */}
        <div className="p-4 rounded-xl border border-slate-200 bg-white">
          <div className="flex items-start justify-between gap-3">
            <div className="flex items-start gap-3">
              <div className="w-8 h-8 rounded-lg bg-blue-100/80 text-blue-700 flex items-center justify-center shrink-0 mt-0.5">
                <Sliders className="w-4 h-4" />
              </div>
              <div className="space-y-1">
                <div className="flex items-center gap-2">
                  <h3 className="text-sm font-semibold text-slate-900">İşlevsel Tercihler (Model &amp; Arayüz)</h3>
                  <span className="text-[10px] font-medium px-2 py-0.5 rounded-full bg-slate-100 text-slate-600">
                    İsteğe Bağlı
                  </span>
                </div>
                <p className="text-xs text-slate-600 leading-relaxed">
                  Sohbette son seçilen yapay zekâ modelinizin (Gemini, Groq, Claude) ve arayüz kişiselleştirmelerinin hatırlanmasını sağlar.
                </p>
                <div className="text-[11px] text-slate-500 font-mono pt-1">
                  Kullanılan: devpilot_selected_model, devpilot_remember_me
                </div>
              </div>
            </div>

            <div className="shrink-0 pt-0.5">
              <button
                type="button"
                role="switch"
                aria-checked={categories.functional}
                onClick={() => handleToggle('functional')}
                className={`w-11 h-6 rounded-full relative transition-colors cursor-pointer ${
                  categories.functional ? 'bg-blue-600' : 'bg-slate-300'
                }`}
              >
                <div
                  className={`w-5 h-5 bg-white rounded-full absolute top-0.5 transition-transform shadow-sm ${
                    categories.functional ? 'right-0.5' : 'left-0.5'
                  }`}
                />
              </button>
            </div>
          </div>
        </div>

        {/* 3. Analitik ve Performans */}
        <div className="p-4 rounded-xl border border-slate-200 bg-white">
          <div className="flex items-start justify-between gap-3">
            <div className="flex items-start gap-3">
              <div className="w-8 h-8 rounded-lg bg-purple-100/80 text-purple-700 flex items-center justify-center shrink-0 mt-0.5">
                <BarChart3 className="w-4 h-4" />
              </div>
              <div className="space-y-1">
                <div className="flex items-center gap-2">
                  <h3 className="text-sm font-semibold text-slate-900">Analitik ve Sistem Performansı</h3>
                  <span className="text-[10px] font-medium px-2 py-0.5 rounded-full bg-slate-100 text-slate-600">
                    İsteğe Bağlı
                  </span>
                </div>
                <p className="text-xs text-slate-600 leading-relaxed">
                  Sistem yükü ve yanıt sürelerinin anonim olarak izlenmesini sağlar. Üçüncü taraf reklam ağlarına veri aktarımı yapılmaz.
                </p>
              </div>
            </div>

            <div className="shrink-0 pt-0.5">
              <button
                type="button"
                role="switch"
                aria-checked={categories.analytics}
                onClick={() => handleToggle('analytics')}
                className={`w-11 h-6 rounded-full relative transition-colors cursor-pointer ${
                  categories.analytics ? 'bg-blue-600' : 'bg-slate-300'
                }`}
              >
                <div
                  className={`w-5 h-5 bg-white rounded-full absolute top-0.5 transition-transform shadow-sm ${
                    categories.analytics ? 'right-0.5' : 'left-0.5'
                  }`}
                />
              </button>
            </div>
          </div>
        </div>

        {/* 4. Pazarlama ve Reklam */}
        <div className="p-4 rounded-xl border border-slate-200 bg-white">
          <div className="flex items-start justify-between gap-3">
            <div className="flex items-start gap-3">
              <div className="w-8 h-8 rounded-lg bg-amber-100/80 text-amber-700 flex items-center justify-center shrink-0 mt-0.5">
                <Megaphone className="w-4 h-4" />
              </div>
              <div className="space-y-1">
                <div className="flex items-center gap-2">
                  <h3 className="text-sm font-semibold text-slate-900">Pazarlama ve Reklam Çerezleri</h3>
                  <span className="text-[10px] font-medium px-2 py-0.5 rounded-full bg-slate-100 text-slate-600">
                    Pasif
                  </span>
                </div>
                <p className="text-xs text-slate-600 leading-relaxed">
                  DevPilot AI'da üçüncü taraf reklam ve izleyici script bulunmamaktadır. Varsayılan olarak kapalıdır.
                </p>
              </div>
            </div>

            <div className="shrink-0 pt-0.5">
              <button
                type="button"
                role="switch"
                aria-checked={categories.marketing}
                onClick={() => handleToggle('marketing')}
                className={`w-11 h-6 rounded-full relative transition-colors cursor-pointer ${
                  categories.marketing ? 'bg-blue-600' : 'bg-slate-300'
                }`}
              >
                <div
                  className={`w-5 h-5 bg-white rounded-full absolute top-0.5 transition-transform shadow-sm ${
                    categories.marketing ? 'right-0.5' : 'left-0.5'
                  }`}
                />
              </button>
            </div>
          </div>
        </div>
      </div>

      {/* Action Buttons */}
      <div className="pt-2 flex flex-col sm:flex-row items-stretch sm:items-center justify-between gap-3 border-t border-slate-100">
        <div className="flex items-center gap-3 text-xs text-slate-500">
          <Link
            to="/cookies"
            className="text-blue-600 hover:underline flex items-center gap-1 font-medium"
          >
            <span>Detaylı Çerez Politikası</span>
            <ExternalLink className="w-3 h-3" />
          </Link>
          <span>•</span>
          <Link
            to="/privacy"
            className="text-blue-600 hover:underline flex items-center gap-1 font-medium"
          >
            <span>Gizlilik Politikası</span>
            <ExternalLink className="w-3 h-3" />
          </Link>
        </div>

        <div className="flex items-center gap-2">
          <button
            type="button"
            onClick={handleSave}
            className="w-full sm:w-auto px-5 py-2.5 bg-blue-600 hover:bg-blue-700 text-white rounded-xl text-xs font-semibold shadow-xs flex items-center justify-center gap-1.5 transition-colors cursor-pointer"
          >
            <Check className="w-4 h-4" />
            <span>Tercihleri Kaydet</span>
          </button>
        </div>
      </div>

      {/* Local Storage & Transparency Info */}
      <div className="p-4 rounded-xl bg-slate-50 border border-slate-200/80 space-y-2">
        <div className="flex items-center gap-2 text-xs font-semibold text-slate-800">
          <HardDrive className="w-4 h-4 text-slate-600" />
          <span>Yerel Depolama ve Veri Şeffaflığı</span>
        </div>
        <p className="text-xs text-slate-600 leading-relaxed">
          Tüm AI API anahtarlarınız (BYOK) veritabanımızda <strong>AES-256</strong> algoritmasıyla şifreli olarak tutulur ve asla düz metin olarak depolanmaz. Dokümanlarınız ve sohbetleriniz yalnızca sizin hesabınıza özel olarak indekslenir; üçüncü şahıslara veya reklam ortaklarına asla satılmaz veya aktarılmaz.
        </p>
      </div>
    </div>
  );
};
