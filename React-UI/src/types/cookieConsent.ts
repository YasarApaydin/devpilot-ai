export type CookieCategoryKey = 'necessary' | 'analytics' | 'functional' | 'marketing';

export interface CookieCategoriesConsent {
  necessary: boolean; // Her zaman true
  analytics: boolean;
  functional: boolean;
  marketing: boolean;
}

export interface StoredCookieConsent {
  version: string;
  consentedAt: string; // ISO 8601 string
  categories: CookieCategoriesConsent;
}

export interface CookieItemDefinition {
  name: string;
  provider: string;
  category: CookieCategoryKey;
  purpose: string;
  duration: string;
  type: 'HttpOnly Çerez' | 'Çerez' | 'LocalStorage' | 'SessionStorage';
}

export const DEVPILOT_COOKIES: CookieItemDefinition[] = [
  {
    name: 'accessToken',
    provider: 'DevPilot AI (.NET Backend API)',
    category: 'necessary',
    purpose: 'Kullanıcının güvenli kimlik doğrulamasını ve API isteklerinin yetkilendirilmesini sağlar.',
    duration: '60 Dakika',
    type: 'HttpOnly Çerez',
  },
  {
    name: 'refreshToken',
    provider: 'DevPilot AI (.NET Backend API)',
    category: 'necessary',
    purpose: 'Kullanıcı oturumunun güvenli biçimde arka planda yenilenmesini sağlar (Beni Hatırla seçeneğinde 30 gün).',
    duration: '30 Gün / Oturum',
    type: 'HttpOnly Çerez',
  },
  {
    name: 'dp_device_id',
    provider: 'DevPilot AI Client & Backend',
    category: 'necessary',
    purpose: 'Yetkisiz erişimleri, oturum kaçırmayı (session hijacking) ve brute-force saldırılarını engellemek için anonim cihaz kimliği.',
    duration: '1 Yıl',
    type: 'Çerez',
  },
  {
    name: 'devpilot_cookie_consent_v1',
    provider: 'DevPilot AI Web Client',
    category: 'necessary',
    purpose: 'Kullanıcının vermiş olduğu çerez izin tercihlerini ve onay tarihini yerel olarak saklar.',
    duration: '1 Yıl',
    type: 'LocalStorage',
  },
  {
    name: 'devpilot_selected_model',
    provider: 'DevPilot AI Web Client',
    category: 'functional',
    purpose: 'Kullanıcının sohbet ekranında en son tercih ettiği yapay zekâ modelini (örn. Gemini, Groq, Claude) hatırlar.',
    duration: 'Kalıcı (İzin kaldırılana kadar)',
    type: 'LocalStorage',
  },
  {
    name: 'devpilot_remember_me',
    provider: 'DevPilot AI Web Client',
    category: 'functional',
    purpose: 'Giriş ekranında "Beni Hatırla" tercih durumunu ve isteğe bağlı e-posta bilgisini yerel olarak hatırlar.',
    duration: 'Kullanıcı isteğine bağlı',
    type: 'LocalStorage',
  },
  {
    name: 'devpilot_session_active',
    provider: 'DevPilot AI Web Client',
    category: 'functional',
    purpose: 'Aktif tarayıcı sekmesinde oturum sürekliliğini kontrol eder.',
    duration: 'Tarayıcı Oturumu',
    type: 'SessionStorage',
  },
];
