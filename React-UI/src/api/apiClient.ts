import axios, { type AxiosError, type AxiosInstance, type InternalAxiosRequestConfig } from 'axios';
import { ENV } from '../config/env';
import type { ApiError } from '../types/api';

const USER_KEY = 'devpilot_user';
const DEVICE_KEY = 'devpilot_device_id';

class ApiClient {
  private client: AxiosInstance;
  private deviceId: string | null = null;
  private refreshPromise: Promise<boolean> | null = null;
  private onUnauthorizedCallback: (() => void) | null = null;

  constructor() {
    // Clean up any legacy localStorage token keys
    if (typeof window !== 'undefined') {
      try {
        localStorage.removeItem('devpilot_access_token');
        localStorage.removeItem('devpilot_refresh_token');
      } catch {}
    }

    this.client = axios.create({
      baseURL: ENV.apiBaseUrl,
      timeout: ENV.requestTimeoutMs,
      headers: {
        'Content-Type': 'application/json',
        Accept: 'application/json',
      },
      withCredentials: true, // Allows HttpOnly cookies (accessToken, refreshToken)
    });

    this.setupInterceptors();
  }

  public getDeviceId(): string {
    if (this.deviceId) return this.deviceId;
    if (typeof window === 'undefined') return 'server_side_device';

    let id = localStorage.getItem(DEVICE_KEY);
    if (!id) {
      if (typeof crypto !== 'undefined' && crypto.randomUUID) {
        id = crypto.randomUUID();
      } else {
        id = 'dp_' + Math.random().toString(36).substring(2, 15) + '_' + Date.now();
      }
      localStorage.setItem(DEVICE_KEY, id);
    }

    try {
      document.cookie = `dp_device_id=${encodeURIComponent(id)}; path=/; max-age=31536000; SameSite=Lax`;
    } catch {
      // Ignore cookie errors
    }

    this.deviceId = id;
    return id;
  }

  private accessToken: string | null = null;

  public setAccessToken(token: string | null) {
    // Kept strictly in memory - NEVER written to localStorage to protect against XSS token exfiltration
    this.accessToken = token;
    if (typeof window !== 'undefined') {
      try {
        localStorage.removeItem('devpilot_access_token');
        localStorage.removeItem('devpilot_refresh_token');
      } catch {}
    }
  }

  public getAccessToken(): string | null {
    // Strictly in-memory token, never read from localStorage
    return this.accessToken;
  }

  public setOnUnauthorized(callback: () => void) {
    this.onUnauthorizedCallback = callback;
  }

