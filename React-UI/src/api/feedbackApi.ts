import { apiClient } from './apiClient';
import { ENDPOINTS } from '../config/endpoints';
import type { ApiResponse } from '../types';

export interface SendFeedbackPayload {
  category: string;
  message: string;
  email?: string;
  name?: string;
  file?: File;
}

export const feedbackApi = {
  /**
   * Send user feedback, bug report, or feature suggestion directly to Yaşar Apaydın via email.
   * Endpoint: POST /api/feedback/send
   */
  async sendFeedback(payload: SendFeedbackPayload): Promise<ApiResponse<boolean>> {
    const formData = new FormData();
    formData.append('category', payload.category);
    formData.append('message', payload.message);
    if (payload.email) formData.append('email', payload.email);
    if (payload.name) formData.append('name', payload.name);
    if (payload.file) formData.append('attachment', payload.file);

    return await apiClient.upload<ApiResponse<boolean>>(
      ENDPOINTS.FEEDBACK.SEND,
      formData
    );
  },
};
