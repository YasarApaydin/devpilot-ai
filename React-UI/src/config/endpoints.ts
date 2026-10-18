/**
 * Centralized API Endpoints Contract
 * Components must NOT hardcode these routes.
 */

export const ENDPOINTS = {
  // Authentication & Session
  AUTH: {
    REGISTER: '/auth/register',
    LOGIN: '/auth/login',
    REFRESH: '/auth/refresh',
    LOGOUT: '/auth/logout',
    VERIFY_EMAIL: '/auth/verify-email',
    RESEND_VERIFICATION: '/auth/resend-verification',
    FORGOT_PASSWORD: '/auth/forgot-password',
    RESET_PASSWORD: '/auth/reset-password',
  },

  // Conversations
  CONVERSATIONS: {
    LIST: '/conversations',
    CREATE: '/conversations',
    DETAIL: (id: string) => `/conversations/${id}`,
    UPDATE: (id: string) => `/conversations/${id}`,
    PIN: (id: string) => `/conversations/${id}/pin`,
    DELETE: (id: string) => `/conversations/${id}`,
    MESSAGES: (id: string) => `/conversations/${id}/messages`,
    PDF: (id: string) => `/conversations/${id}/pdf`,
  },

  // Chat & AI Interactions
  CHAT: {
    STREAM: '/chat/stream',
    VOICE: '/chat/voice',
  },

  // Document Library
  LIBRARY: {
    LIST: '/library',
    UPLOAD: '/library/upload',
    DETAIL: (id: string) => `/library/${id}`,
    DELETE: (id: string) => `/library/${id}`,
  },

  // User Profile
  PROFILE: {
    GET: '/profile',
    UPDATE: '/profile',
    CHANGE_PASSWORD: '/profile/change-password',
    DELETE_ACCOUNT: '/profile/account',
  },

  // PDF Export
  PDF: {
    MESSAGE: (id: string) => `/messages/${id}/pdf`,
    CONVERSATION: (id: string) => `/conversations/${id}/pdf`,
  },

  // Security Scans
  SECURITY_SCANS: {
    CATEGORIES: '/security-scans/categories',
    VALIDATE_TARGET: '/security-scans/validate-target',
    CREATE: '/security-scans',
    LIST: '/security-scans',
    DETAIL: (id: string) => `/security-scans/${id}`,
    CANCEL: (id: string) => `/security-scans/${id}/cancel`,
    REPORT_PDF: (id: string) => `/security-scans/${id}/report/pdf`,
    CONSENT_PDF: (id: string) => `/security-scans/${id}/consent/pdf`,
  },

  // AI Keys & BYOK Model Selection
  AI_KEYS: {
    LIST: '/ai-keys',
    SAVE: '/ai-keys',
    TEST: '/ai-keys/test',
    DELETE: (provider: string) => `/ai-keys/${provider}`,
    MODELS: '/ai-keys/models',
    PREFERENCE: '/ai-keys/preference',
  },

  // User Feedback & Bug Reports
  FEEDBACK: {
    SEND: '/feedback/send',
  },
} as const;
