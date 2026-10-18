import React, { useEffect } from 'react';
import { useNavigate, useOutletContext, useLocation } from 'react-router-dom';
import { Button } from '../components/ui/Button';
import { Bot, Plus, BookOpen } from 'lucide-react';
import type { Conversation } from '../types';

interface AppContext {
  conversations: Conversation[];
  handleNewConversation: (state?: any) => Promise<any>;
}

export const AppPage: React.FC = () => {
  const context = useOutletContext<AppContext | undefined>();
  const conversations = context?.conversations ?? [];
  const handleNewConversation = context?.handleNewConversation;
  const navigate = useNavigate();
  const location = useLocation();
  const hasTriggeredRef = React.useRef(false);

  // If conversations already exist, automatically route to the most recent one
  useEffect(() => {
    if (conversations && conversations.length > 0) {
      navigate(`/app/chat/${conversations[0].id}`, { state: location.state, replace: true });
    } else if (location.state && (location.state as any).analyzeDoc) {
      if (hasTriggeredRef.current) return;
      hasTriggeredRef.current = true;
      handleNewConversation?.(location.state);
    }
  }, [conversations, navigate, location.state, handleNewConversation]);

  return (
    <div className="flex-1 h-full flex flex-col items-center justify-center p-6 bg-[#f8fafc]">
      <div className="max-w-md w-full p-8 rounded-2xl bg-white border border-slate-200 shadow-sm text-center space-y-4">
        <img
          src="/app-logo.png"
          alt="DevPilot AI"
          className="w-16 h-16 rounded-full object-cover mx-auto shadow-sm border border-slate-200"
        />

        <div>
          <h2 className="text-xl font-bold text-slate-900">Welcome to DevPilot AI Workspace</h2>
          <p className="text-xs text-slate-500 mt-1 leading-relaxed">
            Your conversational agent with LangGraph memory, Groq fast inference, voice processing, and PDF document search.
          </p>
        </div>

        <div className="pt-2 flex flex-col sm:flex-row items-center justify-center gap-3">
          <Button
            variant="primary"
            size="md"
            leftIcon={<Plus className="w-4 h-4" />}
            onClick={() => handleNewConversation?.()}
          >
            Start New Conversation
          </Button>

          <Button
            variant="outline"
            size="md"
            leftIcon={<BookOpen className="w-4 h-4" />}
            onClick={() => navigate('/app/library')}
          >
            Explore Library
          </Button>
        </div>
      </div>
    </div>
  );
};
