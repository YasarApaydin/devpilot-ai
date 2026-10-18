import React, { useEffect } from 'react';
import { useParams, useOutletContext, useLocation, useNavigate } from 'react-router-dom';
import { ChatWindow } from '../components/chat/ChatWindow';
import { useChat } from '../hooks/useChat';
import type { Conversation, SecurityScan } from '../types';

interface AppContext {
  conversations: Conversation[];
}

export const ChatPage: React.FC = () => {
  const { conversationId } = useParams<{ conversationId: string }>();
  const navigate = useNavigate();
  const context = useOutletContext<AppContext | undefined>();
  const conversations = context?.conversations ?? [];
  const location = useLocation();
  const initialDocument = (location.state as any)?.analyzeDoc;

  const currentConversation = conversations.find((c) => c.id === conversationId);

  useEffect(() => {
    if (currentConversation?.title) {
      document.title = `${currentConversation.title} | DevPilot AI`;
    } else {
      document.title = 'Sohbet & Siber Güvenlik | DevPilot AI';
    }
  }, [currentConversation?.title]);

  const {
    messages,
    isLoadingMessages,
    loadError,
    isSending,
    isStreaming,
    streamError,
    voiceState,
    voiceError,
    sendMessage,
    sendVoice,
    addSecurityScanMessage,
    handleScanUpdated,
  } = useChat(conversationId);

  const handleScanStarted = (scan: SecurityScan) => {
    addSecurityScanMessage(scan);
    if (!conversationId && scan.conversationId) {
      navigate(`/app/${scan.conversationId}`, { replace: true });
    }
  };

  return (
    <ChatWindow
      conversation={currentConversation}
      messages={messages}
      isLoadingMessages={isLoadingMessages}
      loadError={loadError}
      isSending={isSending}
      isStreaming={isStreaming}
      streamError={streamError}
      voiceState={voiceState}
      voiceError={voiceError}
      initialDocument={initialDocument}
      onSendMessage={sendMessage}
      onSendVoice={sendVoice}
      onScanStarted={handleScanStarted}
      onScanUpdated={handleScanUpdated}
    />
  );
};
