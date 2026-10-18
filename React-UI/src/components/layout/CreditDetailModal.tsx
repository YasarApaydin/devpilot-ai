import React from 'react';
import { useNavigate } from 'react-router-dom';
import { X, Sparkles, Key, Clock, ShieldCheck, CheckCircle2 } from 'lucide-react';
import { useCredit } from '../../context/CreditContext';

interface CreditDetailModalProps {
  isOpen: boolean;
  onClose: () => void;
}

export const CreditDetailModal: React.FC<CreditDetailModalProps> = ({ isOpen, onClose }) => {
  const navigate = useNavigate();
  const {
    creditsRemaining,
    maxCredits,
    usedCredits,
    timeUntilResetFormatted,
    activeProvider,
    isUnlimited,
    isByokActive,
  } = useCredit();

  if (!isOpen) return null;

  const isCurrentByok = activeProvider !== 'system';

  const handleNavigateToKeys = () => {
    onClose();
    navigate('/app/profile?tab=ai-keys');
  };

  const percentage = Math.max(0, Math.min(100, Math.round((creditsRemaining / maxCredits) * 100)));

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/50 backdrop-blur-xs animate-fade-in select-none">
      <div className="w-full max-w-sm sm:max-w-md bg-white rounded-2xl shadow-2xl border border-slate-200 overflow-hidden animate-scale-in">
        {/* Header */}
        <div className="p-4 sm:p-5 border-b border-slate-100 flex items-center justify-between bg-gradient-to-r from-blue-50/70 to-indigo-50/70">
          <div className="flex items-center gap-2.5">
            <div className="w-8 h-8 rounded-xl bg-blue-600 text-white flex items-center justify-center shadow-xs">
              <Sparkles className="w-4 h-4" />
            </div>
            <div>
              <h3 className="text-sm font-bold text-slate-900">Kredi ve Kota Durumu</h3>
              <p className="text-[11px] text-slate-500">DevPilot AI günlük kullanım bilgisi</p>
            </div>
          </div>
          <button
            onClick={onClose}
            className="p-1.5 rounded-lg text-slate-400 hover:text-slate-700 hover:bg-white/80 transition-colors"
          >
            <X className="w-4 h-4" />
          </button>
        </div>

        {/* Content */}
        <div className="p-4 sm:p-5 space-y-4 text-xs">
          {/* Active Mode Banner */}
          <div className={`p-3 rounded-xl border flex items-start gap-2.5 ${
            isCurrentByok
              ? 'bg-emerald-50/80 border-emerald-200 text-emerald-900'
              : 'bg-blue-50/80 border-blue-200 text-blue-900'
          }`}>
            <ShieldCheck className={`w-4 h-4 shrink-0 mt-0.5 ${isCurrentByok ? 'text-emerald-600' : 'text-blue-600'}`} />
            <div>
              <span className="font-semibold block">
                {isCurrentByok ? 'Harici BYOK Modu Aktif' : 'Varsayılan Sistem Modeli Aktif'}
              </span>
              <p className="text-[11px] mt-0.5 opacity-90 leading-relaxed">
                {isCurrentByok
                  ? `Seçtiğiniz ${activeProvider.toUpperCase()} modeli ile sınırsız mesajlaşabilirsiniz. Günlük sistem kotanız eksilmez.`
                  : 'DevPilot varsayılan Türkçe LPU modeli devrede. Her gün 15 adet ücretsiz mesaj tanımlanır.'}
              </p>
            </div>
          </div>

          {/* Progress Card (For System Quota) */}
          <div className="p-3.5 rounded-xl border border-slate-200 bg-slate-50/60 space-y-2.5">
            <div className="flex items-center justify-between">
              <span className="text-slate-600 font-medium">Günlük Kalan Kredi:</span>
              {isUnlimited ? (
                <span className="font-bold text-emerald-600">Sınırsız</span>
              ) : (
                <span className="font-bold text-slate-900 text-sm">
                  <span className={creditsRemaining > 3 ? 'text-blue-600' : creditsRemaining > 0 ? 'text-amber-600' : 'text-rose-600'}>
                    {creditsRemaining}
                  </span>{' '}
                  / {maxCredits}
                </span>
              )}
            </div>

            {/* Progress Bar */}
            {!isUnlimited && (
              <div className="w-full bg-slate-200 rounded-full h-2 overflow-hidden">
                <div
                  className={`h-full transition-all duration-300 rounded-full ${
                    creditsRemaining > 3
                      ? 'bg-blue-600'
                      : creditsRemaining > 0
                      ? 'bg-amber-500'
                      : 'bg-rose-500'
                  }`}
                  style={{ width: `${percentage}%` }}
                />
              </div>
            )}

            {/* Reset Countdown */}
            {!isUnlimited && (
              <div className="flex items-center justify-between text-[11px] text-slate-500 pt-1 border-t border-slate-200/60">
                <span className="flex items-center gap-1">
                  <Clock className="w-3.5 h-3.5 text-slate-400" />
                  Yenilenme süresi:
                </span>
                <span className="font-semibold text-slate-700">
                  {timeUntilResetFormatted || '24 saatte bir'}
                </span>
              </div>
            )}
          </div>

          {/* BYOK Promotion / Upgrade Box */}
          <div className="p-3.5 rounded-xl border border-slate-200 bg-white space-y-2">
            <div className="flex items-center gap-1.5 text-slate-800 font-semibold text-xs">
              <Key className="w-3.5 h-3.5 text-blue-600" />
              <span>Sınırsız Mesajlaşma (BYOK)</span>
            </div>
            <p className="text-[11px] text-slate-500 leading-relaxed">
              OpenAI, Google Gemini, DeepSeek, Mistral veya GroqCloud API anahtarınızı profilinizden ekleyerek hiçbir günlük limit olmadan sınırsız sohbet edebilirsiniz.
            </p>
            <button
              type="button"
              onClick={handleNavigateToKeys}
              className="w-full py-2 px-3 mt-1 rounded-xl bg-slate-900 hover:bg-slate-800 text-white font-medium text-xs transition-colors flex items-center justify-center gap-2 cursor-pointer shadow-xs"
            >
              <Key className="w-3.5 h-3.5" />
              <span>API Anahtarlarını Yönet (BYOK)</span>
            </button>
          </div>
        </div>

        {/* Footer */}
        <div className="p-3 border-t border-slate-100 bg-slate-50 text-right">
          <button
            onClick={onClose}
            className="px-4 py-1.5 rounded-xl bg-white border border-slate-200 text-slate-700 font-medium text-xs hover:bg-slate-100 transition-colors cursor-pointer"
          >
            Kapat
          </button>
        </div>
      </div>
    </div>
  );
};
