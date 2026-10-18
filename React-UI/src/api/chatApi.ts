import { apiClient } from './apiClient';
import { ENDPOINTS } from '../config/endpoints';
import { ENV } from '../config/env';
import type {
  StreamChatRequest,
  VoiceChatRequest,
  VoiceChatResponse,
  ApiResponse,
} from '../types';

export interface StreamMeta {
  modelUsed?: string;
  fallbackReason?: string;
  originalModel?: string;
  remainingCredits?: number;
}

export const chatApi = {
  /**
   * Stream AI response for a chat message.
   * Calls POST /api/chat/stream with fetch / ReadableStream.
   */
  async streamMessage(
    request: StreamChatRequest,
    onChunk: (chunk: string) => void,
    onComplete: (fullText: string, meta?: StreamMeta) => void,
    onError: (error: any) => void,
    onHeaders?: (meta: StreamMeta) => void
  ): Promise<void> {
    try {
      const deviceId = apiClient.getDeviceId();
      const executeStreamFetch = async () => {
        const headers: Record<string, string> = {
          'Content-Type': 'application/json',
          'X-Device-Id': deviceId,
          'X-Silent-Stream-Error': '1',
        };
        const token = apiClient.getAccessToken();
        if (token) {
          headers['Authorization'] = `Bearer ${token}`;
        }
        return fetch(`${ENV.apiBaseUrl}${ENDPOINTS.CHAT.STREAM}`, {
          method: 'POST',
          credentials: 'include', // Transmit HttpOnly auth cookies automatically
          headers,
          body: JSON.stringify(request),
        });
      };

      let response = await executeStreamFetch();

      // If access token expired mid-session, silently refresh once and retry
      if (response.status === 401) {
        const refreshed = await apiClient.refreshToken();
        if (refreshed) {
          response = await executeStreamFetch();
        }
      }

      const streamErrorHeader = response.headers.get('X-Stream-Error');
      if (!response.ok || streamErrorHeader) {
        const errText = await response.text().catch(() => '');
        let parsedMessage = errText;
        try {
          const parsed = JSON.parse(errText);
          if (parsed?.error) parsedMessage = parsed.error;
        } catch {
          // Plain text response
        }
        if (response.status === 429 || streamErrorHeader === 'QUOTA_EXHAUSTED') {
          throw new Error(parsedMessage || 'Günlük kredi limitiniz doldu veya sağlayıcı hız sınırına ulaşıldı.');
        }
        if (response.status === 400 || response.status === 401) {
          throw new Error(parsedMessage || `İstek başarısız oldu (${response.status}).`);
        }
        throw new Error(parsedMessage || `Chat stream failed with status ${response.status}`);
      }

      const safeDecode = (val: string | null): string | undefined => {
        if (!val) return undefined;
        try {
          return decodeURIComponent(val);
        } catch {
          return val;
        }
      };

      const modelUsed = safeDecode(response.headers.get('X-Model-Used'));
      const fallbackReason = safeDecode(response.headers.get('X-Fallback-Reason'));
      const originalModel = safeDecode(response.headers.get('X-Original-Model'));
      const remainingCreditsHeader = response.headers.get('X-Remaining-Credits');
      const parsedCredits = remainingCreditsHeader !== null ? parseInt(remainingCreditsHeader, 10) : NaN;
      const remainingCredits = !isNaN(parsedCredits) ? parsedCredits : undefined;

      const meta: StreamMeta = { modelUsed, fallbackReason, originalModel, remainingCredits };
      onHeaders?.(meta);

      if (!response.body) {
        throw new Error('ReadableStream not supported by browser or empty body.');
      }

      const reader = response.body.getReader();
      const decoder = new TextDecoder();
      let accumulated = '';

      while (true) {
        const { value, done } = await reader.read();
        if (done) break;
        const chunk = decoder.decode(value, { stream: true });
        accumulated += chunk;
        onChunk(chunk);
      }

      onComplete(accumulated, { modelUsed, fallbackReason, originalModel, remainingCredits });
    } catch (err: any) {
      if (ENV.enableMockFallback) {
        // Safe simulated response explaining architecture conceptually
        const mockResponses = [
          `Harika bir soru! "${request.message}" konusunu DevPilot AI mimarisiyle inceledim.\n\nDevPilot AI; .NET 9 Clean Architecture, RAG doküman kütüphanesi, LangGraph bağlamsal hafıza ve LPU hızlandırılmış çıkarım motorlarını bir araya getiren yeni nesil bir çalışma ortamıdır.\n\nİstediğiniz konuyu daha detaylı kavramsal olarak inceleyebiliriz.`,
          `Sorunuzu analiz ettim:\n\nDevPilot AI altyapısında kullanıcı gizliliği ve anahtar güvenliği en yüksek standartlarda korunur (Bring Your Own Key - BYOK). Tüm istekler sunucu tarafında doğrulanır ve modele güvenli biçimde aktarılır.`,
        ];

        const text =
          mockResponses[Math.floor(Math.random() * mockResponses.length)];
        const words = text.split(' ');
        let accumulated = '';

        for (let i = 0; i < words.length; i++) {
          await new Promise((res) => setTimeout(res, 35));
          const chunk = (i === 0 ? '' : ' ') + words[i];
          accumulated += chunk;
          onChunk(chunk);
        }

        onComplete(accumulated, { modelUsed: 'DevPilot Default (Simüle)' });
        return;
      }

      onError(err);
    }
  },

  /**
   * Send recorded voice audio to backend for transcription and AI response.
   * Endpoint: POST /api/chat/voice
   */
  async sendVoice(request: VoiceChatRequest): Promise<ApiResponse<VoiceChatResponse>> {
    const formData = new FormData();
    formData.append('conversationId', request.conversationId);
    formData.append('audioFile', request.audioBlob, request.fileName || 'voice_recording.wav');

    try {
      return await apiClient.upload<ApiResponse<VoiceChatResponse>>(
        ENDPOINTS.CHAT.VOICE,
        formData
      );
    } catch (error) {
      if (ENV.enableMockFallback) {
        // Realistic simulated voice response
        await new Promise((res) => setTimeout(res, 1200));
        return {
          success: true,
          data: {
            messageId: `msg_${Date.now()}`,
            transcription: 'DevPilot AI sisteminde hafıza ve ses özellikleri nasıl çalışır?',
            replyContent:
              'Sesli kaydınız Whisper Large V3 Turbo ile analiz edildi. LangGraph hafızası oturum boyunca önceki sorularınızı hatırlar ve gTTS ile Türkçe sesli yanıt oluşturabilir.',
          },
        };
      }
      throw error;
    }
  },

  /**
   * Get user daily credit status (15 credits per 24 hours).
   * Endpoint: GET /api/chat/credits
   */
  async getCredits(): Promise<ApiResponse<{
    email?: string;
    totalCredits: number;
    usedCredits: number;
    remainingCredits: number;
    nextResetAt?: string;
  }>> {
    return await apiClient.get<ApiResponse<any>>(`/chat/credits?_t=${Date.now()}`);
  },
};
