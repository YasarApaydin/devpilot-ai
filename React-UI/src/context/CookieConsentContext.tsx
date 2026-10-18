import React, {
  createContext,
  useContext,
  useState,
  useEffect,
  useCallback,
} from 'react';
import type {
  CookieCategoriesConsent,
  CookieCategoryKey,
  StoredCookieConsent,
} from '../types/cookieConsent';
import {
  cookieConsentService,
  CONSENT_UPDATED_EVENT,
} from '../services/cookieConsentService';

interface CookieConsentContextValue {
  consent: StoredCookieConsent | null;
  hasResponded: boolean;
  isBannerVisible: boolean;
  isPreferencesModalOpen: boolean;
  openPreferences: () => void;
  closePreferences: () => void;
  acceptAll: () => void;
  rejectAll: () => void;
  savePreferences: (categories: CookieCategoriesConsent) => void;
  resetConsent: () => void;
  hasConsent: (category: CookieCategoryKey) => boolean;
}

const CookieConsentContext = createContext<CookieConsentContextValue | undefined>(
  undefined
);

export const CookieConsentProvider: React.FC<{ children: React.ReactNode }> = ({
  children,
}) => {
  const [consent, setConsent] = useState<StoredCookieConsent | null>(() =>
    cookieConsentService.getStoredConsent()
  );
  const [isBannerVisible, setIsBannerVisible] = useState<boolean>(() => {
    // Only show banner if user has NOT responded yet
    return !cookieConsentService.hasResponded();
  });
  const [isPreferencesModalOpen, setIsPreferencesModalOpen] = useState<boolean>(false);

  // Sync state on external consent updates or storage changes
  useEffect(() => {
    const handleConsentEvent = (e: Event) => {
      const customEvent = e as CustomEvent<StoredCookieConsent | null>;
      const current = customEvent.detail ?? cookieConsentService.getStoredConsent();
      setConsent(current);
      setIsBannerVisible(current === null);
    };

    window.addEventListener(CONSENT_UPDATED_EVENT, handleConsentEvent);
    return () => {
      window.removeEventListener(CONSENT_UPDATED_EVENT, handleConsentEvent);
    };
  }, []);

  // Initial enforcement on load
  useEffect(() => {
    const stored = cookieConsentService.getStoredConsent();
    if (stored) {
      cookieConsentService.enforceConsent(stored);
    }
  }, []);

  const openPreferences = useCallback(() => {
    setIsPreferencesModalOpen(true);
  }, []);

  const closePreferences = useCallback(() => {
    setIsPreferencesModalOpen(false);
  }, []);

  const acceptAll = useCallback(() => {
    const updated = cookieConsentService.acceptAll();
    setConsent(updated);
    setIsBannerVisible(false);
    setIsPreferencesModalOpen(false);
  }, []);

  const rejectAll = useCallback(() => {
    const updated = cookieConsentService.rejectAll();
    setConsent(updated);
    setIsBannerVisible(false);
    setIsPreferencesModalOpen(false);
  }, []);

  const savePreferences = useCallback((categories: CookieCategoriesConsent) => {
    const updated = cookieConsentService.savePreferences(categories);
    setConsent(updated);
    setIsBannerVisible(false);
    setIsPreferencesModalOpen(false);
  }, []);

  const resetConsent = useCallback(() => {
    cookieConsentService.resetConsent();
    setConsent(null);
    setIsBannerVisible(true);
    setIsPreferencesModalOpen(false);
  }, []);

  const hasConsent = useCallback(
    (category: CookieCategoryKey): boolean => {
      if (category === 'necessary') return true;
      if (!consent) return false;
      return !!consent.categories[category];
    },
    [consent]
  );

  return (
    <CookieConsentContext.Provider
      value={{
        consent,
        hasResponded: consent !== null,
        isBannerVisible,
        isPreferencesModalOpen,
        openPreferences,
        closePreferences,
        acceptAll,
        rejectAll,
        savePreferences,
        resetConsent,
        hasConsent,
      }}
    >
      {children}
    </CookieConsentContext.Provider>
  );
};

export function useCookieConsent(): CookieConsentContextValue {
  const ctx = useContext(CookieConsentContext);
  if (!ctx) {
    throw new Error('useCookieConsent must be used within a CookieConsentProvider');
  }
  return ctx;
}
