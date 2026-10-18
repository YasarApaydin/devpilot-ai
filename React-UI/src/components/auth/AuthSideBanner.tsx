import React from 'react';
import { Bot, Sparkles, Shield, Cpu } from 'lucide-react';

export const AuthSideBanner: React.FC = () => {
  return (
    <div className="hidden lg:flex flex-col justify-between p-12 bg-slate-100 text-slate-800 relative overflow-hidden h-full select-none">
      {/* Brand logo - Fixed at top */}
      <div className="relative z-10 flex items-center gap-3 shrink-0">
        <img
          src="/app-logo.png"
          alt="DevPilot AI"
          className="w-11 h-11 rounded-full object-cover shadow-sm border border-slate-200"
        />
        <div>
          <span className="text-xl font-bold tracking-tight text-slate-900">DevPilot</span>
          <span className="text-xl font-light text-blue-600 ml-1">AI</span>
        </div>
      </div>

      {/* Feature summary - Fixed in position */}
      <div className="relative z-10 space-y-6 max-w-md my-auto">
        <div>
          <span className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-semibold bg-blue-50 text-blue-700 border border-blue-200 mb-4 shadow-2xs">
            <Sparkles className="w-3.5 h-3.5" /> Next-Gen AI Workspace
          </span>
          <h2 className="text-3xl font-extrabold text-slate-900 tracking-tight leading-tight">
            Intelligent Agent with Memory, Voice &amp; Documents.
          </h2>
          <p className="text-slate-600 text-sm mt-3 leading-relaxed">
            Harness the power of LangGraph conversational memory, Groq-accelerated inference, and automated knowledge indexing for engineering teams.
          </p>
        </div>

        <div className="space-y-3 pt-2">
          <div className="flex items-start gap-3 p-3 rounded-xl bg-white border border-slate-200/90 shadow-2xs">
            <div className="p-2 rounded-lg bg-blue-50 text-blue-600 shrink-0">
              <Cpu className="w-4 h-4" />
            </div>
            <div>
              <h4 className="text-xs font-semibold text-slate-900">Stateful LangGraph Memory</h4>
              <p className="text-xs text-slate-500 mt-0.5">Seamless session persistence across conversations and tools.</p>
            </div>
          </div>

          <div className="flex items-start gap-3 p-3 rounded-xl bg-white border border-slate-200/90 shadow-2xs">
            <div className="p-2 rounded-lg bg-emerald-50 text-emerald-600 shrink-0">
              <Shield className="w-4 h-4" />
            </div>
            <div>
              <h4 className="text-xs font-semibold text-slate-900">Enterprise Ready .NET Contract</h4>
              <p className="text-xs text-slate-500 mt-0.5">Clean architecture backend layer with zero exposed secrets.</p>
            </div>
          </div>
        </div>
      </div>

      {/* Footer note - Fixed at bottom */}
      <div className="relative z-10 text-xs text-slate-500 flex items-center justify-between shrink-0 pt-4">
        <span>© {new Date().getFullYear()} DevPilot AI Workspace</span>
        <span>Secure &amp; Scalable</span>
      </div>
    </div>
  );
};
