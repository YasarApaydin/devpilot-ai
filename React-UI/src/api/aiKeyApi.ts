import { apiClient } from './apiClient';
import { ENDPOINTS } from '../config/endpoints';
import type {
  ApiResponse,
  UserAiKey,
  SaveAiKeyRequest,
  TestAiKeyRequest,
  TestAiKeyResponse,
  AiModelsResponse,
  SetAiPreferenceRequest,
} from '../types';

export const aiKeyApi = {
  getUserAiKeys: () =>
    apiClient.get<ApiResponse<UserAiKey[]>>(ENDPOINTS.AI_KEYS.LIST),

  saveAiKey: (data: SaveAiKeyRequest) =>
    apiClient.post<ApiResponse<UserAiKey>>(ENDPOINTS.AI_KEYS.SAVE, data),

  testAiKey: (data: TestAiKeyRequest) =>
    apiClient.post<ApiResponse<TestAiKeyResponse>>(ENDPOINTS.AI_KEYS.TEST, data),

  deleteAiKey: (provider: string) =>
    apiClient.delete<ApiResponse<boolean>>(ENDPOINTS.AI_KEYS.DELETE(provider)),

  getAvailableModels: () =>
    apiClient.get<ApiResponse<AiModelsResponse>>(ENDPOINTS.AI_KEYS.MODELS),

  setPreference: (data: SetAiPreferenceRequest) =>
    apiClient.put<ApiResponse<any>>(ENDPOINTS.AI_KEYS.PREFERENCE, data),
};
