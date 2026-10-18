import React, { createContext, useContext, useState, useEffect, useCallback, useMemo } from 'react';
import { useAuth } from './AuthContext';
import { chatApi } from '../api/chatApi';

export interface CreditContextValue {
  creditsRemaining: number;
  maxCredits: number;
  usedCredits: number;
  hasCredits: boolean;
  isByokActive: boolean;
  activeProvider: string;
  isUnlimited: boolean;
  subscriptionTier: string;
  timeUntilResetFormatted: string;
  userEmail: string | null;
  consumeCredit: () => boolean;
  updateRemainingCredits: (remaining: number) => void;
  refreshCredits: () => Promise<void>;
  setActiveProvider: (provider: string) => void;
}

const DEFAULT_DAILY_CREDITS = 15;

const CreditContext = createContext<CreditContextValue | undefined>(undefined);

export const CreditProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const { user, isAuthenticated } = useAuth();

  const [creditsRemaining, setCreditsRemaining] = useState<number>(DEFAULT_DAILY_CREDITS);
  const [maxCredits, setMaxCredits] = useState<number>(DEFAULT_DAILY_CREDITS);
  const [usedCredits, setUsedCredits] = useState<number>(0);
  const [subscriptionTier, setSubscriptionTier] = useState<string>('free');
  const [isByokActive, setIsByokActive] = useState<boolean>(false);
  const [nextResetAt, setNextResetAt] = useState<string | null>(null);
  const [timeUntilResetFormatted, setTimeUntilResetFormatted] = useState<string>('');

  const [activeProvider, setActiveProvider] = useState<string>(() => {
    try {
      const saved = localStorage.getItem('devpilot_selected_model');
      if (saved) {
        const parsed = JSON.parse(saved);
        if (parsed?.provider) return parsed.provider;
      }
    } catch {
      // ignore
    }
    return 'system';
  });

  const isUnlimited = useMemo(() => {
    return subscriptionTier === 'unlimited' || subscriptionTier === 'pro';
  }, [subscriptionTier]);

  const isCurrentModelByok = activeProvider !== 'system';

  const hasCredits = useMemo(() => {
    return isUnlimited || isCurrentModelByok || creditsRemaining > 0;
  }, [isUnlimited, isCurrentModelByok, creditsRemaining]);

  // Sync with backend authoritative quota service
  const refreshCredits = useCallback(async () => {
    if (!isAuthenticated) {
      setCreditsRemaining(DEFAULT_DAILY_CREDITS);
      setMaxCredits(DEFAULT_DAILY_CREDITS);
      setUsedCredits(0);
      setIsByokActive(false);
      setSubscriptionTier('free');
      setNextResetAt(null);
      return;
    }

    try {
      const res = await chatApi.getCredits();
      if (res?.data) {
        const total = typeof res.data.totalCredits === 'number' ? res.data.totalCredits : DEFAULT_DAILY_CREDITS;
        const used = typeof res.data.usedCredits === 'number' ? res.data.usedCredits : 0;
        const remaining = typeof res.data.remainingCredits === 'number'
          ? Math.max(0, res.data.remainingCredits)
          : Math.max(0, total - used);

        setMaxCredits(total);
        setUsedCredits(used);
        setCreditsRemaining(remaining);
        setSubscriptionTier((res.data as any).subscriptionTier || 'free');
        setIsByokActive(!!(res.data as any).isByokActive);

        if (res.data.nextResetAt) {
          setNextResetAt(res.data.nextResetAt);
        }
      }
    } catch {
      // Keep existing state if network glitch
    }
  }, [isAuthenticated]);

  // Reload credits on auth state change
  useEffect(() => {
    refreshCredits();
  }, [refreshCredits, user?.id]);

  // Compute countdown to reset
  useEffect(() => {
    const updateCountdown = () => {
      if (!nextResetAt) {
        setTimeUntilResetFormatted('');
        return;
      }

      const targetTime = new Date(nextResetAt).getTime();
      const now = Date.now();
      const remainingMs = targetTime - now;

      if (remainingMs <= 0) {
        setTimeUntilResetFormatted('0 dk');
        // Trigger server refresh when cycle elapses
        refreshCredits();
      } else {
        const hours = Math.floor(remainingMs / (1000 * 60 * 60));
        const minutes = Math.floor((remainingMs % (1000 * 60 * 60)) / (1000 * 60));
        if (hours > 0) {
          setTimeUntilResetFormatted(`${hours} sa ${minutes < 10 ? '0' : ''}${minutes} dk`);
        } else {
          setTimeUntilResetFormatted(`${minutes} dk`);
        }
      }
    };

    updateCountdown();
    const interval = setInterval(updateCountdown, 30000);
    return () => clearInterval(interval);
  }, [nextResetAt, refreshCredits]);

  // Real-time authoritative sync from response header
  const updateRemainingCredits = useCallback((remaining: number) => {
    setCreditsRemaining(Math.max(0, remaining));
  }, []);

  // Real-time optimistic credit deduction for default model
  const consumeCredit = useCallback((): boolean => {
    if (subscriptionTier === 'unlimited' || subscriptionTier === 'pro') return true;

    let success = false;
    setCreditsRemaining(prev => {
      if (prev <= 0) {
        success = false;
        return 0;
      }
      success = true;
      return prev - 1;
    });

    if (success) {
      setUsedCredits(prev => prev + 1);
    }

    return success;
  }, [subscriptionTier]);

  return (
    <CreditContext.Provider
      value={{
        creditsRemaining,
        maxCredits,
        usedCredits,
        hasCredits,
        isByokActive,
        activeProvider,
        isUnlimited,
        subscriptionTier,
        timeUntilResetFormatted,
        userEmail: user?.email || null,
        consumeCredit,
        updateRemainingCredits,
        refreshCredits,
        setActiveProvider,
      }}
    >
      {children}
    </CreditContext.Provider>
  );
};

export function useCredit(): CreditContextValue {
  const context = useContext(CreditContext);
  if (!context) {
    throw new Error('useCredit must be used within a CreditProvider');
  }
  return context;
}
