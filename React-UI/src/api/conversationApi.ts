import { apiClient } from './apiClient';
import { ENDPOINTS } from '../config/endpoints';
import { ENV } from '../config/env';
import { MOCK_CONVERSATIONS, MOCK_MESSAGES } from './mockData';
import type {
  Conversation,
  CreateConversationRequest,
  UpdateConversationRequest,
  ChatMessage,
  ApiResponse,
} from '../types';

let mockConversations = [...MOCK_CONVERSATIONS];
let mockMessages = { ...MOCK_MESSAGES };

export const conversationApi = {
  async getConversations(page = 1, pageSize = 50): Promise<ApiResponse<Conversation[]>> {
    try {
      return await apiClient.get<ApiResponse<Conversation[]>>(
        `${ENDPOINTS.CONVERSATIONS.LIST}?page=${page}&pageSize=${pageSize}`
      );
    } catch (error) {
      if (ENV.enableMockFallback) {
        const start = (page - 1) * pageSize;
        return {
          success: true,
          data: mockConversations.slice(start, start + pageSize),
        };
      }
      throw error;
    }
  },

  async getConversation(id: string): Promise<ApiResponse<Conversation>> {
    try {
      return await apiClient.get<ApiResponse<Conversation>>(ENDPOINTS.CONVERSATIONS.DETAIL(id));
    } catch (error) {
      if (ENV.enableMockFallback) {
        const found = mockConversations.find((c) => c.id === id);
        if (!found) {
          throw { statusCode: 404, message: 'Conversation not found.' };
        }
        return {
          success: true,
          data: found,
        };
      }
      throw error;
    }
  },

  async createConversation(data?: CreateConversationRequest): Promise<ApiResponse<Conversation>> {
    try {
      return await apiClient.post<ApiResponse<Conversation>>(ENDPOINTS.CONVERSATIONS.CREATE, data);
    } catch (error) {
      if (ENV.enableMockFallback) {
        const newConv: Conversation = {
          id: `conv_${Date.now()}`,
          title: data?.title || 'New Conversation',
          createdAt: new Date().toISOString(),
          updatedAt: new Date().toISOString(),
          messageCount: 0,
        };
        mockConversations = [newConv, ...mockConversations];
        mockMessages[newConv.id] = [];
        return {
          success: true,
          data: newConv,
        };
      }
      throw error;
    }
  },

  async updateConversation(
    id: string,
    data: UpdateConversationRequest
  ): Promise<ApiResponse<Conversation>> {
    try {
      return await apiClient.put<ApiResponse<Conversation>>(ENDPOINTS.CONVERSATIONS.UPDATE(id), data);
    } catch (error) {
      if (ENV.enableMockFallback) {
        mockConversations = mockConversations.map((c) =>
          c.id === id ? { ...c, title: data.title ?? c.title, isPinned: data.isPinned ?? c.isPinned, updatedAt: new Date().toISOString() } : c
        );
        const updated = mockConversations.find((c) => c.id === id)!;
        return {
          success: true,
          data: updated,
        };
      }
      throw error;
    }
  },

  async togglePin(id: string): Promise<ApiResponse<Conversation>> {
    try {
      return await apiClient.patch<ApiResponse<Conversation>>(ENDPOINTS.CONVERSATIONS.PIN(id));
    } catch (error) {
      if (ENV.enableMockFallback) {
        mockConversations = mockConversations.map((c) =>
          c.id === id ? { ...c, isPinned: !c.isPinned, pinnedAt: !c.isPinned ? new Date().toISOString() : undefined } : c
        );
        const updated = mockConversations.find((c) => c.id === id)!;
        return {
          success: true,
          data: updated,
        };
      }
      throw error;
    }
  },

  async deleteConversation(id: string): Promise<ApiResponse<void>> {
    try {
      return await apiClient.delete<ApiResponse<void>>(ENDPOINTS.CONVERSATIONS.DELETE(id));
    } catch (error) {
      if (ENV.enableMockFallback) {
        mockConversations = mockConversations.filter((c) => c.id !== id);
        delete mockMessages[id];
        return {
          success: true,
          data: undefined,
        };
      }
      throw error;
    }
  },

  async getMessages(conversationId: string, page = 1, pageSize = 500): Promise<ApiResponse<ChatMessage[]>> {
    try {
      return await apiClient.get<ApiResponse<ChatMessage[]>>(
        `${ENDPOINTS.CONVERSATIONS.MESSAGES(conversationId)}?page=${page}&pageSize=${pageSize}`
      );
    } catch (error) {
      if (ENV.enableMockFallback) {
        const list = mockMessages[conversationId] || [];
        const start = (page - 1) * pageSize;
        return {
          success: true,
          data: list.slice(start, start + pageSize),
        };
      }
      throw error;
    }
  },
};
