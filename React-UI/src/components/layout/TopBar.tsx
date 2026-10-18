import React, { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import {
  Menu,
  BookOpen,
  User as UserIcon,
  LogOut,
  ShieldCheck,
  Sparkles,
} from 'lucide-react';
import { useAuth } from '../../context/AuthContext';
import { useToast } from '../../context/ToastContext';
import { useCredit } from '../../context/CreditContext';
import { CreditDetailModal } from './CreditDetailModal';

export interface TopBarProps {
  onToggleSidebar: () => void;
  onToggleLibrary: () => void;
  isSidebarOpen: boolean;
  isLibraryOpen: boolean;
}

export const TopBar: React.FC<TopBarProps> = ({
  onToggleSidebar,
  onToggleLibrary,
  isLibraryOpen,
}) => {
  const { user, logout } = useAuth();
  const { showToast } = useToast();
  const {
    creditsRemaining,
    maxCredits,
    timeUntilResetFormatted,
    isUnlimited,
    activeProvider,
  } = useCredit();

  const [isCreditModalOpen, setIsCreditModalOpen] = useState(false);

  const isCurrentByok = activeProvider !== 'system';

  const handleLogout = async () => {
    try {
      await logout();
      showToast('Oturum başarıyla kapatıldı.', 'info');
    } catch {
      showToast('Çıkış yapıldı.', 'info');
    } finally {
      window.location.href = '/login';
    }
  };

  return (
    <>
      <header className="h-14 border-b border-slate-200 bg-white px-2 sm:px-4 flex items-center justify-between shrink-0 z-30 shadow-2xs">
        {/* Left: Mobile hamburger & Brand */}
        <div className="flex items-center gap-1.5 sm:gap-3 shrink-0">
          {/* Mobile Sidebar toggle button */}
          <button
            onClick={onToggleSidebar}
            className="lg:hidden p-1.5 rounded-lg text-slate-500 hover:text-slate-800 hover:bg-slate-100 transition-colors"
            title="Sohbetleri Aç/Kapat"
            aria-label="Sohbetleri Aç/Kapat"
          >
            <Menu className="w-5 h-5" />
          </button>

          <Link to="/app" className="flex items-center gap-2 group shrink-0">
            <img
              src="/app-logo.png"
              alt="DevPilot AI"
              className="w-7 h-7 sm:w-8 sm:h-8 rounded-full object-cover shadow-xs border border-slate-200 transition-transform group-hover:scale-105 shrink-0"
            />
            <div className="flex items-center font-bold text-slate-900 text-xs sm:text-sm">
              <span>DevPilot</span>
              <span className="text-blue-600 font-light ml-0.5">AI</span>
            </div>
          </Link>
        </div>

        {/* Middle: Responsive Credit / Token Status Badge */}
        <div className="flex items-center justify-center px-1">
          <button
            type="button"
            onClick={() => setIsCreditModalOpen(true)}
            className={`flex items-center gap-1 sm:gap-2 px-2 py-1 sm:px-3 sm:py-1.5 rounded-full border text-[11px] sm:text-xs font-medium shadow-2xs select-none transition-all cursor-pointer hover:shadow-xs ${
              isCurrentByok
                ? 'bg-emerald-50/90 border-emerald-200 text-emerald-800 hover:bg-emerald-100/80'
                : 'bg-slate-50 hover:bg-slate-100 border-slate-200 text-slate-700'
            }`}
            title={
              isCurrentByok
                ? 'BYOK devrede. Harici API anahtarınız kullanılıyor, kota sınırsızdır.'
                : isUnlimited
                ? 'Sınırsız paket devrede.'
                : `Günlük Kredi: ${creditsRemaining} / ${maxCredits} kaldı. Sıfırlanma: ${timeUntilResetFormatted}. Detaylar için tıklayın.`
            }
          >
            <span
              className={`w-2 h-2 rounded-full shrink-0 ${
                isCurrentByok || isUnlimited || creditsRemaining > 3
                  ? 'bg-emerald-500'
                  : creditsRemaining > 0
                  ? 'bg-amber-500'
                  : 'bg-rose-500'
              } ${isCurrentByok || isUnlimited || creditsRemaining > 0 ? 'animate-pulse' : ''}`}
            />

            {isCurrentByok ? (
              <>
                <span className="font-semibold text-emerald-700 whitespace-nowrap">BYOK Aktif</span>
                <span className="text-[10px] text-emerald-600 font-normal hidden sm:inline">(Sınırsız)</span>
              </>
            ) : isUnlimited ? (
              <span className="font-semibold text-emerald-600">Sınırsız</span>
            ) : (
              <>
                <span className="text-slate-500 text-[10px] sm:text-[11px] hidden md:inline">Kredi:</span>
                <span
                  className={`font-semibold tracking-tight whitespace-nowrap ${
                    creditsRemaining > 3
                      ? 'text-blue-600'
                      : creditsRemaining > 0
                      ? 'text-amber-600'
                      : 'text-rose-600'
                  }`}
                >
                  {creditsRemaining} / {maxCredits}
                </span>
                {creditsRemaining === 0 && (
                  <span className="text-[9px] text-rose-500 font-normal hidden lg:inline">
                    ({timeUntilResetFormatted})
                  </span>
                )}
              </>
            )}
          </button>
        </div>

        {/* Right: User Actions and Secondary Buttons */}
        <div className="flex items-center gap-1 sm:gap-2 shrink-0">
          {/* Toggle Library on tablet / laptop */}
          <button
            onClick={onToggleLibrary}
            className={`p-1.5 rounded-lg text-xs font-medium flex items-center gap-1.5 transition-colors hidden sm:flex xl:hidden ${
              isLibraryOpen
                ? 'bg-blue-50 text-blue-700 border border-blue-200'
                : 'text-slate-600 hover:text-slate-900 hover:bg-slate-100'
            }`}
            title="Doküman Kütüphanesini Aç/Kapat"
          >
            <BookOpen className="w-4 h-4" />
            <span className="hidden md:inline">Library</span>
          </button>

          {/* Privacy & Cookie Preferences shortcut */}
          <Link
            to="/app/profile?tab=privacy"
            className="p-1.5 rounded-lg text-slate-400 hover:text-blue-600 hover:bg-slate-100 transition-colors hidden sm:flex"
            title="Gizlilik ve Çerez Tercihleri"
          >
            <ShieldCheck className="w-4 h-4" />
          </Link>

          {/* Profile Link */}
          <Link
            to="/app/profile"
            className="flex items-center gap-1.5 p-1 rounded-lg hover:bg-slate-100 text-slate-700 transition-colors"
            title="Kullanıcı Profili"
          >
            <div className="w-7 h-7 rounded-lg bg-slate-100 border border-slate-200 flex items-center justify-center text-xs font-semibold text-blue-600 shrink-0">
              {user?.firstName ? user.firstName[0].toUpperCase() : <UserIcon className="w-3.5 h-3.5" />}
            </div>
            <span className="text-xs font-medium hidden md:inline truncate max-w-[90px]">
              {user?.firstName}
            </span>
          </Link>

          {/* Logout */}
          <button
            onClick={handleLogout}
            className="p-1.5 rounded-lg text-slate-400 hover:text-rose-600 hover:bg-rose-50 transition-colors cursor-pointer"
            title="Çıkış Yap"
          >
            <LogOut className="w-4 h-4" />
          </button>
        </div>
      </header>

      {/* Credit Details Interactive Popover/Modal */}
      <CreditDetailModal
        isOpen={isCreditModalOpen}
        onClose={() => setIsCreditModalOpen(false)}
      />
    </>
  );
};
