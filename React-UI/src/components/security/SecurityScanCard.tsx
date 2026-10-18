import React, { useState, useEffect } from 'react';
import {
  ShieldCheck,
  ShieldAlert,
  AlertTriangle,
  Loader2,
  FileDown,
  FileCheck2,
  XCircle,
  ChevronDown,
  ChevronUp,
} from 'lucide-react';
import { securityScanApi } from '../../api/securityScanApi';
import { useToast } from '../../context/ToastContext';
import type { SecurityScan } from '../../types';

interface SecurityScanCardProps {
  scan: SecurityScan;
  onScanUpdated?: (updatedScan: SecurityScan) => void;
}

export const SecurityScanCard: React.FC<SecurityScanCardProps> = ({
  scan: initialScan,
  onScanUpdated,
}) => {
  const { showToast } = useToast();
  const [scan, setScan] = useState<SecurityScan>(initialScan);
  const [isCancelling, setIsCancelling] = useState(false);
  const [isDownloadingPdf, setIsDownloadingPdf] = useState(false);
  const [isDownloadingConsent, setIsDownloadingConsent] = useState(false);
  const [isExpanded, setIsExpanded] = useState(true);

  // Poll status while scan is Pending or Running
  useEffect(() => {
    setScan(initialScan);
  }, [initialScan]);

  useEffect(() => {
    if (scan.status !== 'Pending' && scan.status !== 'Running') {
      return;
    }

    const interval = setInterval(async () => {
      try {
        const updated = await securityScanApi.getScan(scan.id);
        setScan(updated);
        onScanUpdated?.(updated);
        if (updated.status !== 'Pending' && updated.status !== 'Running') {
          clearInterval(interval);
        }
      } catch {
        // Silently retry next tick
      }
    }, 3000);

    return () => clearInterval(interval);
  }, [scan.id, scan.status, onScanUpdated]);

  const handleCancel = async () => {
    setIsCancelling(true);
    try {
      await securityScanApi.cancelScan(scan.id);
      showToast('Tarama iptal edildi.', 'info');
      const updated = await securityScanApi.getScan(scan.id);
      setScan(updated);
      onScanUpdated?.(updated);
    } catch {
      showToast('Tarama iptal edilemedi.', 'error');
    } finally {
      setIsCancelling(false);
    }
  };

  const handleDownloadPdf = async () => {
    setIsDownloadingPdf(true);
    try {
      await securityScanApi.downloadReportPdf(scan.id);
      showToast('Güvenlik raporu PDF olarak indirildi.', 'success');
    } catch {
      showToast('PDF raporu indirilemedi.', 'error');
    } finally {
      setIsDownloadingPdf(false);
    }
  };

  const handleDownloadConsent = async () => {
    setIsDownloadingConsent(true);
    try {
      await securityScanApi.downloadConsentPdf(scan.id);
      showToast('Yasal onay belgesi PDF olarak indirildi.', 'success');
    } catch {
      showToast('Onay belgesi indirilemedi.', 'error');
    } finally {
      setIsDownloadingConsent(false);
    }
  };

  const getSeverityBadgeClass = (severity: string) => {
    switch (severity.toLowerCase()) {
      case 'critical':
        return 'bg-rose-100 text-rose-800 border-rose-300';
      case 'high':
        return 'bg-orange-100 text-orange-800 border-orange-300';
      case 'medium':
        return 'bg-amber-100 text-amber-800 border-amber-300';
      case 'low':
        return 'bg-blue-100 text-blue-800 border-blue-300';
      default:
        return 'bg-slate-100 text-slate-700 border-slate-300';
    }
  };

  const CATEGORY_LABELS: Record<string, string> = {
    security_headers: 'HTTP Başlıkları & İzolasyon',
    ssl_tls: 'SSL/TLS & HSTS',
    cookie_cors_security: 'Çerez & CORS Güvenliği',
    info_disclosure: 'Bilgi İfşası Kontrolü',
    robots_wellknown: 'Robots & Security.txt',
    form_client_security: 'Script SRI & İstemci Güvenliği',
    recon_discovery: 'Sunucu & Teknoloji Parmak İzi',
    ai_config_audit: 'AI Konfigürasyon Analizi',
  };

  const formattedCategories = (scan.selectedCategories || [])
    .map((id) => CATEGORY_LABELS[id] || id)
    .join(' • ');

  const isRunning = scan.status === 'Pending' || scan.status === 'Running';
  const isSuccess = scan.status === 'Completed';

  return (
    <div className="my-4 rounded-2xl border border-slate-200 bg-white shadow-xs overflow-hidden text-xs sm:text-sm">
      {/* Top Banner Header */}
      <div
        className={`px-4 py-3 flex items-center justify-between border-b ${
          isRunning
            ? 'bg-blue-50/70 border-blue-200'
            : isSuccess
            ? 'bg-slate-50 border-slate-200'
            : 'bg-rose-50/70 border-rose-200'
        }`}
      >
        <div className="flex items-center gap-2.5 min-w-0">
          <div
            className={`w-8 h-8 rounded-xl flex items-center justify-center shrink-0 shadow-xs ${
              isRunning
                ? 'bg-blue-600 text-white animate-pulse'
                : isSuccess
                ? 'bg-emerald-600 text-white'
                : 'bg-rose-600 text-white'
            }`}
          >
            {isRunning ? (
              <Loader2 className="w-4 h-4 animate-spin" />
            ) : isSuccess ? (
              <ShieldCheck className="w-4 h-4" />
            ) : (
              <ShieldAlert className="w-4 h-4" />
            )}
          </div>
          <div className="min-w-0">
            <div className="flex items-center gap-2 flex-wrap">
              <span className="font-bold text-slate-900 text-xs sm:text-sm">
                Güvenlik Taraması — {scan.targetUrl}
              </span>
              <span className="text-[10px] font-bold px-2 py-0.5 rounded-full border bg-blue-100 text-blue-700 border-blue-300">
                Pasif Dış Tarama
              </span>
              <span
                className={`text-[10px] uppercase font-bold px-2 py-0.5 rounded-full border ${
                  isRunning
                    ? 'bg-blue-100 text-blue-700 border-blue-300'
                    : isSuccess
                    ? 'bg-emerald-100 text-emerald-800 border-emerald-300'
                    : 'bg-rose-100 text-rose-800 border-rose-300'
                }`}
              >
                {scan.status === 'Running'
                  ? 'Taranıyor...'
                  : scan.status === 'Pending'
                  ? 'Hazırlanıyor...'
                  : scan.status === 'Completed'
                  ? 'Tamamlandı'
                  : scan.status === 'Cancelled'
                  ? 'İptal Edildi'
                  : 'Başarısız'}
              </span>
              <span className="text-[10px] font-mono text-slate-500 bg-slate-100 px-1.5 py-0.5 rounded">
                Mod: {scan.scanMode.toUpperCase()}
              </span>
            </div>
            <p className="text-[11px] text-slate-500 mt-0.5 truncate" title={formattedCategories}>
              ID: {scan.id} • Kapsam: {formattedCategories}
            </p>
          </div>
        </div>

        <div className="flex items-center gap-1.5">
          {isRunning && (
            <button
              type="button"
              onClick={handleCancel}
              disabled={isCancelling}
              className="px-2.5 py-1 text-xs text-rose-600 hover:text-rose-700 hover:bg-rose-50 border border-rose-200 rounded-lg transition-colors font-medium flex items-center gap-1"
            >
              {isCancelling ? (
                <Loader2 className="w-3 h-3 animate-spin" />
              ) : (
                <XCircle className="w-3 h-3" />
              )}
              <span>İptal Et</span>
            </button>
          )}

          <button
            type="button"
            onClick={() => setIsExpanded(!isExpanded)}
            className="p-1.5 text-slate-400 hover:text-slate-600 hover:bg-slate-100 rounded-lg transition-colors"
          >
            {isExpanded ? <ChevronUp className="w-4 h-4" /> : <ChevronDown className="w-4 h-4" />}
          </button>
        </div>
      </div>

      {/* Expandable Body */}
      {isExpanded && (
        <div className="p-4 space-y-4">
          {/* Running State */}
          {isRunning && (
            <div className="p-3 rounded-xl bg-blue-50/60 border border-blue-100 text-xs text-blue-900 flex items-center gap-2.5">
              <Loader2 className="w-4 h-4 text-blue-600 animate-spin shrink-0" />
              <div>
                <p className="font-semibold">
                  Pasif Dış Güvenlik ve Konfigürasyon İncelemesi Yürütülüyor
                </p>
                <p className="text-[11px] text-blue-700 mt-0.5">
                  Hedef sistem üzerinde belirlenen kategoriler doğrultusunda HTTP güvenlik başlıkları, SSL/TLS, robots.txt ve açık yapılandırma kontrolleri gerçekleştiriliyor...
                </p>
              </div>
            </div>
          )}

          {/* Failure Alert */}
          {scan.status === 'Failed' && (
            <div className="p-3 rounded-xl bg-rose-50 border border-rose-200 text-xs text-rose-800 flex items-center gap-2">
              <AlertTriangle className="w-4 h-4 text-rose-600 shrink-0" />
              <span>{scan.errorMessage || 'Tarama yürütülürken beklenmeyen bir hata oluştu.'}</span>
            </div>
          )}

          {/* Severity Counters Badge Row */}
          <div className="grid grid-cols-5 gap-2 text-center text-xs">
            <div className="p-2 rounded-xl bg-rose-50 border border-rose-200">
              <span className="block text-[10px] font-bold text-rose-600 uppercase">Kritik</span>
              <span className="text-base font-extrabold text-rose-800">{scan.criticalCount}</span>
            </div>
            <div className="p-2 rounded-xl bg-orange-50 border border-orange-200">
              <span className="block text-[10px] font-bold text-orange-600 uppercase">Yüksek</span>
              <span className="text-base font-extrabold text-orange-800">{scan.highCount}</span>
            </div>
            <div className="p-2 rounded-xl bg-amber-50 border border-amber-200">
              <span className="block text-[10px] font-bold text-amber-600 uppercase">Orta</span>
              <span className="text-base font-extrabold text-amber-800">{scan.mediumCount}</span>
            </div>
            <div className="p-2 rounded-xl bg-blue-50 border border-blue-200">
              <span className="block text-[10px] font-bold text-blue-600 uppercase">Düşük</span>
              <span className="text-base font-extrabold text-blue-800">{scan.lowCount}</span>
            </div>
            <div className="p-2 rounded-xl bg-slate-50 border border-slate-200">
              <span className="block text-[10px] font-bold text-slate-600 uppercase">Bilgi</span>
              <span className="text-base font-extrabold text-slate-800">{scan.infoCount}</span>
            </div>
          </div>

          {/* Findings List */}
          {scan.findings && scan.findings.length > 0 && (
            <div className="space-y-2.5">
              <h5 className="font-bold text-xs text-slate-800 flex items-center justify-between">
                <span>Tespit Edilen Güvenlik Bulguları ({scan.findings.length})</span>
                <span className="text-[11px] font-normal text-slate-500">
                  CVSS ve risk sırasına göre listelenmiştir
                </span>
              </h5>

              <div className="space-y-2 max-h-72 overflow-y-auto pr-1">
                {scan.findings.map((f, idx) => (
                  <div
                    key={f.id || idx}
                    className="p-3 rounded-xl border border-slate-200 bg-slate-50/50 hover:bg-white hover:border-slate-300 transition-all text-xs space-y-1.5"
                  >
                    <div className="flex items-start justify-between gap-2">
                      <div className="flex items-center gap-2 flex-wrap min-w-0">
                        <span
                          className={`px-2 py-0.5 rounded-md font-bold uppercase text-[10px] border ${getSeverityBadgeClass(
                            f.severity
                          )}`}
                        >
                          {f.severity}
                        </span>
                        {f.cwe && (
                          <span className="text-[10px] font-mono text-slate-600 bg-white border border-slate-200 px-1.5 py-0.5 rounded">
                            {f.cwe}
                          </span>
                        )}
                        {f.cvssScore !== undefined && f.cvssScore !== null && (
                          <span className="text-[10px] font-semibold text-slate-700 bg-amber-50 border border-amber-200 px-1.5 py-0.5 rounded">
                            CVSS: {f.cvssScore.toFixed(1)}
                          </span>
                        )}
                        {f.confidence && (
                          <span
                            className={`text-[10px] font-medium px-1.5 py-0.5 rounded border ${
                              f.confidence.toLowerCase() === 'high'
                                ? 'bg-emerald-50 text-emerald-700 border-emerald-200'
                                : 'bg-indigo-50 text-indigo-700 border-indigo-200'
                            }`}
                          >
                            {f.confidence.toLowerCase() === 'high'
                              ? 'Deterministik Kontrol (Yüksek Güven)'
                              : `AI Pasif Analiz (${f.confidence.toUpperCase()})`}
                          </span>
                        )}
                        <span className="font-semibold text-slate-900 truncate">{f.title}</span>
                      </div>
                      {f.endpoint && (
                        <span className="text-[10px] text-slate-500 font-mono shrink-0">
                          {f.endpoint}
                        </span>
                      )}
                    </div>

                    <p className="text-slate-600 leading-relaxed text-[11px]">{f.description}</p>

                    {f.remediation && (
                      <div className="p-2 rounded-lg bg-emerald-50/80 border border-emerald-100 text-[11px] text-emerald-900">
                        <span className="font-semibold">Önerilen Çözüm:</span> {f.remediation}
                      </div>
                    )}
                  </div>
                ))}
              </div>
            </div>
          )}

          {/* Action Buttons: PDF Report & Consent Record */}
          <div className="pt-2 border-t border-slate-100 flex items-center justify-between flex-wrap gap-2">
            <span className="text-[11px] text-slate-500">
              {scan.completedAt
                ? `Tamamlandı: ${new Date(scan.completedAt).toLocaleString('tr-TR')}`
                : `Başlatıldı: ${new Date(scan.createdAt).toLocaleString('tr-TR')}`}
            </span>

            <div className="flex items-center gap-2">
              <button
                type="button"
                onClick={handleDownloadConsent}
                disabled={isDownloadingConsent}
                className="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-xl border border-slate-200 bg-white hover:bg-slate-50 text-slate-700 font-medium text-xs transition-colors shadow-xs disabled:opacity-50"
                title="Yasal yetki ve onay kaydı belgesini indir"
              >
                {isDownloadingConsent ? (
                  <Loader2 className="w-3.5 h-3.5 animate-spin text-blue-600" />
                ) : (
                  <FileCheck2 className="w-3.5 h-3.5 text-blue-600" />
                )}
                <span>Onay Belgesi İndir</span>
              </button>

              <button
                type="button"
                onClick={handleDownloadPdf}
                disabled={isDownloadingPdf}
                className="inline-flex items-center gap-1.5 px-3.5 py-1.5 rounded-xl bg-blue-600 hover:bg-blue-700 text-white font-medium text-xs transition-colors shadow-xs disabled:opacity-50"
                title="Ayrıntılı güvenlik değerlendirme raporunu PDF olarak indir"
              >
                {isDownloadingPdf ? (
                  <Loader2 className="w-3.5 h-3.5 animate-spin" />
                ) : (
                  <FileDown className="w-3.5 h-3.5" />
                )}
                <span>PDF Raporu İndir</span>
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
