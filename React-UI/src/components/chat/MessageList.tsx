import React, { useEffect, useRef } from 'react';
import { MessageBubble } from './MessageBubble';
import { EmptyState } from '../ui/EmptyState';
import { TypingIndicator } from './TypingIndicator';
import { Bot } from 'lucide-react';
import type { ChatMessage } from '../../types';

export interface MessageListProps {
  messages: ChatMessage[];
  isLoading: boolean;
  isStreaming: boolean;
  loadError?: string | null;
  onScanUpdated?: (scan: import('../../types').SecurityScan) => void;
}

export const MessageList: React.FC<MessageListProps> = ({
  messages,
  isLoading,
  isStreaming,
  loadError,
  onScanUpdated,
}) => {
  const bottomRef = useRef<HTMLDivElement>(null);

  // Auto-scroll on new message or during stream
  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages, isStreaming]);

  if (isLoading) {
    return (
      <div className="flex-1 flex items-center justify-center text-slate-500 text-xs">
        Konuşma geçmişi yükleniyor...
      </div>
    );
  }

  if (loadError && messages.length === 0) {
    return (
      <div className="flex-1 flex flex-col items-center justify-center p-6 text-center">
        <div className="max-w-md w-full p-6 rounded-2xl bg-white border border-rose-200 shadow-2xs space-y-2">
          <p className="text-sm font-semibold text-rose-700">Konuşma geçmişi yüklenemedi</p>
          <p className="text-xs text-slate-500 leading-relaxed">{loadError}</p>
        </div>
      </div>
    );
  }

  if (messages.length === 0) {
    return (
      <div className="flex-1 flex flex-col items-center justify-center p-6">
        <EmptyState
          icon={<img src="/app-logo.png" alt="DevPilot" className="w-8 h-8 rounded-full object-cover" />}
          title="Start a conversation with your AI assistant"
          description="Ask technical questions, generate code, analyze topics using internet search, or record voice messages."
          className="max-w-md w-full py-10"
        />
      </div>
    );
  }

  return (
    <div className="flex-1 overflow-y-auto overflow-x-hidden px-4 py-4 sm:px-6">
      <div className="max-w-4xl mx-auto w-full space-y-2">
        {messages.map((message, index) => {
          const previousUserMsg =
            index > 0 && messages[index - 1].role === 'user'
              ? messages[index - 1].content
              : undefined;

          return (
            <MessageBubble
              key={message.id}
              id={message.id}
              role={message.role}
              content={message.content}
              createdAt={message.createdAt}
              inputType={message.inputType}
              isStreaming={message.isStreaming}
              audioUrl={message.audioUrl}
              userPrompt={previousUserMsg}
              securityScan={message.securityScan}
              modelUsed={message.modelUsed}
              fallbackReason={message.fallbackReason}
              originalModel={message.originalModel}
              onScanUpdated={onScanUpdated}
            />
          );
        })}

        {isStreaming && messages[messages.length - 1]?.role !== 'assistant' && (
          <div className="my-2">
            <TypingIndicator />
          </div>
        )}

        <div ref={bottomRef} />
      </div>
    </div>
  );
};
