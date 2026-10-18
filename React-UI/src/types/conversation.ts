/**
 * Conversation Types
 */

export interface Conversation {
  id: string;
  title: string;
  createdAt: string;
  updatedAt?: string;
  messageCount?: number;
  isPinned?: boolean;
  pinnedAt?: string;
}

export interface CreateConversationRequest {
  title?: string;
}

export interface UpdateConversationRequest {
  title?: string;
  isPinned?: boolean;
}
