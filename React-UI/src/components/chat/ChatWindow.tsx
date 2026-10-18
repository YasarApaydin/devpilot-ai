import React from 'react';
import { MessageList } from './MessageList';
import { ChatComposer } from './ChatComposer';
import { PdfButton } from './PdfButton';
import { SecurityScanModal } from '../security/SecurityScanModal';
import { AlertCircle, MessageSquare, Shield } from 'lucide-react';
import type { ChatMessage, Conversation, SecurityScan } from '../../types';
import type { VoiceState } from '../../hooks/useChat';

export interface ChatWindowProps {
  conversation?: Conversation;
  messages: ChatMessage[];
  isLoadingMessages: boolean;
  loadError?: string | null;
  isSending: boolean;
  isStreaming: boolean;
  streamError: string | null;
  voiceState: VoiceState;
  voiceError?: string | null;
  initialDocument?: { id: string; fileName: string };
  onSendMessage: (text: string, documentId?: string, provider?: string, model?: string) => void;
  onSendVoice: (audioBlob: Blob, fileName?: string) => void;
  onScanStarted?: (scan: SecurityScan) => void;
  onScanUpdated?: (scan: SecurityScan) => void;
}

export const ChatWindow: React.FC<ChatWindowProps> = ({
  conversation,
  messages,
  isLoadingMessages,
  loadError,
  isSending,
  isStreaming,
  streamError,
  voiceState,
  voiceError,
  initialDocument,
  onSendMessage,
  onSendVoice,
  onScanStarted,
  onScanUpdated,
}) => {
  const [isSecurityModalOpen, setIsSecurityModalOpen] = React.useState(false);
  const [securityScanUrl, setSecurityScanUrl] = React.useState('');

  const handleOpenSecurityScan = (url?: string) => {
    setSecurityScanUrl(url || '');
    setIsSecurityModalOpen(true);
  };

  React.useEffect(() => {
    const handleGlobalScanTrigger = (e: any) => {
      handleOpenSecurityScan(e?.detail?.url);
    };
    window.addEventListener('devpilot:open-security-scan', handleGlobalScanTrigger);
    return () => {
      window.removeEventListener('devpilot:open-security-scan', handleGlobalScanTrigger);
    };
  }, []);

  const handleScanStartedInternal = (scan: SecurityScan) => {
    onScanStarted?.(scan);
  };

  return (
    <div className="flex-1 h-full flex flex-col bg-[#f8fafc] overflow-hidden relative">
      {/* Conversation Top Header */}
      <div className="h-14 px-6 border-b border-slate-200 flex items-center justify-between bg-white shrink-0">
        <div className="flex items-center gap-2.5 min-w-0">
          <div className="w-7 h-7 rounded-md bg-blue-50 text-blue-600 flex items-center justify-center shrink-0">
            <MessageSquare className="w-4 h-4" />
          </div>
          <h2 className="text-sm font-semibold text-slate-800 truncate">
            {conversation?.title || 'Active Conversation'}
          </h2>
          <span className="text-[11px] text-slate-500 font-mono hidden md:inline">
            ({messages.length} messages)
          </span>
        </div>

        <div className="flex items-center gap-2">
          {/* Security Scan Quick Button */}
          <button
            type="button"
            onClick={() => handleOpenSecurityScan()}
            className="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-xs font-semibold text-blue-700 bg-blue-50 hover:bg-blue-100 border border-blue-200 transition-colors shadow-xs"
            title="Hedef Web Uygulaması İçin Güvenlik Testi Başlat"
          >
            <Shield className="w-3.5 h-3.5 text-blue-600" />
            <span className="hidden sm:inline">Güvenlik Testi</span>
          </button>

          {conversation && (
            <PdfButton
              type="conversation"
              id={conversation.id}
              label="Export Conversation (PDF)"
              customTitle={conversation.title}
              messages={messages}
            />
          )}
        </div>
      </div>

      {/* Error Alert if occurred */}
      {(streamError || loadError) && (
        <div className="mx-6 mt-3 p-3 rounded-lg bg-rose-50 border border-rose-200 text-rose-700 text-xs flex items-center gap-2 shrink-0">
          <AlertCircle className="w-4 h-4 shrink-0 text-rose-500" />
          <span>{streamError || loadError}</span>
        </div>
      )}

      {/* Message List */}
      <MessageList
        messages={messages}
        isLoading={isLoadingMessages}
        isStreaming={isStreaming}
        loadError={loadError}
        onScanUpdated={onScanUpdated}
      />

      {/* Floating Bottom Chat Composer */}
      <div className="p-2.5 sm:p-6 pb-16 sm:pb-6 shrink-0 bg-gradient-to-t from-[#f8fafc] via-[#f8fafc]/95 to-transparent">
        <div className="max-w-4xl mx-auto w-full">
          <ChatComposer
            onSendMessage={onSendMessage}
            onSendVoice={onSendVoice}
            isSending={isSending}
            isStreaming={isStreaming}
            voiceState={voiceState}
            voiceError={voiceError}
            initialDocument={initialDocument}
            onOpenSecurityScan={handleOpenSecurityScan}
          />
        </div>
      </div>

      {/* Security Scan Modal Dialog */}
      <SecurityScanModal
        isOpen={isSecurityModalOpen}
        initialUrl={securityScanUrl}
        conversationId={conversation?.id}
        onClose={() => {
          setIsSecurityModalOpen(false);
          setSecurityScanUrl('');
        }}
        onScanStarted={handleScanStartedInternal}
      />
    </div>
  );
};
