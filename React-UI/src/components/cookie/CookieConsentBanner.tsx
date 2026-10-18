import React from 'react';
import { Link } from 'react-router-dom';
import { ShieldCheck, Sliders, Check, X, ExternalLink } from 'lucide-react';
import { useCookieConsent } from '../../context/CookieConsentContext';
import { Button } from '../ui/Button';

export const CookieConsentBanner: React.FC = () => {
  const { isBannerVisible, acceptAll, rejectAll, openPreferences } = useCookieConsent();

  if (!isBannerVisible) return null;

  return (
    <aside
      role="dialog"
      aria-label="Çerez ve Gizlilik Bildirimi"
      aria-describedby="cookie-consent-desc"
      className="fixed bottom-0 inset-x-0 z-50 p-3 sm:p-5 pointer-events-none animate-slide-up"
    >
      <div className="max-w-4xl mx-auto bg-white/95 backdrop-blur-md border border-slate-200/90 rounded-2xl shadow-2xl p-4 sm:p-6 pointer-events-auto transition-all">
        <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 sm:gap-6">
          {/* Content & Icon */}
          <div className="flex items-start gap-3.5">
            <div className="w-10 h-10 rounded-xl bg-blue-50 border border-blue-200 text-blue-600 flex items-center justify-center shrink-0 mt-0.5">
              <ShieldCheck className="w-5 h-5" />
            </div>
            <div className="space-y-1 max-w-2xl">
              <div className="flex items-center gap-2 flex-wrap">
                <h3 className="text-sm font-bold text-slate-900 tracking-tight">
                  Çerez ve Gizlilik Tercihleriniz
                </h3>
                <span className="text-[11px] font-medium px-2 py-0.5 rounded-full bg-slate-100 text-slate-600 border border-slate-200">
                  KVKK &amp; GDPR Uyumlu
                </span>
              </div>
              <p
                id="cookie-consent-desc"
                className="text-xs text-slate-600 leading-relaxed"
              >
                DevPilot AI, oturum güvenliğinizi sağlamak, tercihlerinizi hatırlamak ve sistem
                performansını optimize etmek amacıyla birinci taraf zorunlu ve işlevsel çerezler
                kullanmaktadır. İsteğe bağlı çerezleri dilediğiniz zaman reddedebilir veya özelleştirebilirsiniz.
              </p>
              <div className="pt-1 flex items-center gap-3 text-xs">
                <Link
                  to="/cookies"
                  className="inline-flex items-center gap-1 font-medium text-blue-600 hover:text-blue-700 hover:underline"
                >
                  <span>Çerez Politikası</span>
                  <ExternalLink className="w-3 h-3" />
                </Link>
                <span className="text-slate-300">•</span>
                <Link
                  to="/privacy"
                  className="inline-flex items-center gap-1 font-medium text-blue-600 hover:text-blue-700 hover:underline"
                >
                  <span>Gizlilik Politikası</span>
                  <ExternalLink className="w-3 h-3" />
                </Link>
              </div>
            </div>
          </div>

          {/* Action Buttons (Equal prominence for Accept & Reject to avoid dark patterns) */}
          <div className="flex flex-col sm:flex-row items-stretch sm:items-center gap-2 sm:gap-2.5 shrink-0 pt-2 md:pt-0 border-t md:border-t-0 border-slate-100">
            <button
              type="button"
              onClick={openPreferences}
              className="inline-flex items-center justify-center gap-1.5 px-3.5 py-2 rounded-xl text-xs font-semibold text-slate-700 bg-slate-100 hover:bg-slate-200/80 transition-colors cursor-pointer"
              title="Kategorileri incele ve özelleştir"
            >
              <Sliders className="w-3.5 h-3.5 text-slate-500" />
              <span>Özelleştir</span>
            </button>

            <button
              type="button"
              onClick={rejectAll}
              className="inline-flex items-center justify-center gap-1.5 px-4 py-2 rounded-xl text-xs font-semibold text-slate-700 bg-white border border-slate-300 hover:bg-slate-50 hover:border-slate-400 shadow-2xs transition-colors cursor-pointer"
              title="Yalnızca zorunlu çerezleri kullan"
            >
              <X className="w-3.5 h-3.5 text-slate-500" />
              <span>Tümünü Reddet</span>
            </button>

            <button
              type="button"
              onClick={acceptAll}
              className="inline-flex items-center justify-center gap-1.5 px-4 py-2 rounded-xl text-xs font-semibold text-white bg-blue-600 hover:bg-blue-700 active:bg-blue-800 shadow-xs transition-colors cursor-pointer"
              title="Tüm çerez kategorilerini kabul et"
            >
              <Check className="w-3.5 h-3.5" />
              <span>Tümünü Kabul Et</span>
            </button>
          </div>
        </div>
      </div>
    </aside>
  );
};
