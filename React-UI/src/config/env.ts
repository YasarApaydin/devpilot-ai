/**
 * Centralized Environment Configuration
 * Strictly exposes frontend-safe settings; no secret keys!
 */

interface EnvConfig {
  apiBaseUrl: string;
  appName: string;
  maxUploadMb: number;
  requestTimeoutMs: number;
  enableMockFallback: boolean;
}

export const ENV: EnvConfig = {
  apiBaseUrl: import.meta.env.VITE_API_BASE_URL || 'https://localhost:5001/api',
  appName: import.meta.env.VITE_APP_NAME || 'AI Agent',
  maxUploadMb: Number(import.meta.env.VITE_MAX_UPLOAD_MB) || 20,
  requestTimeoutMs: Number(import.meta.env.VITE_REQUEST_TIMEOUT_MS) || 30000,
  enableMockFallback: import.meta.env.VITE_ENABLE_MOCK_FALLBACK === 'true',
};
