import React from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { AuthProvider, useAuth } from './context/AuthContext';
import { CreditProvider } from './context/CreditContext';
import { ToastProvider } from './context/ToastContext';
import { CookieConsentProvider } from './context/CookieConsentContext';
import { ToastContainer } from './components/ui/ToastContainer';
import { CookieConsentBanner } from './components/cookie/CookieConsentBanner';
import { CookiePreferencesModal } from './components/cookie/CookiePreferencesModal';
import { ErrorBoundary } from './components/ui/ErrorBoundary';

// Layouts
import { AuthLayout } from './layouts/AuthLayout';
import { AppLayout } from './layouts/AppLayout';

// Pages
import { LandingPage } from './pages/LandingPage';
import { LoginPage } from './pages/LoginPage';
import { RegisterPage } from './pages/RegisterPage';
import { VerifyEmailPage } from './pages/VerifyEmailPage';
import { ForgotPasswordPage } from './pages/ForgotPasswordPage';
import { ResetPasswordPage } from './pages/ResetPasswordPage';
import { AppPage } from './pages/AppPage';
import { ChatPage } from './pages/ChatPage';
import { LibraryPage } from './pages/LibraryPage';
import { ProfilePage } from './pages/ProfilePage';
import { PrivacyPolicyPage } from './pages/PrivacyPolicyPage';
import { CookiePolicyPage } from './pages/CookiePolicyPage';
import { NotFoundPage } from './pages/NotFoundPage';

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      refetchOnWindowFocus: false,
      retry: 1,
    },
  },
});

/**
 * RootRoute renders LandingPage on root "/" URL.
 */
const RootRoute: React.FC = () => {
  const { isLoading } = useAuth();

  if (isLoading) {
    return (
      <div className="min-h-screen bg-[#f8fafc] flex items-center justify-center">
        <div className="w-8 h-8 border-3 border-blue-600 border-t-transparent rounded-full animate-spin" />
      </div>
    );
  }

  return <LandingPage />;
};

export function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <ToastProvider>
        <AuthProvider>
          <CreditProvider>
            <CookieConsentProvider>
              <BrowserRouter>
                <ErrorBoundary>
                  <Routes>
                    {/* Root Route */}
                    <Route path="/" element={<RootRoute />} />

                    {/* Public Legal / Policy Routes */}
                    <Route path="/privacy" element={<PrivacyPolicyPage />} />
                    <Route path="/cookies" element={<CookiePolicyPage />} />

                    {/* Public Auth Routes */}
                    <Route element={<AuthLayout />}>
                      <Route path="/login" element={<LoginPage />} />
                      <Route path="/register" element={<RegisterPage />} />
                      <Route path="/verify-email" element={<VerifyEmailPage />} />
                      <Route path="/forgot-password" element={<ForgotPasswordPage />} />
                      <Route path="/reset-password" element={<ResetPasswordPage />} />
                    </Route>

                    {/* Authenticated Application Routes */}
                    <Route path="/app" element={<AppLayout />}>
                      <Route index element={<AppPage />} />
                      <Route path="chat/:conversationId" element={<ChatPage />} />
                      <Route path="library" element={<LibraryPage />} />
                      <Route path="profile" element={<ProfilePage />} />
                    </Route>

                    {/* 404 Route */}
                    <Route path="*" element={<NotFoundPage />} />
                  </Routes>

                  {/* Global Toasts */}
                  <ToastContainer />

                  {/* Cookie Consent System */}
                  <CookieConsentBanner />
                  <CookiePreferencesModal />
                </ErrorBoundary>
              </BrowserRouter>
            </CookieConsentProvider>
          </CreditProvider>
        </AuthProvider>
      </ToastProvider>
    </QueryClientProvider>
  );
}

export default App;
