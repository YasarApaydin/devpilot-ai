import { useState, useEffect, useCallback } from 'react';
import { conversationApi } from '../api/conversationApi';
import type { Conversation } from '../types';

export function useConversations(activeConversationId?: string, enabled: boolean = true) {
  const [conversations, setConversations] = useState<Conversation[]>([]);
  const [isLoading, setIsLoading] = useState<boolean>(enabled);
  const [error, setError] = useState<string | null>(null);

  const [page, setPage] = useState<number>(1);
  const [hasMore, setHasMore] = useState<boolean>(true);

  const fetchConversations = useCallback(async (targetPage = 1, pageSize = 50, append = false) => {
    setIsLoading(true);
    setError(null);
    try {
      const res = await conversationApi.getConversations(targetPage, pageSize);
      if (res.data) {
        setConversations((prev) => append ? [...prev, ...res.data] : res.data);
        setHasMore(res.data.length >= pageSize);
        setPage(targetPage);
      }
    } catch (err: any) {
      setError(err?.message || 'Failed to load conversations.');
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    if (!enabled) {
      setIsLoading(false);
      return;
    }
    fetchConversations(1, 50);
  }, [enabled, fetchConversations]);

  const createConversation = useCallback(async (title?: string): Promise<Conversation> => {
    const res = await conversationApi.createConversation({ title });
    if (res.data) {
      setConversations((prev) => [res.data, ...prev]);
      return res.data;
    }
    throw new Error('Failed to create conversation');
  }, []);

  const renameConversation = useCallback(async (id: string, newTitle: string): Promise<Conversation> => {
    const res = await conversationApi.updateConversation(id, { title: newTitle });
    if (res.data) {
      setConversations((prev) =>
        prev.map((c) => (c.id === id ? { ...c, title: newTitle, updatedAt: res.data.updatedAt } : c))
      );
      return res.data;
    }
    throw new Error('Failed to rename conversation');
  }, []);

  const togglePin = useCallback(async (id: string): Promise<Conversation> => {
    const res = await conversationApi.togglePin(id);
    if (res.data) {
      await fetchConversations();
      return res.data;
    }
    throw new Error('Failed to toggle pin');
  }, [fetchConversations]);

  const deleteConversation = useCallback(async (id: string): Promise<void> => {
    await conversationApi.deleteConversation(id);
    setConversations((prev) => prev.filter((c) => c.id !== id));
  }, []);

  return {
    conversations,
    isLoading,
    error,
    refresh: fetchConversations,
    createConversation,
    renameConversation,
    togglePin,
    deleteConversation,
    activeConversation: conversations.find((c) => c.id === activeConversationId),
  };
}
