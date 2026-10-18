import type {
  CookieCategoriesConsent,
  CookieCategoryKey,
  StoredCookieConsent,
} from '../types/cookieConsent';

export const CURRENT_COOKIE_POLICY_VERSION = '1.0';
export const COOKIE_CONSENT_STORAGE_KEY = 'devpilot_cookie_consent_v1';
export const CONSENT_UPDATED_EVENT = 'devpilot:consent_updated';

const DEFAULT_CATEGORIES: CookieCategoriesConsent = {
  necessary: true,
  analytics: false,
  functional: false,
  marketing: false,
};

class CookieConsentService {
  /**
   * Reads stored consent from localStorage and validates policy version.
   */
  public getStoredConsent(): StoredCookieConsent | null {
    if (typeof window === 'undefined') return null;

    try {
      const raw = localStorage.getItem(COOKIE_CONSENT_STORAGE_KEY);
      if (!raw) return null;

      const parsed = JSON.parse(raw) as StoredCookieConsent;
      if (!parsed || typeof parsed !== 'object') return null;

      // Check version compatibility
      if (parsed.version !== CURRENT_COOKIE_POLICY_VERSION) {
        // If major version changed, user must be re-prompted
        return null;
      }

      // Ensure necessary is always true
      parsed.categories.necessary = true;
      return parsed;
    } catch {
      return null;
    }
  }

  /**
   * Checks whether the user has responded to the consent prompt for the current policy version.
   */
  public hasResponded(): boolean {
    return this.getStoredConsent() !== null;
  }

  /**
   * Checks if a specific cookie category has been consented to.
   */
  public hasConsent(category: CookieCategoryKey): boolean {
    if (category === 'necessary') return true;
    const stored = this.getStoredConsent();
    if (!stored) return false; // Default is strict: false until consented
    return !!stored.categories[category];
  }

  /**
   * Saves custom preferences and enforces them.
   */
  public savePreferences(
    categories: Partial<CookieCategoriesConsent>
  ): StoredCookieConsent {
    const consentPayload: StoredCookieConsent = {
      version: CURRENT_COOKIE_POLICY_VERSION,
      consentedAt: new Date().toISOString(),
      categories: {
        necessary: true, // Always locked to true
        analytics: Boolean(categories.analytics),
        functional: Boolean(categories.functional),
        marketing: Boolean(categories.marketing),
      },
    };

    if (typeof window !== 'undefined') {
      try {
        localStorage.setItem(
          COOKIE_CONSENT_STORAGE_KEY,
          JSON.stringify(consentPayload)
        );
      } catch (err) {
        console.error('Failed to save cookie consent to localStorage', err);
      }

      this.enforceConsent(consentPayload);
      this.dispatchConsentEvent(consentPayload);
    }

    return consentPayload;
  }

  /**
   * Accepts all cookie categories.
   */
  public acceptAll(): StoredCookieConsent {
    return this.savePreferences({
      necessary: true,
      analytics: true,
      functional: true,
      marketing: true,
    });
  }

  /**
   * Rejects all optional categories (keeps only strictly necessary).
   */
  public rejectAll(): StoredCookieConsent {
    return this.savePreferences({
      necessary: true,
      analytics: false,
      functional: false,
      marketing: false,
    });
  }

  /**
   * Clears consent record so user can re-trigger initial review.
   */
  public resetConsent(): void {
    if (typeof window !== 'undefined') {
      try {
        localStorage.removeItem(COOKIE_CONSENT_STORAGE_KEY);
      } catch {}

      // Enforce strict necessary only on reset
      this.enforceConsent({
        version: CURRENT_COOKIE_POLICY_VERSION,
        consentedAt: new Date().toISOString(),
        categories: { ...DEFAULT_CATEGORIES },
      });

      this.dispatchConsentEvent(null);
    }
  }

  /**
   * Enforces user choices in the browser environment.
   * Cleans up storage if functional permissions are revoked.
   * NOTE: Strictly necessary auth cookies (accessToken, refreshToken, dp_device_id)
   * are never deleted as they are required for security & core operation.
   */
  public enforceConsent(consent: StoredCookieConsent): void {
    if (typeof window === 'undefined') return;

    try {
      // 1. Functional Category: If rejected, clean up model selection & remember me storage
      if (!consent.categories.functional) {
        localStorage.removeItem('devpilot_selected_model');
        // Do not touch devpilot_user or devpilot_device_id as they are necessary for session
      }

      // 2. Analytics Category: If rejected, disable any window.__devpilot_analytics flag
      if (!consent.categories.analytics) {
        (window as any).__devpilot_telemetry_disabled = true;
      } else {
        (window as any).__devpilot_telemetry_disabled = false;
      }

      // 3. Marketing Category: If rejected, disable marketing flags
      if (!consent.categories.marketing) {
        (window as any).__devpilot_marketing_disabled = true;
      } else {
        (window as any).__devpilot_marketing_disabled = false;
      }
    } catch (e) {
      console.warn('Error during cookie consent enforcement:', e);
    }
  }

  private dispatchConsentEvent(consent: StoredCookieConsent | null) {
    if (typeof window !== 'undefined') {
      window.dispatchEvent(
        new CustomEvent(CONSENT_UPDATED_EVENT, { detail: consent })
      );
    }
  }
}

export const cookieConsentService = new CookieConsentService();
