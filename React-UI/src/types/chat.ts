/**
 * Chat and Message Types
 */

export type MessageRole = 'user' | 'assistant' | 'system' | 'tool';
export type InputType = 'text' | 'voice';

export interface ChatMessage {
  id: string;
  conversationId: string;
  role: MessageRole;
  content: string;
  createdAt: string;
  inputType?: InputType;
  audioUrl?: string;
  hasPdf?: boolean;
  isStreaming?: boolean;
  securityScan?: import('./securityScan').SecurityScan;
  modelUsed?: string;
  fallbackReason?: string;
  originalModel?: string;
}

export interface StreamChatRequest {
  conversationId: string;
  message: string;
  documentId?: string;
  provider?: string;
  model?: string;
}

export interface VoiceChatRequest {
  conversationId: string;
  audioBlob: Blob;
  fileName?: string;
}

export interface VoiceChatResponse {
  messageId: string;
  transcription: string;
  replyContent: string;
  audioUrl?: string;
}
