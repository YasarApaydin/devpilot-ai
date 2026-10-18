export type ScanMode = 'quick' | 'standard' | 'deep';

export type ScanStatus = 'Pending' | 'Running' | 'Completed' | 'Failed' | 'Cancelled';

export interface ScanCategory {
  id: string;
  title: string;
  description: string;
  cwes: string[];
  enabledByDefault: boolean;
}

export interface ValidateTargetRequest {
  targetUrl: string;
}

export interface ValidateTargetResponse {
  isValid: boolean;
  normalizedUrl?: string;
  host?: string;
  errorMessage?: string;
}

export interface CreateSecurityScanRequest {
  targetUrl: string;
  conversationId?: string;
  scanMode: ScanMode;
  selectedCategories: string[];
  customInstructions?: string;
  disclaimerVersion?: string;
  hasOwnershipAuthorization: boolean;
  hasRiskAcknowledged: boolean;
}

export interface SecurityScanFinding {
  id: string;
  securityScanId: string;
  title: string;
  severity: 'critical' | 'high' | 'medium' | 'low' | 'info';
  cwe?: string;
  cvssScore?: number;
  cvssVector?: string;
  targetUrl: string;
  endpoint?: string;
  description: string;
  impact?: string;
  technicalAnalysis?: string;
  remediation?: string;
  evidence?: string;
  pocDescription?: string;
  confidence?: string;
}

export interface SecurityScanConsent {
  id: string;
  securityScanId: string;
  targetUrl: string;
  disclaimerVersion: string;
  hasOwnershipAuthorization: boolean;
  hasRiskAcknowledged: boolean;
  consentedAt: string;
  clientIp?: string;
}

export interface SecurityScan {
  id: string;
  userId: string;
  conversationId?: string;
  targetUrl: string;
  normalizedTargetUrl: string;
  scanMode: ScanMode;
  selectedCategories: string[];
  customInstructions?: string;
  status: ScanStatus;
  createdAt: string;
  startedAt?: string;
  completedAt?: string;
  errorMessage?: string;
  summary?: string;
  totalFindingsCount: number;
  criticalCount: number;
  highCount: number;
  mediumCount: number;
  lowCount: number;
  infoCount: number;
  strixRunId?: string;
  consent?: SecurityScanConsent;
  findings: SecurityScanFinding[];
}

export interface SecurityScanSummary {
  id: string;
  targetUrl: string;
  scanMode: ScanMode;
  status: ScanStatus;
  createdAt: string;
  completedAt?: string;
  totalFindingsCount: number;
  criticalCount: number;
  highCount: number;
  mediumCount: number;
  lowCount: number;
  infoCount: number;
}