  private setupInterceptors() {
    // Request interceptor: attach device id header and Authorization token
    this.client.interceptors.request.use(
      (config: InternalAxiosRequestConfig) => {
        const deviceId = this.getDeviceId();
        if (config.headers) {
          config.headers['X-Device-Id'] = deviceId;
          const token = this.getAccessToken();
          if (token) {
            config.headers['Authorization'] = `Bearer ${token}`;
          } else {
            delete config.headers['Authorization'];
          }
        }
        return config;
      },
      (error) => Promise.reject(error)
    );

    // Response interceptor: standard error handling, 401 refresh, 429 rate limit
    this.client.interceptors.response.use(
      (response) => response,
      async (error: AxiosError<any>) => {
        const originalRequest = error.config as (InternalAxiosRequestConfig & { _retry?: boolean }) | undefined;

        // Rate limiting (429)
        if (error.response?.status === 429) {
          const apiError: ApiError = {
            statusCode: 429,
            message: (error.response?.data as any)?.message || 'Too many requests. Please try again later.',
            code: 'RATE_LIMITED',
          };
          return Promise.reject(apiError);
        }

        // Unauthorized (401)
        const isAuthEndpoint = originalRequest?.url?.includes('/auth/');
        if (error.response?.status === 401 && originalRequest && !originalRequest._retry && !isAuthEndpoint) {
          originalRequest._retry = true;

          // If a parallel request already rotated the in-memory token while this request was in flight,
          // retry immediately with the updated token without triggering another refresh call.
          const sentAuthHeader = originalRequest.headers?.['Authorization'] as string | undefined;
          const currentInMemoryToken = this.getAccessToken();
          if (
            currentInMemoryToken &&
            sentAuthHeader &&
            sentAuthHeader !== `Bearer ${currentInMemoryToken}`
          ) {
            originalRequest.headers['Authorization'] = `Bearer ${currentInMemoryToken}`;
            return this.client(originalRequest);
          }

          let refreshed = false;
          try {
            // Attempt silent session refresh via HttpOnly cookie (deduplicated via refreshPromise)
            refreshed = await this.refreshToken();
          } catch {
            refreshed = false;
          }

          if (refreshed) {
            // Replace or remove stale Bearer Authorization header so retry uses the fresh token or HttpOnly cookie
            if (originalRequest.headers) {
              const freshToken = this.getAccessToken();
              if (freshToken) {
                originalRequest.headers['Authorization'] = `Bearer ${freshToken}`;
              } else {
                delete originalRequest.headers['Authorization'];
              }
            }
            return this.client(originalRequest);
          }

          this.setAccessToken(null);
          if (typeof window !== 'undefined') {
            try {
              localStorage.removeItem(USER_KEY);
              sessionStorage.removeItem(USER_KEY);
            } catch {}
          }
          if (this.onUnauthorizedCallback) {
            this.onUnauthorizedCallback();
          }
        }

        // Format error object
        const formattedError: ApiError = {
          statusCode: error.response?.status || 0,
          message:
            (error.response?.data as any)?.message ||
            (error.response?.data as any)?.title ||
            (error.code === 'ECONNABORTED'
              ? 'Request timed out. Please try again.'
              : error.message || 'An unexpected network error occurred.'),
          errors: (error.response?.data as any)?.errors,
          code: (error.response?.data as any)?.code || error.code,
        };

        return Promise.reject(formattedError);
      }
    );
  }

  public async refreshToken(): Promise<boolean> {
    if (this.refreshPromise) {
      return this.refreshPromise;
    }

    this.refreshPromise = (async () => {
      try {
        // Backend reads HttpOnly refreshToken cookie, rotates tokens, sets fresh cookies and returns new accessToken in body
        const res = await axios.post(
          `${ENV.apiBaseUrl}/auth/refresh`,
          {},
          {
            withCredentials: true,
            headers: {
              'X-Silent-Refresh': '1',
              'X-Device-Id': this.getDeviceId(),
            },
          }
        );

        const newAccessToken = (res.data as any)?.data?.accessToken;
        if ((res.data as any)?.success !== false && newAccessToken) {
          this.setAccessToken(newAccessToken);
          return true;
        }

        this.setAccessToken(null);
        return false;
      } catch {
        this.setAccessToken(null);
        return false;
      } finally {
        this.refreshPromise = null;
      }
    })();

    return this.refreshPromise;
  }

  public getRawClient(): AxiosInstance {
    return this.client;
  }

  public async get<T>(url: string, params?: Record<string, any>): Promise<T> {
    const res = await this.client.get<T>(url, { params });
    return res.data;
  }

  public async post<T>(url: string, data?: any): Promise<T> {
    const res = await this.client.post<T>(url, data);
    return res.data;
  }

  public async put<T>(url: string, data?: any): Promise<T> {
    const res = await this.client.put<T>(url, data);
    return res.data;
  }

  public async patch<T>(url: string, data?: any): Promise<T> {
    const res = await this.client.patch<T>(url, data);
    return res.data;
  }

  public async delete<T>(url: string): Promise<T> {
    const res = await this.client.delete<T>(url);
    return res.data;
  }

  public async upload<T>(
    url: string,
    formData: FormData,
    onProgress?: (percent: number) => void
  ): Promise<T> {
    const res = await this.client.post<T>(url, formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
      onUploadProgress: (progressEvent) => {
        if (progressEvent.total && onProgress) {
          const percent = Math.round((progressEvent.loaded * 100) / progressEvent.total);
          onProgress(percent);
        }
      },
    });
    return res.data;
  }
}

export const apiClient = new ApiClient();
