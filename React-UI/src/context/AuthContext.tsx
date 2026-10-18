import React, { createContext, useContext, useState, useEffect, useCallback } from 'react';
import { authApi } from '../api/authApi';
import { userApi } from '../api/userApi';
import { apiClient } from '../api/apiClient';
import type {
  User,
  LoginRequest,
  RegisterRequest,
  ResetPasswordRequest,
  VerifyEmailRequest,
} from '../types';

interface AuthContextValue {
  user: User | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  accessToken: string | null;
  emailVerified: boolean;
  login: (data: LoginRequest) => Promise<void>;
  register: (data: RegisterRequest) => Promise<{ requiresVerification: boolean }>;
  logout: () => Promise<void>;
  refreshSession: () => Promise<boolean>;
  verifyEmail: (data: VerifyEmailRequest) => Promise<void>;
  resendVerification: (email: string) => Promise<void>;
  forgotPassword: (email: string) => Promise<void>;
  resetPassword: (data: ResetPasswordRequest) => Promise<void>;
  updateUser: (user: User) => void;
}

const USER_KEY = 'devpilot_user';
const REMEMBER_KEY = 'devpilot_remember_me';
const SESSION_ACTIVE_KEY = 'devpilot_session_active';

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [user, setUser] = useState<User | null>(() => {
    if (typeof window === 'undefined') return null;
    const isRemembered = localStorage.getItem(REMEMBER_KEY) === 'true';
    const isSessionActive = sessionStorage.getItem(SESSION_ACTIVE_KEY) === 'true';

    // If user did not check Remember Me and this is not an active session, do not restore
    if (!isRemembered && !isSessionActive) {
      localStorage.removeItem(USER_KEY);
      return null;
    }

    const saved = isRemembered ? localStorage.getItem(USER_KEY) : sessionStorage.getItem(USER_KEY);
    try {
      return saved ? JSON.parse(saved) : null;
    } catch {
      return null;
    }
  });

  // Always start in loading state until backend verifies session
  const [isLoading, setIsLoading] = useState<boolean>(true);

  // User is strictly authenticated when user exists and session is valid
  const isAuthenticated = !isLoading && !!user;
  const emailVerified = user?.emailVerified ?? false;

  const updateUser = useCallback((updated: User) => {
    setUser(updated);
    if (typeof window !== 'undefined') {
      const isRemembered = localStorage.getItem(REMEMBER_KEY) === 'true';
      if (isRemembered) {
        localStorage.setItem(USER_KEY, JSON.stringify(updated));
      } else {
        sessionStorage.setItem(USER_KEY, JSON.stringify(updated));
      }
    }
  }, []);

  // Handle automatic 401 callback
  useEffect(() => {
    apiClient.setOnUnauthorized(() => {
      setUser(null);
      apiClient.setAccessToken(null);
      if (typeof window !== 'undefined') {
        localStorage.removeItem(USER_KEY);
        sessionStorage.removeItem(USER_KEY);
        sessionStorage.removeItem(SESSION_ACTIVE_KEY);
      }
    });
  }, []);

  // Multi-tab synchronization (BroadcastChannel + storage event without tokens)
  useEffect(() => {
    const handleStorageChange = (e: StorageEvent) => {
      if (e.key === USER_KEY) {
        if (!e.newValue) {
          // Logged out in another tab
          setUser(null);
        } else {
          // Logged in or updated in another tab
          try {
            setUser(JSON.parse(e.newValue));
          } catch {}
        }
      }
    };

    window.addEventListener('storage', handleStorageChange);

    let authChannel: BroadcastChannel | null = null;
    try {
      if (typeof BroadcastChannel !== 'undefined') {
        authChannel = new BroadcastChannel('devpilot_auth_channel');
        authChannel.onmessage = (event) => {
          if (event.data?.type === 'LOGOUT') {
            setUser(null);
          } else if (event.data?.type === 'LOGIN' && event.data.user) {
            setUser(event.data.user);
          }
        };
      }
    } catch {}

    return () => {
      window.removeEventListener('storage', handleStorageChange);
      authChannel?.close();
    };
  }, []);

  // Session validation and silent refresh on mount via HttpOnly cookies
  const refreshSession = useCallback(async (): Promise<boolean> => {
    try {
      const isRemembered = typeof window !== 'undefined' && localStorage.getItem(REMEMBER_KEY) === 'true';
      const isSessionActive = typeof window !== 'undefined' && sessionStorage.getItem(SESSION_ACTIVE_KEY) === 'true';

      // If user did NOT check Remember Me and this is not an active session, do not restore
      if (!isRemembered && !isSessionActive) {
        setUser(null);
        apiClient.setAccessToken(null);
        if (typeof window !== 'undefined') {
          localStorage.removeItem(USER_KEY);
          sessionStorage.clear();
        }
        setIsLoading(false);
        return false;
      }

      // Always verify with backend /auth/refresh (X-Silent-Refresh: 1 -> HTTP 200)
      // If Docker/backend was restarted, refresh tokens were revoked on startup and this returns false cleanly without a 401 console error.
      const refreshed = await apiClient.refreshToken();
      if (!refreshed) {
        setUser(null);
        apiClient.setAccessToken(null);
        if (typeof window !== 'undefined') {
          localStorage.removeItem(USER_KEY);
          localStorage.removeItem(REMEMBER_KEY);
          sessionStorage.clear();
        }
        return false;
      }

      // Load current profile using fresh token
      const res = await userApi.getProfile();
      if (res?.data) {
        setUser(res.data);
        if (typeof window !== 'undefined') {
          sessionStorage.setItem(SESSION_ACTIVE_KEY, 'true');
          if (isRemembered) {
            localStorage.setItem(USER_KEY, JSON.stringify(res.data));
          } else {
            sessionStorage.setItem(USER_KEY, JSON.stringify(res.data));
          }
        }
        return true;
      }
      return false;
    } catch {
      // If validation or refresh failed, wipe user state
      setUser(null);
      apiClient.setAccessToken(null);
      if (typeof window !== 'undefined') {
        localStorage.removeItem(USER_KEY);
        localStorage.removeItem(REMEMBER_KEY);
        sessionStorage.clear();
      }
      return false;
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    refreshSession();
  }, [refreshSession]);

  // Re-verify session silently when user returns to the browser tab (e.g. after restarting Docker)
  useEffect(() => {
    if (typeof window === 'undefined') return;

    const handleVisibilityOrFocus = async () => {
      if (document.visibilityState === 'hidden') return;
      const isRemembered = localStorage.getItem(REMEMBER_KEY) === 'true';
      const isSessionActive = sessionStorage.getItem(SESSION_ACTIVE_KEY) === 'true';
      if (!isRemembered && !isSessionActive) return;

      const stillValid = await apiClient.refreshToken();
      if (!stillValid) {
        setUser(null);
        apiClient.setAccessToken(null);
        localStorage.removeItem(USER_KEY);
        localStorage.removeItem(REMEMBER_KEY);
        sessionStorage.clear();
      }
    };

    window.addEventListener('focus', handleVisibilityOrFocus);
    document.addEventListener('visibilitychange', handleVisibilityOrFocus);
    return () => {
      window.removeEventListener('focus', handleVisibilityOrFocus);
      document.removeEventListener('visibilitychange', handleVisibilityOrFocus);
    };
  }, []);

  const login = async (data: LoginRequest) => {
    const res = await authApi.login(data);
    if (res.data?.user) {
      setUser(res.data.user);
      if (res.data.accessToken) {
        apiClient.setAccessToken(res.data.accessToken);
      }

      if (typeof window !== 'undefined') {
        sessionStorage.setItem(SESSION_ACTIVE_KEY, 'true');
        if (data.rememberMe) {
          localStorage.setItem(REMEMBER_KEY, 'true');
          localStorage.setItem(USER_KEY, JSON.stringify(res.data.user));
        } else {
          localStorage.removeItem(REMEMBER_KEY);
          localStorage.removeItem(USER_KEY);
          sessionStorage.setItem(USER_KEY, JSON.stringify(res.data.user));
        }

        try {
          if (typeof BroadcastChannel !== 'undefined') {
            const channel = new BroadcastChannel('devpilot_auth_channel');
            channel.postMessage({
              type: 'LOGIN',
              user: res.data.user,
            });
            channel.close();
          }
        } catch {}
      }
    }
  };

  const register = async (data: RegisterRequest) => {
    const res = await authApi.register(data);
    return { requiresVerification: res.data?.requiresVerification ?? true };
  };

  const logout = async () => {
    setIsLoading(true);
    try {
      apiClient.setAccessToken(null);
      try {
        if (typeof BroadcastChannel !== 'undefined') {
          const channel = new BroadcastChannel('devpilot_auth_channel');
          channel.postMessage({ type: 'LOGOUT' });
          channel.close();
        }
      } catch {}

      await authApi.logout();
    } catch {
      // Ignore network errors during logout
    } finally {
      setUser(null);
      apiClient.setAccessToken(null);
      if (typeof window !== 'undefined') {
        try {
          localStorage.removeItem('devpilot_access_token');
          localStorage.removeItem('devpilot_refresh_token');
        } catch {}
        localStorage.removeItem(USER_KEY);
        localStorage.removeItem(REMEMBER_KEY);
        localStorage.removeItem('devpilot_selected_model');
        sessionStorage.clear();
      }
      setIsLoading(false);
    }
  };

  const verifyEmail = async (data: VerifyEmailRequest) => {
    await authApi.verifyEmail(data);
    if (user) {
      setUser({ ...user, emailVerified: true });
    }
  };

  const resendVerification = async (email: string) => {
    await authApi.resendVerification({ email });
  };

  const forgotPassword = async (email: string) => {
    await authApi.forgotPassword({ email });
  };

  const resetPassword = async (data: ResetPasswordRequest) => {
    await authApi.resetPassword(data);
  };

  return (
    <AuthContext.Provider
      value={{
        user,
        isAuthenticated,
        isLoading,
        accessToken: null,
        emailVerified,
        login,
        register,
        logout,
        refreshSession,
        verifyEmail,
        resendVerification,
        forgotPassword,
        resetPassword,
        updateUser,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
};

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
}
