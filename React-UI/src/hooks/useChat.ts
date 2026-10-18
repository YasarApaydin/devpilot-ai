import { useState, useEffect, useCallback } from 'react';
import { conversationApi } from '../api/conversationApi';
import { chatApi } from '../api/chatApi';
import { useCredit } from '../context/CreditContext';
import type { ChatMessage } from '../types';

export type VoiceState =
  | 'idle'
  | 'recording'
  | 'uploading'
  | 'transcribing'
  | 'processing'
  | 'completed'
  | 'error';

export function useChat(conversationId?: string) {
  const { refreshCredits, consumeCredit, updateRemainingCredits } = useCredit();
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [isLoadingMessages, setIsLoadingMessages] = useState<boolean>(false);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [isSending, setIsSending] = useState<boolean>(false);
  const [isStreaming, setIsStreaming] = useState<boolean>(false);
  const [streamError, setStreamError] = useState<string | null>(null);
  const [voiceState, setVoiceState] = useState<VoiceState>('idle');
  const [voiceError, setVoiceError] = useState<string | null>(null);

  // Load message history when conversationId changes
  useEffect(() => {
    // Immediately clear previous conversation's messages and reset error states
    setMessages([]);
    setLoadError(null);
    setStreamError(null);

    if (!conversationId) {
      return;
    }

    let isMounted = true;
    const fetchHistory = async () => {
      setIsLoadingMessages(true);
      try {
        const res = await conversationApi.getMessages(conversationId, 1, 500);
        if (isMounted) {
          if (res.data) {
            setMessages(res.data);
          } else {
            setMessages([]);
          }
        }
      } catch (err: any) {
        if (isMounted) {
          setMessages([]);
          setLoadError(err?.message || 'Konuşma geçmişi yüklenemedi.');
        }
      } finally {
        if (isMounted) setIsLoadingMessages(false);
      }
    };

    fetchHistory();
    return () => {
      isMounted = false;
    };
  }, [conversationId]);

  const sendMessage = useCallback(
    async (content: string, documentId?: string, provider?: string, model?: string) => {
      if (!conversationId || !content.trim() || isSending || isStreaming) return;

      setIsSending(true);
      setStreamError(null);

      const userMessageId = `msg_user_${Date.now()}`;
      const userMessage: ChatMessage = {
        id: userMessageId,
        conversationId,
        role: 'user',
        content: content.trim(),
        createdAt: new Date().toISOString(),
        inputType: 'text',
      };

      const assistantMessageId = `msg_asst_${Date.now() + 1}`;
      const assistantPlaceholder: ChatMessage = {
        id: assistantMessageId,
        conversationId,
        role: 'assistant',
        content: '',
        createdAt: new Date().toISOString(),
        isStreaming: true,
        hasPdf: true,
      };

      // 1. Optimistic append of user message & empty assistant placeholder
      setMessages((prev) => [...prev, userMessage, assistantPlaceholder]);
      setIsStreaming(true);

      // Instantly decrement credit in UI if using default system model (real-time feedback)
      if (!provider || provider === 'system') {
        consumeCredit();
      }

      let accumulated = '';
      let isErrorHandled = false;
      let isCompleted = false;

      const handleStreamError = (err: any) => {
        if (isErrorHandled || isCompleted) return;
        isErrorHandled = true;

        setIsStreaming(false);
        setIsSending(false);
        const errMsg = err?.message || 'İstek işlenirken bir hata oluştu.';
        setStreamError(errMsg);

        setMessages((prev) =>
          prev.map((msg) => {
            if (msg.id !== assistantMessageId) return msg;

            const existingContent = accumulated.trim();
            const errorNotice = `⚠️ Hata oluştu: ${errMsg}`;
            const finalContent = existingContent
              ? `${existingContent}\n\n${errorNotice}`
              : errorNotice;

            return {
              ...msg,
              content: finalContent,
              isStreaming: false,
            };
          })
        );
        refreshCredits();
      };

      try {
        await chatApi.streamMessage(
          {
            conversationId,
            message: content.trim(),
            documentId,
            provider,
            model,
          },
          (chunk) => {
            accumulated += chunk;
            setMessages((prev) =>
              prev.map((msg) =>
                msg.id === assistantMessageId
                  ? { ...msg, content: accumulated, isStreaming: true }
                  : msg
              )
            );
          },
          (finalText, meta) => {
            isCompleted = true;
            setIsStreaming(false);
            setIsSending(false);
            setMessages((prev) =>
              prev.map((msg) =>
                msg.id === assistantMessageId
                  ? {
                      ...msg,
                      content: finalText,
                      isStreaming: false,
                      modelUsed: meta?.modelUsed,
                      fallbackReason: meta?.fallbackReason,
                      originalModel: meta?.originalModel,
                    }
                  : msg
              )
            );
            if (typeof meta?.remainingCredits === 'number') {
              updateRemainingCredits(meta.remainingCredits);
            }
            refreshCredits();
          },
          (err) => {
            handleStreamError(err);
          },
          (meta) => {
            if (typeof meta?.remainingCredits === 'number') {
              updateRemainingCredits(meta.remainingCredits);
            }
          }
        );
      } catch (err: any) {
        handleStreamError(err);
      }
    },
    [conversationId, isSending, isStreaming, consumeCredit, updateRemainingCredits, refreshCredits]
  );

  const sendVoice = useCallback(
    async (audioBlob: Blob, fileName?: string) => {
      if (!conversationId || isSending || isStreaming) return;

      setVoiceState('uploading');
      setVoiceError(null);
      setIsSending(true);

      try {
        setVoiceState('transcribing');
        const res = await chatApi.sendVoice({
          conversationId,
          audioBlob,
          fileName,
        });

        if (res.data) {
          setVoiceState('processing');

          const userVoiceMsg: ChatMessage = {
            id: `msg_voice_${Date.now()}`,
            conversationId,
            role: 'user',
            content: res.data.transcription || '🎤 Voice message',
            createdAt: new Date().toISOString(),
            inputType: 'voice',
          };

          const assistantMsg: ChatMessage = {
            id: res.data.messageId || `msg_asst_${Date.now() + 1}`,
            conversationId,
            role: 'assistant',
            content: res.data.replyContent,
            createdAt: new Date().toISOString(),
            audioUrl: res.data.audioUrl,
            hasPdf: true,
          };

          setMessages((prev) => [...prev, userVoiceMsg, assistantMsg]);
          setVoiceState('completed');
          refreshCredits();
          setTimeout(() => setVoiceState('idle'), 2000);
        }
      } catch (err: any) {
        setVoiceState('error');
        setVoiceError(err?.message || 'Failed to process voice message.');
        refreshCredits();
        // Return voiceState to idle after a delay so it does not stay permanently stuck in 'error'
        setTimeout(() => {
          setVoiceState((prev) => (prev === 'error' ? 'idle' : prev));
        }, 3000);
      } finally {
        setIsSending(false);
      }
    },
    [conversationId, isSending, isStreaming, refreshCredits]
  );

  const addSecurityScanMessage = useCallback(
    async (scan: import('../types').SecurityScan) => {
      const targetConvId = scan.conversationId || conversationId || '';

      const userMessage: ChatMessage = {
        id: `msg_user_scan_${Date.now()}`,
        conversationId: targetConvId,
        role: 'user',
        content: `${scan.targetUrl} adresindeki web uygulamasının kamuya açık dış yüzeyini güvenlik ve yapılandırma açısından analiz et. (Mod: ${scan.scanMode.toUpperCase()})`,
        createdAt: new Date().toISOString(),
        inputType: 'text',
      };

      const hasSummary = Boolean(scan.summary && scan.summary.trim());
      const initialContent =
        hasSummary && scan.status === 'Completed'
          ? `🛡️ **${scan.targetUrl}** güvenlik taraması tamamlandı.\n\n` +
            `**Bulgu Özeti:** Toplam ${scan.totalFindingsCount} bulgu tespit edildi. ` +
            `(Kritik: ${scan.criticalCount}, Yüksek: ${scan.highCount}, Orta: ${scan.mediumCount}, Düşük: ${scan.lowCount}, Bilgi: ${scan.infoCount})\n\n` +
            scan.summary!
          : `🛡️ **${scan.targetUrl}** için güvenlik & konfigürasyon taraması başlatıldı.\n\n` +
            `• **Hedef:** ${scan.normalizedTargetUrl || scan.targetUrl}\n` +
            `• **Test Modu:** ${scan.scanMode.toUpperCase()} (Saldırgan olmayan salt-okunur keşif ve konfigürasyon kontrolleri)\n` +
            `• **Hukuki Yetki:** Onaylandı\n` +
            `• **Metodoloji:** Saldırgan olmayan salt-okunur HTTP/TLS ve konfigürasyon kontrolleri\n\n` +
            `Güvenlik değerlendirmesi ve yapılandırma analizi aşağıda canlı olarak işlenmektedir.`;

      const scanMessage: ChatMessage = {
        id: `msg_scan_${scan.id}`,
        conversationId: targetConvId,
        role: 'assistant',
        content: initialContent,
        createdAt: new Date().toISOString(),
        securityScan: scan,
      };

      // 1. Optimistic append
      setMessages((prev) => [...prev, userMessage, scanMessage]);

      // 2. Sync from backend to fetch database message records
      if (targetConvId) {
        try {
          const res = await conversationApi.getMessages(targetConvId, 1, 500);
          if (res.data && res.data.length > 0) {
            setMessages(res.data);
          }
        } catch {
          // Keep optimistic messages
        }
      }
    },
    [conversationId]
  );

  const handleScanUpdated = useCallback(
    async (updatedScan: import('../types').SecurityScan) => {
      const targetConvId = updatedScan.conversationId || conversationId;

      // Immediately update the attached scan object and message content in local messages state
      setMessages((prev) =>
        prev.map((m) => {
          if (m.securityScan?.id === updatedScan.id || m.id === `msg_scan_${updatedScan.id}`) {
            const hasSummary = Boolean(updatedScan.summary && updatedScan.summary.trim());
            const updatedContent =
              hasSummary && updatedScan.status === 'Completed'
                ? `🛡️ **${updatedScan.targetUrl}** güvenlik taraması tamamlandı.\n\n` +
                  `**Bulgu Özeti:** Toplam ${updatedScan.totalFindingsCount} bulgu tespit edildi. ` +
                  `(Kritik: ${updatedScan.criticalCount}, Yüksek: ${updatedScan.highCount}, Orta: ${updatedScan.mediumCount}, Düşük: ${updatedScan.lowCount}, Bilgi: ${updatedScan.infoCount})\n\n` +
                  updatedScan.summary!
                : m.content;

            return {
              ...m,
              content: updatedContent,
              securityScan: updatedScan,
            };
          }
          return m;
        })
      );

      // When scan reaches terminal state, sync conversation messages from backend so message.content matches DB immediately
      if (
        targetConvId &&
        updatedScan.status !== 'Pending' &&
        updatedScan.status !== 'Running'
      ) {
        try {
          const res = await conversationApi.getMessages(targetConvId, 1, 500);
          if (res.data && res.data.length > 0) {
            setMessages(res.data);
          }
        } catch {
          // Keep existing state if sync fails
        }
      }
    },
    [conversationId]
  );

  return {
    messages,
    isLoadingMessages,
    loadError,
    isSending,
    isStreaming,
    streamError,
    voiceState,
    setVoiceState,
    voiceError,
    sendMessage,
    sendVoice,
    addSecurityScanMessage,
    handleScanUpdated,
  };
}
