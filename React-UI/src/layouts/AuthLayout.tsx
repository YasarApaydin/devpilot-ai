import React from 'react';
import { Link, Navigate, Outlet } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { AuthSideBanner } from '../components/auth/AuthSideBanner';
import { Spinner } from '../components/ui/Spinner';

export const AuthLayout: React.FC = () => {
  const { isAuthenticated, isLoading } = useAuth();

  if (isLoading) {
    return (
      <div className="min-h-screen bg-[#f8fafc] flex items-center justify-center">
        <Spinner size="lg" className="text-blue-600" />
      </div>
    );
  }

  // Redirect authenticated user to app
  if (isAuthenticated) {
    return <Navigate to="/app" replace />;
  }

  return (
    <div className="min-h-screen w-full bg-[#f8fafc] flex relative">
      {/* Left Column: Visual branding banner on desktop (Fixed & Static, never shifts) */}
      <div className="w-1/2 hidden lg:block sticky top-0 h-screen shrink-0 overflow-hidden border-r border-slate-200">
        <AuthSideBanner />
      </div>

      {/* Right Column: Form card container */}
      <div className="w-full lg:w-1/2 min-h-screen flex items-center justify-center p-4 sm:p-8 md:p-12 overflow-y-auto">
        <div className="w-full max-w-md animate-slide-up">
          {/* Mobile Header Logo */}
          <div className="lg:hidden flex items-center justify-center gap-2.5 mb-6">
            <img
              src="/app-logo.png"
              alt="DevPilot AI"
              className="w-10 h-10 rounded-full object-cover shadow-xs border border-slate-200"
            />
            <div className="flex items-center font-bold text-slate-900 text-lg">
              <span>DevPilot</span>
              <span className="text-blue-600 font-light ml-0.5">AI</span>
            </div>
          </div>
          <Outlet />

          {/* Auth Footer Links */}
          <div className="mt-8 pt-4 border-t border-slate-200/80 text-center text-xs text-slate-500 flex items-center justify-center gap-3">
            <Link to="/privacy" className="hover:text-blue-600 transition-colors">
              Gizlilik Politikası
            </Link>
            <span className="text-slate-300">•</span>
            <Link to="/cookies" className="hover:text-blue-600 transition-colors">
              Çerez Politikası
            </Link>
          </div>
        </div>
      </div>
    </div>
  );
};
