import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import {
  ShieldCheck,
  Lock,
  BarChart3,
  Sliders,
  Megaphone,
  Check,
  X,
  ExternalLink,
  Info,
} from 'lucide-react';
import { useCookieConsent } from '../../context/CookieConsentContext';
import { DEVPILOT_COOKIES } from '../../types/cookieConsent';
import type { CookieCategoriesConsent } from '../../types/cookieConsent';

export const CookiePreferencesModal: React.FC = () => {
  const {
    consent,
    isPreferencesModalOpen,
    closePreferences,
    savePreferences,
    acceptAll,
    rejectAll,
  } = useCookieConsent();

  const [formCategories, setFormCategories] = useState<CookieCategoriesConsent>({
    necessary: true,
    analytics: false,
    functional: false,
    marketing: false,
  });

  const [activeTab, setActiveTab] = useState<'all' | 'necessary' | 'analytics' | 'functional' | 'marketing'>('all');

  useEffect(() => {
    if (consent) {
      setFormCategories({
        necessary: true,
        analytics: consent.categories.analytics,
        functional: consent.categories.functional,
        marketing: consent.categories.marketing,
      });
    } else {
      setFormCategories({
        necessary: true,
        analytics: false,
        functional: false,
        marketing: false,
      });
    }
  }, [consent, isPreferencesModalOpen]);

  // Escape key closes modal
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape' && isPreferencesModalOpen) {
        closePreferences();
      }
    };
    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [isPreferencesModalOpen, closePreferences]);

  if (!isPreferencesModalOpen) return null;

  const handleToggle = (key: keyof CookieCategoriesConsent) => {
    if (key === 'necessary') return; // Cannot toggle necessary
    setFormCategories((prev) => ({
      ...prev,
      [key]: !prev[key],
    }));
  };

  const handleSave = () => {
    savePreferences(formCategories);
  };

  return (
    <div
      role="dialog"
      aria-modal="true"
      aria-labelledby="cookie-preferences-title"
      className="fixed inset-0 z-50 overflow-y-auto"
    >
      {/* Backdrop */}
      <div
        className="fixed inset-0 bg-slate-900/60 backdrop-blur-xs transition-opacity animate-fade-in"
        onClick={closePreferences}
      />

      <div className="flex min-h-full items-center justify-center p-3 sm:p-5 text-center">
        <div
          className="relative w-full max-w-2xl transform overflow-hidden rounded-2xl bg-white border border-slate-200 p-5 sm:p-7 text-left shadow-2xl transition-all animate-scale-in max-h-[92vh] flex flex-col"
          onClick={(e) => e.stopPropagation()}
        >
          {/* Header */}
          <div className="flex items-start justify-between pb-4 border-b border-slate-100 shrink-0">
            <div className="flex items-center gap-3">
              <div className="w-10 h-10 rounded-xl bg-blue-50 border border-blue-200 text-blue-600 flex items-center justify-center shrink-0">
                <Sliders className="w-5 h-5" />
              </div>
              <div>
                <h2
                  id="cookie-preferences-title"
                  className="text-base sm:text-lg font-bold text-slate-900 tracking-tight"
                >
                  Gizlilik ve Çerez Tercihlerini Yönet
                </h2>
                <p className="text-xs text-slate-500 mt-0.5">
                  DevPilot AI üzerinde hangi çerez ve depolama mekanizmalarına izin vereceğinizi seçin.
                </p>
              </div>
            </div>

            <button
              type="button"
              onClick={closePreferences}
              className="p-1.5 rounded-lg text-slate-400 hover:text-slate-700 hover:bg-slate-100 transition-colors cursor-pointer"
              title="Kapat"
            >
              <X className="w-5 h-5" />
            </button>
          </div>

          {/* Quick Notice */}
          <div className="my-3.5 p-3 rounded-xl bg-slate-50 border border-slate-200/80 flex items-start gap-2.5 text-xs text-slate-600 shrink-0">
            <Info className="w-4 h-4 text-blue-600 shrink-0 mt-0.5" />
            <div className="leading-relaxed">
              Kişisel gizliliğinize azami saygı duyuyoruz. 6698 sayılı KVKK ve GDPR uyumluluğu
              gereğince, zorunlu güvenlik çerezleri dışındaki tüm çerezler açık rızanıza bağlıdır.
            </div>
          </div>

          {/* Scrollable Categories Body */}
          <div className="flex-1 overflow-y-auto space-y-3.5 pr-1 py-1">
            {/* 1. Zorunlu Çerezler */}
            <div className="p-4 rounded-xl border border-slate-200 bg-slate-50/60">
              <div className="flex items-start justify-between gap-3">
                <div className="flex items-start gap-3">
                  <div className="w-8 h-8 rounded-lg bg-emerald-100/70 text-emerald-700 flex items-center justify-center shrink-0 mt-0.5">
                    <ShieldCheck className="w-4 h-4" />
                  </div>
                  <div>
                    <div className="flex items-center gap-2">
                      <h3 className="text-sm font-semibold text-slate-900">
                        Zorunlu ve Güvenlik Çerezleri
                      </h3>
                      <span className="text-[10px] font-semibold px-2 py-0.5 rounded-full bg-emerald-100 text-emerald-800 border border-emerald-200 flex items-center gap-1">
                        <Lock className="w-2.5 h-2.5" /> Her Zaman Etkin
                      </span>
                    </div>
                    <p className="text-xs text-slate-600 mt-1 leading-relaxed">
                      Sisteme güvenli biçimde giriş yapabilmeniz, kimliğinizin doğrulanması,
                      oturumun yenilenmesi ve CSRF/brute-force saldırılarına karşı korunmanız için
                      teknik olarak zorunludur. Bu çerezler devre dışı bırakılamaz.
                    </p>
                  </div>
                </div>

                {/* Locked Switch */}
                <div className="shrink-0 pt-0.5">
                  <div
                    aria-checked="true"
                    role="switch"
                    aria-disabled="true"
                    className="w-11 h-6 bg-emerald-600 rounded-full relative cursor-not-allowed opacity-90 transition-colors"
                  >
                    <div className="w-5 h-5 bg-white rounded-full absolute top-0.5 right-0.5 shadow-sm flex items-center justify-center">
                      <Lock className="w-2.5 h-2.5 text-emerald-700" />
                    </div>
                  </div>
                </div>
              </div>

              {/* Cookie Inventory Details */}
              <div className="mt-3 pt-3 border-t border-slate-200/80 text-[11px] text-slate-500 space-y-1">
                <span className="font-semibold text-slate-700 block">Kullanılan Öğeler:</span>
                <ul className="list-disc list-inside space-y-0.5 text-slate-600">
                  <li><code className="text-slate-800 font-mono">accessToken</code>: Güvenli JWT oturum doğrulama (HttpOnly, 60 dk)</li>
                  <li><code className="text-slate-800 font-mono">refreshToken</code>: Oturum yenileme belirteci (HttpOnly, 30 gün)</li>
                  <li><code className="text-slate-800 font-mono">dp_device_id</code>: Güvenlik ve yetkisiz oturum engelleme kimliği (1 yıl)</li>
                  <li><code className="text-slate-800 font-mono">devpilot_cookie_consent_v1</code>: Çerez izin tercih kaydı (1 yıl)</li>
                </ul>
              </div>
            </div>

            {/* 2. İşlevsel Çerezler */}
            <div className="p-4 rounded-xl border border-slate-200 bg-white hover:border-slate-300 transition-colors">
              <div className="flex items-start justify-between gap-3">
                <div className="flex items-start gap-3">
                  <div className="w-8 h-8 rounded-lg bg-blue-100/70 text-blue-700 flex items-center justify-center shrink-0 mt-0.5">
                    <Sliders className="w-4 h-4" />
                  </div>
                  <div>
                    <div className="flex items-center gap-2">
                      <h3 className="text-sm font-semibold text-slate-900">
                        İşlevsel Çerezler ve Kullanıcı Tercihleri
                      </h3>
                      <span className="text-[10px] font-medium px-2 py-0.5 rounded-full bg-slate-100 text-slate-600 border border-slate-200">
                        İsteğe Bağlı
                      </span>
                    </div>
                    <p className="text-xs text-slate-600 mt-1 leading-relaxed">
                      Sohbet ekranında son seçtiğiniz yapay zekâ modelinin (OpenAI, Gemini, Groq, Claude)
                      hatırlanması ve arayüz kişiselleştirmelerinizin korunması için yerel olarak saklanır.
                    </p>
                  </div>
                </div>

                {/* Toggle Switch */}
                <div className="shrink-0 pt-0.5">
                  <button
                    type="button"
                    role="switch"
                    aria-checked={formCategories.functional}
                    onClick={() => handleToggle('functional')}
                    className={`w-11 h-6 rounded-full relative transition-colors cursor-pointer ${
                      formCategories.functional ? 'bg-blue-600' : 'bg-slate-300'
                    }`}
                  >
                    <div
                      className={`w-5 h-5 bg-white rounded-full absolute top-0.5 transition-transform shadow-sm ${
                        formCategories.functional ? 'right-0.5' : 'left-0.5'
                      }`}
                    />
                  </button>
                </div>
              </div>

              <div className="mt-3 pt-3 border-t border-slate-100 text-[11px] text-slate-500">
                <span className="font-semibold text-slate-700 block">Kullanılan Öğeler:</span>
                <span className="text-slate-600">
                  <code className="text-slate-800 font-mono">devpilot_selected_model</code>,{' '}
                  <code className="text-slate-800 font-mono">devpilot_remember_me</code>
                </span>
              </div>
            </div>

            {/* 3. Analitik ve Performans */}
            <div className="p-4 rounded-xl border border-slate-200 bg-white hover:border-slate-300 transition-colors">
              <div className="flex items-start justify-between gap-3">
                <div className="flex items-start gap-3">
                  <div className="w-8 h-8 rounded-lg bg-purple-100/70 text-purple-700 flex items-center justify-center shrink-0 mt-0.5">
                    <BarChart3 className="w-4 h-4" />
                  </div>
                  <div>
                    <div className="flex items-center gap-2">
                      <h3 className="text-sm font-semibold text-slate-900">
                        Analitik ve Performans
                      </h3>
                      <span className="text-[10px] font-medium px-2 py-0.5 rounded-full bg-slate-100 text-slate-600 border border-slate-200">
                        İsteğe Bağlı
                      </span>
                    </div>
                    <p className="text-xs text-slate-600 mt-1 leading-relaxed">
                      Sayfa yüklenme hızları, sistem hata oranları ve sunucu yanıt sürelerini anonim
                      olarak ölçümlememize yardımcı olur. Üçüncü taraf reklam ağlarıyla paylaşılmaz.
                    </p>
                  </div>
                </div>

                {/* Toggle Switch */}
                <div className="shrink-0 pt-0.5">
                  <button
                    type="button"
                    role="switch"
                    aria-checked={formCategories.analytics}
                    onClick={() => handleToggle('analytics')}
                    className={`w-11 h-6 rounded-full relative transition-colors cursor-pointer ${
                      formCategories.analytics ? 'bg-blue-600' : 'bg-slate-300'
                    }`}
                  >
                    <div
                      className={`w-5 h-5 bg-white rounded-full absolute top-0.5 transition-transform shadow-sm ${
                        formCategories.analytics ? 'right-0.5' : 'left-0.5'
                      }`}
                    />
                  </button>
                </div>
              </div>
            </div>

            {/* 4. Pazarlama ve Reklam */}
            <div className="p-4 rounded-xl border border-slate-200 bg-white hover:border-slate-300 transition-colors">
              <div className="flex items-start justify-between gap-3">
                <div className="flex items-start gap-3">
                  <div className="w-8 h-8 rounded-lg bg-amber-100/70 text-amber-700 flex items-center justify-center shrink-0 mt-0.5">
                    <Megaphone className="w-4 h-4" />
                  </div>
                  <div>
                    <div className="flex items-center gap-2">
                      <h3 className="text-sm font-semibold text-slate-900">
                        Pazarlama ve Hedefleme Çerezleri
                      </h3>
                      <span className="text-[10px] font-medium px-2 py-0.5 rounded-full bg-slate-100 text-slate-600 border border-slate-200">
                        Pasif
                      </span>
                    </div>
                    <p className="text-xs text-slate-600 mt-1 leading-relaxed">
                      DevPilot AI platformunda hiçbir üçüncü taraf reklam veya çapraz site izleyicisi
                      bulunmamaktadır. Bu kategori şeffaflık amacıyla sunulmuş olup varsayılan olarak kapalıdır.
                    </p>
                  </div>
                </div>

                {/* Toggle Switch */}
                <div className="shrink-0 pt-0.5">
                  <button
                    type="button"
                    role="switch"
                    aria-checked={formCategories.marketing}
                    onClick={() => handleToggle('marketing')}
                    className={`w-11 h-6 rounded-full relative transition-colors cursor-pointer ${
                      formCategories.marketing ? 'bg-blue-600' : 'bg-slate-300'
                    }`}
                  >
                    <div
                      className={`w-5 h-5 bg-white rounded-full absolute top-0.5 transition-transform shadow-sm ${
                        formCategories.marketing ? 'right-0.5' : 'left-0.5'
                      }`}
                    />
                  </button>
                </div>
              </div>
            </div>
          </div>

          {/* Footer Actions */}
          <div className="pt-4 mt-2 border-t border-slate-100 flex flex-col sm:flex-row items-stretch sm:items-center justify-between gap-3 shrink-0">
            <div className="flex items-center gap-3 text-xs text-slate-500">
              <Link
                to="/cookies"
                onClick={closePreferences}
                className="hover:text-blue-600 hover:underline flex items-center gap-1"
              >
                <span>Çerez Politikası</span>
                <ExternalLink className="w-3 h-3" />
              </Link>
              <span>•</span>
              <Link
                to="/privacy"
                onClick={closePreferences}
                className="hover:text-blue-600 hover:underline flex items-center gap-1"
              >
                <span>Gizlilik Politikası</span>
                <ExternalLink className="w-3 h-3" />
              </Link>
            </div>

            <div className="flex flex-col sm:flex-row items-stretch sm:items-center gap-2">
              <button
                type="button"
                onClick={rejectAll}
                className="px-3.5 py-2 rounded-xl text-xs font-semibold text-slate-700 bg-white border border-slate-300 hover:bg-slate-50 shadow-2xs transition-colors cursor-pointer"
              >
                Tümünü Reddet
              </button>

              <button
                type="button"
                onClick={acceptAll}
                className="px-3.5 py-2 rounded-xl text-xs font-semibold text-slate-700 bg-slate-100 hover:bg-slate-200 transition-colors cursor-pointer"
              >
                Tümünü Kabul Et
              </button>

              <button
                type="button"
                onClick={handleSave}
                className="px-4 py-2 rounded-xl text-xs font-semibold text-white bg-blue-600 hover:bg-blue-700 shadow-xs transition-colors cursor-pointer flex items-center justify-center gap-1.5"
              >
                <Check className="w-3.5 h-3.5" />
                <span>Seçimlerimi Kaydet</span>
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};
