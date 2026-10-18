import { apiClient } from './apiClient';
import { ENDPOINTS } from '../config/endpoints';
import type {
  User,
  UpdateProfileRequest,
  ChangePasswordRequest,
  ApiResponse,
} from '../types';

export const userApi = {
  async getProfile(): Promise<ApiResponse<User>> {
    return await apiClient.get<ApiResponse<User>>(ENDPOINTS.PROFILE.GET);
  },

  async updateProfile(data: UpdateProfileRequest): Promise<ApiResponse<User>> {
    return await apiClient.put<ApiResponse<User>>(ENDPOINTS.PROFILE.UPDATE, data);
  },

  async changePassword(data: ChangePasswordRequest): Promise<ApiResponse<void>> {
    return await apiClient.post<ApiResponse<void>>(
      ENDPOINTS.PROFILE.CHANGE_PASSWORD,
      data
    );
  },

  async deleteAccount(): Promise<ApiResponse<void>> {
    return await apiClient.delete<ApiResponse<void>>(ENDPOINTS.PROFILE.DELETE_ACCOUNT);
  },
};
