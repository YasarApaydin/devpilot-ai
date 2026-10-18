import React from 'react';
import { Link } from 'react-router-dom';
import {
  Bot,
  MessageSquare,
  Mic,
  FileText,
  Database,
  ArrowRight,
  Sparkles,
  Zap,
  ShieldCheck,
  LogOut,
} from 'lucide-react';
import { Button } from '../components/ui/Button';
import { useAuth } from '../context/AuthContext';

import { useCookieConsent } from '../context/CookieConsentContext';

export const LandingPage: React.FC = () => {
  const { isAuthenticated, user, logout } = useAuth();
  const { openPreferences } = useCookieConsent();

  return (
    <div className="min-h-screen bg-[#f8fafc] text-slate-800 flex flex-col selection:bg-blue-600 selection:text-white">
      {/* Navigation Bar */}
      <header className="border-b border-slate-200 bg-white/90 backdrop-blur-md sticky top-0 z-40 shadow-2xs">
        <div className="max-w-7xl mx-auto px-6 h-16 flex items-center justify-between">
          <div className="flex items-center gap-3">
            <img
              src="/app-logo.png"
              alt="DevPilot AI"
              className="w-9 h-9 rounded-full object-cover shadow-xs border border-slate-200"
            />
            <div className="flex items-center">
              <span className="text-lg font-bold tracking-tight text-slate-900">DevPilot</span>
              <span className="text-lg font-light text-blue-600 ml-1">AI</span>
            </div>
          </div>

          <div className="flex items-center gap-3">
            {isAuthenticated ? (
              <>
                <span className="text-xs text-slate-500 hidden sm:inline-block">
                  {user?.email}
                </span>
                <Link to="/app">
                  <Button variant="primary" size="sm" rightIcon={<ArrowRight className="w-3.5 h-3.5" />}>
                    Uygulamaya Git
                  </Button>
                </Link>
                <Button variant="ghost" size="sm" onClick={() => logout()} title="Çıkış Yap">
                  <LogOut className="w-4 h-4 text-slate-500" />
                </Button>
              </>
            ) : (
              <>
                <Link to="/login">
                  <Button variant="ghost" size="sm">
                    Giriş Yap
                  </Button>
                </Link>
                <Link to="/register">
                  <Button variant="primary" size="sm" rightIcon={<ArrowRight className="w-3.5 h-3.5" />}>
                    Kayıt Ol
                  </Button>
                </Link>
              </>
            )}
          </div>
        </div>
      </header>

      {/* Hero Section */}
      <main className="flex-1">
        <section className="relative pt-16 pb-20 md:pt-24 md:pb-28 px-6">
          <div className="max-w-4xl mx-auto text-center space-y-5">
            <div className="inline-flex items-center gap-2 px-3.5 py-1.5 rounded-full text-xs font-semibold bg-blue-50 text-blue-700 border border-blue-200 shadow-2xs">
              <Sparkles className="w-3.5 h-3.5" /> Enterprise AI Agent Workspace
            </div>

            <h1 className="text-4xl sm:text-5xl md:text-6xl font-extrabold text-slate-900 tracking-tight leading-[1.15]">
              Your Personal AI Workspace
            </h1>

            <p className="text-base sm:text-lg text-slate-600 max-w-2xl mx-auto leading-relaxed">
              Talk to your AI assistant, upload your documents and use your personal knowledge library.
            </p>

            <div className="flex flex-col sm:flex-row items-center justify-center gap-3 pt-3">
              {isAuthenticated ? (
                <Link to="/app" className="w-full sm:w-auto">
                  <Button
                    variant="primary"
                    size="lg"
                    className="w-full sm:w-auto px-8 font-semibold shadow-xs"
                    rightIcon={<ArrowRight className="w-4 h-4" />}
                  >
                    Uygulamaya Git
                  </Button>
                </Link>
              ) : (
                <>
                  <Link to="/register" className="w-full sm:w-auto">
                    <Button
                      variant="primary"
                      size="lg"
                      className="w-full sm:w-auto px-8 font-semibold shadow-xs"
                      rightIcon={<ArrowRight className="w-4 h-4" />}
                    >
                      Hemen Başla
                    </Button>
                  </Link>
                  <Link to="/login" className="w-full sm:w-auto">
                    <Button variant="outline" size="lg" className="w-full sm:w-auto px-8">
                      Giriş Yap
                    </Button>
                  </Link>
                </>
              )}
            </div>

            {/* Feature Highlights Grid */}
            <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-4 gap-4 pt-14 text-left">
              <div className="p-5 rounded-2xl bg-white border border-slate-200/90 shadow-2xs hover:border-slate-300 hover:shadow-xs transition-all">
                <div className="w-10 h-10 rounded-xl bg-blue-50 text-blue-600 flex items-center justify-center mb-3">
                  <MessageSquare className="w-5 h-5" />
                </div>
                <h3 className="text-sm font-semibold text-slate-900">AI Chat</h3>
                <p className="text-xs text-slate-500 mt-1 leading-relaxed">
                  LangGraph conversational state with Tavily web search tool routing.
                </p>
              </div>

              <div className="p-5 rounded-2xl bg-white border border-slate-200/90 shadow-2xs hover:border-slate-300 hover:shadow-xs transition-all">
                <div className="w-10 h-10 rounded-xl bg-purple-50 text-purple-600 flex items-center justify-center mb-3">
                  <Mic className="w-5 h-5" />
                </div>
                <h3 className="text-sm font-semibold text-slate-900">Voice Interaction</h3>
                <p className="text-xs text-slate-500 mt-1 leading-relaxed">
                  Whisper Large V3 Turbo STT with local noise filtering & gTTS audio playback.
                </p>
              </div>

              <div className="p-5 rounded-2xl bg-white border border-slate-200/90 shadow-2xs hover:border-slate-300 hover:shadow-xs transition-all">
                <div className="w-10 h-10 rounded-xl bg-amber-50 text-amber-600 flex items-center justify-center mb-3">
                  <FileText className="w-5 h-5" />
                </div>
                <h3 className="text-sm font-semibold text-slate-900">PDF Export</h3>
                <p className="text-xs text-slate-500 mt-1 leading-relaxed">
                  Export formatted AI reports and entire conversation threads to PDF.
                </p>
              </div>

              <div className="p-5 rounded-2xl bg-white border border-slate-200/90 shadow-2xs hover:border-slate-300 hover:shadow-xs transition-all">
                <div className="w-10 h-10 rounded-xl bg-emerald-50 text-emerald-600 flex items-center justify-center mb-3">
                  <Database className="w-5 h-5" />
                </div>
                <h3 className="text-sm font-semibold text-slate-900">Personal Library</h3>
                <p className="text-xs text-slate-500 mt-1 leading-relaxed">
                  Upload PDF documents for private search and augmented AI context.
                </p>
              </div>
            </div>
          </div>
        </section>

        {/* Technical Architecture Section */}
        <section className="py-14 border-t border-slate-200 bg-white px-6">
          <div className="max-w-5xl mx-auto">
            <div className="text-center mb-8">
              <h2 className="text-2xl font-bold text-slate-900 tracking-tight">Enterprise Architecture</h2>
              <p className="text-xs text-slate-500 mt-1">
                Zero secret exposure. React UI communicates strictly with the .NET API.
              </p>
            </div>

            <div className="flex flex-col md:flex-row items-center justify-center gap-4 text-center">
              <div className="p-4 rounded-xl bg-slate-50 border border-slate-200 w-full md:w-56 shadow-2xs">
                <Zap className="w-5 h-5 text-blue-600 mx-auto mb-1.5" />
                <span className="text-xs font-semibold text-slate-900">React UI</span>
                <p className="text-[11px] text-slate-500 mt-0.5">TypeScript & Clean Components</p>
              </div>

              <div className="text-slate-400 rotate-90 md:rotate-0 font-mono text-sm">➔</div>

              <div className="p-4 rounded-xl bg-slate-50 border border-slate-200 w-full md:w-56 shadow-2xs">
                <ShieldCheck className="w-5 h-5 text-emerald-600 mx-auto mb-1.5" />
                <span className="text-xs font-semibold text-slate-900">.NET API Layer</span>
                <p className="text-[11px] text-slate-500 mt-0.5">Clean Architecture & Auth</p>
              </div>

              <div className="text-slate-400 rotate-90 md:rotate-0 font-mono text-sm">➔</div>

              <div className="p-4 rounded-xl bg-slate-50 border border-slate-200 w-full md:w-56 shadow-2xs">
                <Bot className="w-5 h-5 text-purple-600 mx-auto mb-1.5" />
                <span className="text-xs font-semibold text-slate-900">AI Agent Service</span>
                <p className="text-[11px] text-slate-500 mt-0.5">LangChain / LangGraph / Groq</p>
              </div>
            </div>
          </div>
        </section>
      </main>

      {/* Footer */}
      <footer className="border-t border-slate-200 py-6 px-6 text-center text-xs text-slate-500 bg-slate-50">
        <div className="max-w-7xl mx-auto flex flex-col sm:flex-row items-center justify-between gap-4">
          <div className="flex items-center gap-2">
            <span className="font-semibold text-slate-700">DevPilot AI</span>
            <span>— Your Personal AI Workspace</span>
          </div>

          <div className="flex flex-wrap items-center justify-center gap-4 text-xs">
            <button
              type="button"
              onClick={openPreferences}
              className="text-slate-600 hover:text-blue-600 transition-colors cursor-pointer font-medium"
            >
              Çerez Tercihleri
            </button>
            <span className="text-slate-300">•</span>
            <Link to="/cookies" className="text-slate-600 hover:text-blue-600 transition-colors font-medium">
              Çerez Politikası
            </Link>
            <span className="text-slate-300">•</span>
            <Link to="/privacy" className="text-slate-600 hover:text-blue-600 transition-colors font-medium">
              Gizlilik Politikası
            </Link>
          </div>

          <div>© {new Date().getFullYear()} DevPilot AI. Tüm hakları saklıdır.</div>
        </div>
      </footer>
    </div>
  );
};
