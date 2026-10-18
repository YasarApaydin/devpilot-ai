import { apiClient } from './apiClient';
import { ENDPOINTS } from '../config/endpoints';
import type {
  ScanCategory,
  ValidateTargetRequest,
  ValidateTargetResponse,
  CreateSecurityScanRequest,
  SecurityScan,
  SecurityScanSummary,
} from '../types';

export const securityScanApi = {
  /**
   * Güvenlik motoru tarafından desteklenen kategorileri getirir.
   */
  async getCategories(): Promise<ScanCategory[]> {
    return await apiClient.get<ScanCategory[]>(ENDPOINTS.SECURITY_SCANS.CATEGORIES);
  },

  /**
   * Hedef URL'yi SSRF, yerel ağ ve DNS güvenliği açısından doğrular.
   */
  async validateTarget(targetUrl: string): Promise<ValidateTargetResponse> {
    const payload: ValidateTargetRequest = { targetUrl };
    return await apiClient.post<ValidateTargetResponse>(
      ENDPOINTS.SECURITY_SCANS.VALIDATE_TARGET,
      payload
    );
  },

  /**
   * Yasal onay ve kapsam seçimi ile yeni bir güvenlik taraması başlatır.
   */
  async createScan(request: CreateSecurityScanRequest): Promise<SecurityScan> {
    return await apiClient.post<SecurityScan>(ENDPOINTS.SECURITY_SCANS.CREATE, request);
  },

  /**
   * Kullanıcıya ait tüm taramaları listeler.
   */
  async listScans(): Promise<SecurityScanSummary[]> {
    return await apiClient.get<SecurityScanSummary[]>(ENDPOINTS.SECURITY_SCANS.LIST);
  },

  /**
   * Belirli bir taramanın detaylarını ve anlık durumunu getirir.
   */
  async getScan(id: string): Promise<SecurityScan> {
    return await apiClient.get<SecurityScan>(ENDPOINTS.SECURITY_SCANS.DETAIL(id));
  },

  /**
   * Devam eden taramayı iptal eder.
   */
  async cancelScan(id: string): Promise<{ message: string }> {
    return await apiClient.post<{ message: string }>(ENDPOINTS.SECURITY_SCANS.CANCEL(id));
  },

  /**
   * Güvenlik değerlendirme teknik raporunu PDF olarak indirir.
   */
  async downloadReportPdf(id: string, fileName?: string): Promise<void> {
    const rawClient = apiClient.getRawClient();
    const response = await rawClient.get(ENDPOINTS.SECURITY_SCANS.REPORT_PDF(id), {
      responseType: 'blob',
    });

    const blob = new Blob([response.data], { type: 'application/pdf' });
    const url = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = fileName || `DevPilot-Security-Report-${id.slice(0, 8)}.pdf`;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    window.URL.revokeObjectURL(url);
  },

  /**
   * Yasal onay belgesini (Consent Audit) PDF olarak indirir.
   */
  async downloadConsentPdf(id: string, fileName?: string): Promise<void> {
    const rawClient = apiClient.getRawClient();

    const response = await rawClient.get(ENDPOINTS.SECURITY_SCANS.CONSENT_PDF(id), {
      responseType: 'blob',
    });

    const blob = new Blob([response.data], { type: 'application/pdf' });
    const url = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = fileName || `DevPilot-Scan-Consent-${id.slice(0, 8)}.pdf`;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    window.URL.revokeObjectURL(url);
  },
};
