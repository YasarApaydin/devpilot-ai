import { apiClient } from './apiClient';
import { ENDPOINTS } from '../config/endpoints';
import type {
  AuthResponse,
  LoginRequest,
  RegisterRequest,
  VerifyEmailRequest,
  ResendVerificationRequest,
  ForgotPasswordRequest,
  ResetPasswordRequest,
  ApiResponse,
} from '../types';

export const authApi = {
  async register(data: RegisterRequest): Promise<ApiResponse<{ message: string; requiresVerification: boolean }>> {
    return await apiClient.post<ApiResponse<{ message: string; requiresVerification: boolean }>>(
      ENDPOINTS.AUTH.REGISTER,
      data
    );
  },

  async login(data: LoginRequest): Promise<ApiResponse<AuthResponse>> {
    return await apiClient.post<ApiResponse<AuthResponse>>(ENDPOINTS.AUTH.LOGIN, data);
  },

  async refreshToken(refreshToken?: string | null): Promise<ApiResponse<AuthResponse>> {
    return await apiClient.post<ApiResponse<AuthResponse>>(
      ENDPOINTS.AUTH.REFRESH,
      refreshToken ? { refreshToken } : {}
    );
  },

  async logout(): Promise<ApiResponse<void>> {
    return await apiClient.post<ApiResponse<void>>(ENDPOINTS.AUTH.LOGOUT);
  },

  async verifyEmail(data: VerifyEmailRequest): Promise<ApiResponse<{ verified: boolean; message: string }>> {
    return await apiClient.post<ApiResponse<{ verified: boolean; message: string }>>(
      ENDPOINTS.AUTH.VERIFY_EMAIL,
      data
    );
  },

  async resendVerification(data: ResendVerificationRequest): Promise<ApiResponse<{ message: string }>> {
    return await apiClient.post<ApiResponse<{ message: string }>>(
      ENDPOINTS.AUTH.RESEND_VERIFICATION,
      data
    );
  },

  async forgotPassword(data: ForgotPasswordRequest): Promise<ApiResponse<{ message: string }>> {
    return await apiClient.post<ApiResponse<{ message: string }>>(
      ENDPOINTS.AUTH.FORGOT_PASSWORD,
      data
    );
  },

  async resetPassword(data: ResetPasswordRequest): Promise<ApiResponse<{ message: string }>> {
    return await apiClient.post<ApiResponse<{ message: string }>>(
      ENDPOINTS.AUTH.RESET_PASSWORD,
      data
    );
  },
};
