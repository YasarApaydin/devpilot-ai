import React from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import {
  MessageSquare,
  Plus,
  Shield,
  BookOpen,
  User as UserIcon,
} from 'lucide-react';

export interface MobileFooterProps {
  onToggleSidebar: () => void;
  onToggleLibrary: () => void;
  onNewConversation?: () => void;
  onOpenSecurityScan?: () => void;
}

export const MobileFooter: React.FC<MobileFooterProps> = ({
  onToggleSidebar,
  onToggleLibrary,
  onNewConversation,
  onOpenSecurityScan,
}) => {
  const navigate = useNavigate();
  const location = useLocation();

  const isProfileActive = location.pathname.startsWith('/app/profile');
  const isChatActive = location.pathname.startsWith('/app/chat') || location.pathname === '/app';

  const handleSecurityClick = () => {
    if (onOpenSecurityScan) {
      onOpenSecurityScan();
    } else {
      window.dispatchEvent(new CustomEvent('devpilot:open-security-scan'));
    }
  };

  const handleNewChatClick = () => {
    if (onNewConversation) {
      onNewConversation();
    } else {
      navigate('/app');
    }
  };

  return (
    <nav
      className="lg:hidden fixed bottom-0 left-0 right-0 z-40 bg-white/95 backdrop-blur-md border-t border-slate-200/90 shadow-[0_-4px_20px_rgba(0,0,0,0.06)] px-2 py-1 pb-[max(0.4rem,env(safe-area-inset-bottom))] select-none"
      aria-label="Mobil Gezinme Menüsü"
    >
      <div className="flex items-center justify-around max-w-lg mx-auto">
        {/* 1. Sohbetler Geçmişi */}
        <button
          type="button"
          onClick={onToggleSidebar}
          className="flex flex-col items-center justify-center py-1 px-2.5 rounded-xl text-slate-500 hover:text-slate-900 active:scale-95 transition-all cursor-pointer"
          title="Kayıtlı Sohbetler"
        >
          <MessageSquare className="w-5 h-5" />
          <span className="text-[10px] font-medium mt-0.5">Sohbetler</span>
        </button>

        {/* 2. Yeni Sohbet */}
        <button
          type="button"
          onClick={handleNewChatClick}
          className="flex flex-col items-center justify-center py-1 px-2.5 rounded-xl text-blue-600 hover:text-blue-700 active:scale-95 transition-all cursor-pointer"
          title="Yeni Sohbet Başlat"
        >
          <div className="w-7 h-7 rounded-full bg-blue-50 text-blue-600 flex items-center justify-center shadow-xs">
            <Plus className="w-4 h-4 stroke-[2.5]" />
          </div>
          <span className="text-[10px] font-semibold mt-0.5 text-blue-600">Yeni</span>
        </button>

        {/* 3. Güvenlik Testi */}
        <button
          type="button"
          onClick={handleSecurityClick}
          className="flex flex-col items-center justify-center py-1 px-2.5 rounded-xl text-slate-500 hover:text-blue-600 active:scale-95 transition-all cursor-pointer"
          title="Web Güvenlik Testi"
        >
          <Shield className="w-5 h-5 text-blue-600" />
          <span className="text-[10px] font-medium mt-0.5">Güvenlik</span>
        </button>

        {/* 4. Doküman Kütüphanesi */}
        <button
          type="button"
          onClick={onToggleLibrary}
          className="flex flex-col items-center justify-center py-1 px-2.5 rounded-xl text-slate-500 hover:text-slate-900 active:scale-95 transition-all cursor-pointer"
          title="Doküman Kütüphanesi"
        >
          <BookOpen className="w-5 h-5" />
          <span className="text-[10px] font-medium mt-0.5">Kütüphane</span>
        </button>

        {/* 5. Profil & API Anahtarları */}
        <button
          type="button"
          onClick={() => navigate('/app/profile')}
          className={`flex flex-col items-center justify-center py-1 px-2.5 rounded-xl active:scale-95 transition-all cursor-pointer ${
            isProfileActive ? 'text-blue-600 font-semibold' : 'text-slate-500 hover:text-slate-900'
          }`}
          title="Profil & Ayarlar"
        >
          <UserIcon className="w-5 h-5" />
          <span className="text-[10px] font-medium mt-0.5">Profil</span>
        </button>
      </div>
    </nav>
  );
};
