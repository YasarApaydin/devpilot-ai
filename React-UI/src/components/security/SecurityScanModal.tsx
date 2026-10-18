import React, { useState, useEffect } from 'react';
import {
  Shield,
  ShieldAlert,
  AlertTriangle,
  CheckCircle2,
  X,
  Loader2,
  Info,
  Check,
  ChevronRight,
  ChevronDown,
} from 'lucide-react';
import { securityScanApi } from '../../api/securityScanApi';
import { useToast } from '../../context/ToastContext';
import { useCredit } from '../../context/CreditContext';
import type { ScanCategory, ScanMode, SecurityScan } from '../../types';

interface SecurityScanModalProps {
  isOpen: boolean;
  initialUrl?: string;
  conversationId?: string;
  onClose: () => void;
  onScanStarted: (scan: SecurityScan) => void;
}

export const SecurityScanModal: React.FC<SecurityScanModalProps> = ({
  isOpen,
  initialUrl = '',
  conversationId,
  onClose,
  onScanStarted,
}) => {
  const { showToast } = useToast();
  const { hasCredits, consumeCredit } = useCredit();

  const [step, setStep] = useState<'config' | 'confirm'>('config');
  const [targetUrl, setTargetUrl] = useState(initialUrl);
  const [isValidatingUrl, setIsValidatingUrl] = useState(false);
  const [urlValidation, setUrlValidation] = useState<{
    isValid: boolean;
    normalizedUrl?: string;
    host?: string;
    errorMessage?: string;
  } | null>(null);

  const [categories, setCategories] = useState<ScanCategory[]>([]);
  const [selectedCategories, setSelectedCategories] = useState<string[]>([]);
  const [scanMode, setScanMode] = useState<ScanMode>('standard');
  const [customInstructions, setCustomInstructions] = useState('');

  // Legal Consent Checkboxes & Accordion Details
  const [hasOwnershipAuth, setHasOwnershipAuth] = useState(false);
  const [hasRiskAcknowledged, setHasRiskAcknowledged] = useState(false);
  const [isAuthDetailOpen, setIsAuthDetailOpen] = useState(false);
  const [isRiskDetailOpen, setIsRiskDetailOpen] = useState(false);

  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleValidateUrl = async (urlToTest: string) => {
    if (!urlToTest.trim()) {
      setUrlValidation(null);
      return;
    }
    setIsValidatingUrl(true);
    try {
      const res = await securityScanApi.validateTarget(urlToTest);
      setUrlValidation(res);
    } catch (err: any) {
      setUrlValidation({
        isValid: false,
        errorMessage: err?.response?.data?.message || 'URL doğrulanamadı.',
      });
    } finally {
      setIsValidatingUrl(false);
    }
  };

  const handleClose = () => {
    setStep('config');
    setHasOwnershipAuth(false);
    setHasRiskAcknowledged(false);
    setIsAuthDetailOpen(false);
    setIsRiskDetailOpen(false);
    setCustomInstructions('');
    setUrlValidation(null);
    onClose();
  };

  // Reset modal state whenever opened or initialUrl changes
  useEffect(() => {
    if (isOpen) {
      setStep('config');
      setHasOwnershipAuth(false);
      setHasRiskAcknowledged(false);
      setIsAuthDetailOpen(false);
      setIsRiskDetailOpen(false);
      setCustomInstructions('');
      setScanMode('standard');

      if (categories.length > 0) {
        setSelectedCategories(categories.filter((c) => c.enabledByDefault).map((c) => c.id));
      }

      if (initialUrl && initialUrl.trim()) {
        const trimmed = initialUrl.trim();
        setTargetUrl(trimmed);
        handleValidateUrl(trimmed);
      } else {
        setTargetUrl('');
        setUrlValidation(null);
      }
    }
  }, [isOpen, initialUrl]);

  // Load supported categories
  useEffect(() => {
    if (isOpen && categories.length === 0) {
      securityScanApi
        .getCategories()
        .then((cats) => {
          setCategories(cats);
          setSelectedCategories(cats.filter((c) => c.enabledByDefault).map((c) => c.id));
        })
        .catch(() => {
          // Fallback defaults matching supported real-time categories
          const defaultCats: ScanCategory[] = [
            {
              id: 'security_headers',
              title: 'HTTP Güvenlik Başlıkları ve İzolasyon Politikaları',
              description: 'CSP (unsafe-inline/eval/wildcard), X-Frame-Options, X-Content-Type-Options, Referrer-Policy, Permissions-Policy ve COOP/CORP başlıklarının canlı analizi.',
              cwes: ['CWE-16', 'CWE-693', 'CWE-1021', 'CWE-430'],
              enabledByDefault: true,
            },
            {
              id: 'ssl_tls',
              title: 'SSL/TLS, HTTPS Zorunluluğu ve HSTS Taşıma Güvenliği',
              description: 'HTTPS şifreleme durumu, HSTS (Strict-Transport-Security) max-age süresi, alt alan adı koruması ve sayfa içi karma içerik (Mixed Content) testi.',
              cwes: ['CWE-319', 'CWE-295', 'CWE-523'],
              enabledByDefault: true,
            },
            {
              id: 'cookie_cors_security',
              title: 'Çerez (Cookie) Güvenlik Bayrakları ve CORS Politikası',
              description: 'Canlı Set-Cookie yanıtlarında Secure, HttpOnly, SameSite bayraklarının ve Access-Control-Allow-Origin/Credentials (CORS) kurallarının testi.',
              cwes: ['CWE-614', 'CWE-1004', 'CWE-1275', 'CWE-942'],
              enabledByDefault: true,
            },
            {
              id: 'info_disclosure',
              title: 'Hassas Dosya, Ortam (.env/.git) ve API Şeması İfşa Testi',
              description: '.env, .git/HEAD, .git/config, Swagger/OpenAPI şemaları, çalışma zamanı durum uç noktaları ve HTML kaynak kodu yorum/hata izi ifşası kontrolü.',
              cwes: ['CWE-200', 'CWE-538', 'CWE-552', 'CWE-615'],
              enabledByDefault: true,
            },
            {
              id: 'robots_wellknown',
              title: 'Robots.txt, Sitemap.xml ve Security.txt Keşif Analizi',
              description: 'robots.txt içindeki Disallow/Allow direktiflerinde hassas dizin ifşası, sitemap.xml erişilebilirliği ve RFC 9116 security.txt doğrulaması.',
              cwes: ['CWE-200', 'CWE-548'],
              enabledByDefault: true,
            },
            {
              id: 'form_client_security',
              title: 'İstemci Tarafı Script (SRI), HTML Form ve Yönlendirme Güvenliği',
              description: 'Harici CDN scriptlerinde Subresource Integrity (SRI) bütünlük kontrolü, güvensiz form hedefleri ve açık yönlendirme (Open Redirect) yüzeyi testi.',
              cwes: ['CWE-353', 'CWE-319', 'CWE-601'],
              enabledByDefault: true,
            },
            {
              id: 'recon_discovery',
              title: 'Sunucu, Teknoloji Yığını ve Mimari Parmak İzi (Fingerprinting)',
              description: 'Server, X-Powered-By, X-AspNet-Version başlıkları, CDN/WAF izleri, meta generator etiketleri ve canlı yanıt süresi üzerinden teknoloji tespiti.',
              cwes: ['CWE-200', 'CWE-497'],
              enabledByDefault: true,
            },
            {
              id: 'ai_config_audit',
              title: 'Yapay Zekâ Destekli Derinlemesine Konfigürasyon ve Mimari Analiz',
              description: 'Canlı HTTP yanıtlarından toplanan (hassas verileri maskelenmiş) gerçek telemetri üzerinden yapay zeka destekli derinlemesine güvenlik değerlendirmesi.',
              cwes: ['CWE-16', 'CWE-200', 'CWE-693'],
              enabledByDefault: true,
            },
          ];
          setCategories(defaultCats);
          setSelectedCategories(defaultCats.filter((c) => c.enabledByDefault).map((c) => c.id));
        });
    }
  }, [isOpen]);

  const handleToggleCategory = (id: string) => {
    setSelectedCategories((prev) =>
      prev.includes(id) ? prev.filter((item) => item !== id) : [...prev, id]
    );
  };

  const canProceedToConfirm =
    targetUrl.trim().length > 0 &&
    urlValidation?.isValid &&
    selectedCategories.length > 0 &&
    hasOwnershipAuth &&
    hasRiskAcknowledged;

  const handleStartScan = async () => {
    if (!canProceedToConfirm) return;

    if (!hasCredits) {
      showToast('Günlük mesaj ve tarama krediniz kalmamıştır. Kredi yenilenene kadar tarama başlatılamaz.', 'error');
      return;
    }

    if (!consumeCredit()) {
      showToast('Kredi düşülemedi.', 'error');
      return;
    }

    setIsSubmitting(true);
    try {
      const scan = await securityScanApi.createScan({
        targetUrl: targetUrl.trim(),
        conversationId,
        scanMode,
        selectedCategories,
        customInstructions: customInstructions.trim() || undefined,
        hasOwnershipAuthorization: hasOwnershipAuth,
        hasRiskAcknowledged: hasRiskAcknowledged,
      });

      showToast('Güvenlik taraması başarıyla başlatıldı.', 'success');
      onScanStarted(scan);
      handleClose();
    } catch (err: any) {
      const msg = err?.response?.data?.message || err?.message || 'Tarama başlatılırken bir hata oluştu.';
      showToast(msg, 'error');
    } finally {
      setIsSubmitting(false);
    }
  };

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/60 backdrop-blur-xs animate-in fade-in duration-200">
      <div className="bg-white rounded-2xl shadow-2xl border border-slate-200 w-full max-w-3xl max-h-[90vh] flex flex-col overflow-hidden">
        {/* Modal Header */}
        <div className="px-6 py-4 border-b border-slate-200 flex items-center justify-between bg-slate-50/80">
          <div className="flex items-center gap-2.5">
            <div className="w-9 h-9 rounded-xl bg-blue-600 text-white flex items-center justify-center shadow-xs">
              <Shield className="w-5 h-5" />
            </div>
            <div>
              <h3 className="text-base font-bold text-slate-900">
                DevPilot AI Dış Güvenlik ve Konfigürasyon Değerlendirmesi
              </h3>
              <p className="text-xs text-slate-500">
                Pasif dış kontroller ve yapay zeka destekli konfigürasyon analizi
              </p>
            </div>
          </div>
          <button
            type="button"
            onClick={handleClose}
            className="w-8 h-8 rounded-lg text-slate-400 hover:text-slate-600 hover:bg-slate-200/60 flex items-center justify-center transition-colors"
          >
            <X className="w-4 h-4" />
          </button>
        </div>

        {/* Modal Body */}
        <div className="p-6 overflow-y-auto space-y-6 flex-1 text-slate-700 text-xs sm:text-sm">
          {step === 'config' ? (
            <>
              {/* 1. Target URL Input */}
              <div className="space-y-2">
                <label className="block text-xs font-semibold text-slate-800">
                  Hedef Web Uygulaması / Endpoint URL <span className="text-rose-500">*</span>
                </label>
                <div className="flex gap-2">
                  <div className="relative flex-1">
                    <input
                      type="text"
                      value={targetUrl}
                      onChange={(e) => {
                        setTargetUrl(e.target.value);
                        setUrlValidation(null);
                      }}
                      onBlur={() => handleValidateUrl(targetUrl)}
                      placeholder="https://example.com"
                      className={`w-full px-3.5 py-2.5 text-xs rounded-xl border bg-white focus:outline-hidden focus:ring-2 transition-all ${
                        urlValidation?.isValid
                          ? 'border-emerald-300 focus:ring-emerald-200 focus:border-emerald-500'
                          : urlValidation?.isValid === false
                          ? 'border-rose-300 focus:ring-rose-200 focus:border-rose-500'
                          : 'border-slate-300 focus:ring-blue-100 focus:border-blue-500'
                      }`}
                    />
                    {isValidatingUrl && (
                      <Loader2 className="w-4 h-4 text-blue-500 animate-spin absolute right-3 top-3" />
                    )}
                  </div>
                  <button
                    type="button"
                    onClick={() => handleValidateUrl(targetUrl)}
                    disabled={!targetUrl.trim() || isValidatingUrl}
                    className="px-4 py-2 bg-slate-100 hover:bg-slate-200 text-slate-700 font-medium rounded-xl text-xs transition-colors disabled:opacity-50"
                  >
                    Hedefi Doğrula
                  </button>
                </div>

                {/* Validation Status Notice */}
                {urlValidation?.isValid && (
                  <div className="p-2.5 rounded-lg bg-emerald-50 border border-emerald-200 text-emerald-800 text-xs flex items-center gap-2">
                    <CheckCircle2 className="w-4 h-4 text-emerald-600 shrink-0" />
                    <span>
                      Hedef URL geçerli ve SSRF kontrollerinden geçti: <strong>{urlValidation.normalizedUrl}</strong>
                    </span>
                  </div>
                )}
                {urlValidation?.isValid === false && (
                  <div className="p-2.5 rounded-lg bg-rose-50 border border-rose-200 text-rose-800 text-xs flex items-center gap-2">
                    <AlertTriangle className="w-4 h-4 text-rose-600 shrink-0" />
                    <span>{urlValidation.errorMessage}</span>
                  </div>
                )}
              </div>

              {/* 2. Scan Depth Selection */}
              <div className="space-y-2">
                <label className="block text-xs font-semibold text-slate-800">
                  Tarama Derinliği (Scan Mode)
                </label>
                <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
                  {[
                    {
                      id: 'quick',
                      title: 'Quick (Hızlı Mod)',
                      desc: 'Yalnızca ana sayfa (GET /) HTTP güvenlik başlıkları, SSL/TLS, çerezler, CORS politikası, SRI ve sunucu parmak izi analizi',
                    },
                    {
                      id: 'standard',
                      title: 'Standard (Standart Mod)',
                      desc: 'Quick kontrolleri + kamuya açık keşif dosyaları (/robots.txt, /.well-known/security.txt, /sitemap.xml) için sınırlı salt-okunur GET istekleri ve temel AI konfigürasyon analizi',
                    },
                    {
                      id: 'deep',
                      title: 'Deep (Kapsamlı Mod)',
                      desc: 'Standard kontrolleri + yaygın hassas dosya ve şema yolları (/.env, /.git/HEAD, /.git/config, /swagger, /openapi gibi sınırlı GET kontrolleri) ve derinlemesine AI analizi',
                    },
                  ].map((mode) => (
                    <label
                      key={mode.id}
                      className={`flex flex-col p-3 rounded-xl border cursor-pointer transition-all ${
                        scanMode === mode.id
                          ? 'border-blue-500 bg-blue-50/50 shadow-xs'
                          : 'border-slate-200 hover:border-slate-300 bg-white'
                      }`}
                    >
                      <div className="flex items-center justify-between mb-1">
                        <span className="font-semibold text-xs text-slate-900">{mode.title}</span>
                        <input
                          type="radio"
                          name="scanMode"
                          value={mode.id}
                          checked={scanMode === mode.id}
                          onChange={() => setScanMode(mode.id as ScanMode)}
                          className="w-3.5 h-3.5 text-blue-600"
                        />
                      </div>
                      <span className="text-[11px] text-slate-500">{mode.desc}</span>
                    </label>
                  ))}
                </div>
              </div>

              {/* 3. Category Multi-select */}
              <div className="space-y-2">
                <div className="flex items-center justify-between">
                  <label className="block text-xs font-semibold text-slate-800">
                    Test Edilecek Güvenlik Kategorileri ({selectedCategories.length} / {categories.length} seçili)
                  </label>
                  <button
                    type="button"
                    onClick={() =>
                      setSelectedCategories(
                        selectedCategories.length === categories.length
                          ? []
                          : categories.map((c) => c.id)
                      )
                    }
                    className="text-[11px] text-blue-600 hover:text-blue-700 font-medium"
                  >
                    {selectedCategories.length === categories.length
                      ? 'Tümünü Kaldır'
                      : 'Tümünü Seç'}
                  </button>
                </div>
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-2.5 max-h-64 overflow-y-auto pr-1">
                  {categories.map((cat) => {
                    const checked = selectedCategories.includes(cat.id);
                    return (
                      <div
                        key={cat.id}
                        onClick={() => handleToggleCategory(cat.id)}
                        className={`flex items-start gap-2.5 p-2.5 rounded-xl border cursor-pointer transition-all ${
                          checked
                            ? 'border-blue-400 bg-blue-50/40'
                            : 'border-slate-200 hover:border-slate-300 bg-white'
                        }`}
                      >
                        <div
                          className={`w-4 h-4 rounded-md mt-0.5 flex items-center justify-center shrink-0 border ${
                            checked
                              ? 'bg-blue-600 border-blue-600 text-white'
                              : 'border-slate-300 bg-white'
                          }`}
                        >
                          {checked && <Check className="w-3 h-3" />}
                        </div>
                        <div className="min-w-0 flex-1">
                          <div className="flex items-center justify-between gap-1.5">
                            <p className="text-xs font-semibold text-slate-800 leading-tight">
                              {cat.title}
                            </p>
                          </div>
                          <p className="text-[11px] text-slate-500 mt-0.5 line-clamp-2">
                            {cat.description}
                          </p>
                          {cat.cwes && cat.cwes.length > 0 && (
                            <div className="flex flex-wrap gap-1 mt-1.5">
                              {cat.cwes.slice(0, 3).map((cwe) => (
                                <span
                                  key={cwe}
                                  className="px-1.5 py-0.5 rounded bg-slate-100 text-slate-600 font-mono text-[9px] border border-slate-200"
                                >
                                  {cwe}
                                </span>
                              ))}
                            </div>
                          )}
                        </div>
                      </div>
                    );
                  })}
                </div>
              </div>

              {/* 4. Optional Custom Instructions */}
              <div className="space-y-1.5">
                <label className="block text-xs font-semibold text-slate-800">
                  Ek Test Talimatları (İsteğe Bağlı)
                </label>
                <textarea
                  value={customInstructions}
                  onChange={(e) => setCustomInstructions(e.target.value)}
                  placeholder="Örnek: CSP ve HSTS başlık yapılandırmasını inceleyin, çerez güvenlik bayraklarını ve açık meta verileri değerlendirin..."
                  rows={2}
                  className="w-full px-3 py-2 text-xs rounded-xl border border-slate-300 bg-white focus:outline-hidden focus:ring-2 focus:ring-blue-100 focus:border-blue-500"
                />
              </div>

              {/* 5. Legal & Authorization Consent Disclaimer */}
              <div className="p-4 rounded-2xl border space-y-3 bg-amber-50/80 border-amber-200 text-amber-950">
                <div className="flex items-start gap-2.5">
                  <ShieldAlert className="w-5 h-5 shrink-0 mt-0.5 text-amber-600" />
                  <div className="space-y-1">
                    <h4 className="font-bold text-xs text-amber-900">
                      Yasal Bilgilendirme, Kapsam ve Veri Aktarımı Beyanı
                    </h4>
                    <p className="text-[11px] leading-relaxed text-amber-800">
                      Bu güvenlik değerlendirmesi yalnızca test etme yetkisine sahip olduğunuz web sistemleri üzerinde yürütülmelidir. Testler saldırgan olmayan, salt-okunur HTTP/HTTPS GET kontrolleri (güvenlik başlıkları, TLS/SSL, çerez/CORS, sunucu parmak izi ve seçilen moda göre robots.txt, sitemap.xml, security.txt veya sınırlı hassas dosya/şema kontrolleri) kapsamında yürütülür; asla SQL injection, form fuzzing, parola tahmini, brute-force veya exploit testi içermez.
                    </p>
                  </div>
                </div>

                <div className="space-y-2.5 pt-2 border-t border-amber-200/80">
                  {/* Checkbox 1: Yetki ve Mülkiyet Onayı */}
                  <div className="rounded-xl border bg-white/70 overflow-hidden transition-all duration-200 shadow-2xs border-amber-200/90">
                    <div className="p-3 flex items-center justify-between gap-2.5">
                      <label className="flex items-center gap-2.5 cursor-pointer flex-1 select-none">
                        <input
                          type="checkbox"
                          checked={hasOwnershipAuth}
                          onChange={(e) => setHasOwnershipAuth(e.target.checked)}
                          className="w-4 h-4 rounded text-blue-600 border-slate-300 focus:ring-blue-500"
                        />
                        <span className="text-xs font-semibold text-amber-950">
                          Test hedefi üzerinde gerekli izin ve yetkiye sahip olduğumu onaylıyorum.{' '}
                          <span className="text-rose-600">*</span>
                        </span>
                      </label>
                      <button
                        type="button"
                        onClick={() => setIsAuthDetailOpen(!isAuthDetailOpen)}
                        className="inline-flex items-center gap-1 px-2.5 py-1 rounded-lg text-[11px] font-semibold text-nowrap transition-all cursor-pointer shrink-0 border text-amber-900 bg-amber-100/80 hover:bg-amber-200 border-amber-300/60"
                        title={isAuthDetailOpen ? 'Metni Kapat' : 'Metni Oku'}
                      >
                        <span>{isAuthDetailOpen ? 'Metni Gizle' : 'Metni Oku'}</span>
                        <ChevronDown
                          className={`w-3.5 h-3.5 transition-transform duration-300 ${
                            isAuthDetailOpen ? 'rotate-180' : ''
                          }`}
                        />
                      </button>
                    </div>

                    {/* Slide-Down Expandable Panel */}
                    <div
                      className={`grid transition-all duration-300 ease-in-out ${
                        isAuthDetailOpen ? 'grid-rows-[1fr] opacity-100' : 'grid-rows-[0fr] opacity-0'
                      }`}
                    >
                      <div className="overflow-hidden">
                        <div className="p-3.5 pt-2 text-[11px] leading-relaxed border-t bg-white/80 space-y-1.5 text-amber-900/90 border-amber-100">
                          <p className="font-bold text-amber-950">
                            📜 Yasal Yetki ve Mülkiyet Taahhütnamesi:
                          </p>
                          <p>
                            Bu onay ile taranacak hedef web uygulamasının (domain, IP adresi ve endpoint’ler) mülkiyetinin şahsınıza/kurumunuza ait olduğunu veya sistem sahibi tarafından güvenlik testi yürütmek üzere açıkça yetkilendirildiğinizi beyan etmektesiniz.
                          </p>
                          <p>
                            Üçüncü şahıslara ait sistemlerin veya izinsiz hedeflerin taranması 5237 sayılı Türk Ceza Kanunu (Madde 243-244), CFAA ve uluslararası siber güvenlik mevzuatı kapsamında suç teşkil eder. Yetkisiz testlerden doğabilecek her türlü cezai ve hukuki sorumluluk münhasıran kullanıcıya aittir.
                          </p>
                        </div>
                      </div>
                    </div>
                  </div>

                  {/* Checkbox 2: Risk Beyanı ve Feragatname */}
                  <div className="rounded-xl border bg-white/70 overflow-hidden transition-all duration-200 shadow-2xs border-amber-200/90">
                    <div className="p-3 flex items-center justify-between gap-2.5">
                      <label className="flex items-center gap-2.5 cursor-pointer flex-1 select-none">
                        <input
                          type="checkbox"
                          checked={hasRiskAcknowledged}
                          onChange={(e) => setHasRiskAcknowledged(e.target.checked)}
                          className="w-4 h-4 rounded text-blue-600 border-slate-300 focus:ring-blue-500"
                        />
                        <span className="text-xs font-semibold text-amber-950">
                          Seçtiğim test modunu ve kapsamını (saldırgan olmayan salt-okunur GET kontrolleri), bulut AI veri aktarımı/maskeleme kurallarını ve bilgilendirme metnini okudum ve kabul ediyorum.{' '}
                          <span className="text-rose-600">*</span>
                        </span>
                      </label>
                      <button
                        type="button"
                        onClick={() => setIsRiskDetailOpen(!isRiskDetailOpen)}
                        className="inline-flex items-center gap-1 px-2.5 py-1 rounded-lg text-[11px] font-semibold text-nowrap transition-all cursor-pointer shrink-0 border text-amber-900 bg-amber-100/80 hover:bg-amber-200 border-amber-300/60"
                        title={isRiskDetailOpen ? 'Metni Kapat' : 'Metni Oku'}
                      >
                        <span>{isRiskDetailOpen ? 'Metni Gizle' : 'Metni Oku'}</span>
                        <ChevronDown
                          className={`w-3.5 h-3.5 transition-transform duration-300 ${
                            isRiskDetailOpen ? 'rotate-180' : ''
                          }`}
                        />
                      </button>
                    </div>

                    {/* Slide-Down Expandable Panel */}
                    <div
                      className={`grid transition-all duration-300 ease-in-out ${
                        isRiskDetailOpen ? 'grid-rows-[1fr] opacity-100' : 'grid-rows-[0fr] opacity-0'
                      }`}
                    >
                      <div className="overflow-hidden">
                        <div className="p-3.5 pt-2 text-[11px] leading-relaxed border-t bg-white/80 space-y-1.5 text-amber-900/90 border-amber-100">
                          <p className="font-bold text-amber-950">
                            ℹ️ Test Metodolojisi, Kapsam ve Bulut AI Veri Aktarımı / Maskeleme:
                          </p>
                          <p>
                            • <strong>Saldırgan Olmayan Salt-Okunur Metodoloji:</strong> Bu tarama, seçilen moda göre hedef sisteme yalnızca sınırlı sayıda standart, salt-okunur HTTP/HTTPS GET isteği gönderir (Quick: yalnızca GET /, Standard: kamuya açık keşif dosyaları, Deep: sınırlı hassas dosya ve API şema yolları). Asla SQL injection, form fuzzing, brute-force, parola denemesi veya aktif exploit simülasyonu uygulanmaz.
                          </p>
                          <p>
                            • <strong>Bulut AI Sağlayıcısına Aktarılabilecek Veriler:</strong> Standard ve Deep modlarında yapay zekâ destekli güvenlik ve mimari konfigürasyon değerlendirmesi etkin olduğunda; hedef URL, HTTP durum kodları, girdiğiniz özel odak talimatları ve <strong>hassas başlıkları (Authorization, Cookie, Set-Cookie, Proxy-Authorization, X-Api-Key vb. ile olası token/secret değerleri) otomatik olarak maskelenmiş/redakte edilmiş</strong> HTTP yanıt başlık özetleri analiz amacıyla bulut AI sağlayıcısına (ör. Groq) veya yerel LLM motoruna iletilebilir.
                          </p>
                        </div>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            </>
          ) : (
            /* Confirm Screen */
            <div className="space-y-5">
              <div className="p-4 rounded-xl border space-y-3 bg-blue-50 border-blue-200">
                <div className="flex items-center gap-2 font-bold text-sm text-blue-800">
                  <Info className="w-4 h-4 text-blue-600" />
                  <span>Tarama Başlatma Ön Onayı</span>
                </div>
                <p className="text-xs text-blue-700">
                  Lütfen tarama öncesinde belirlediğiniz parametreleri son bir kez inceleyin:
                </p>

                <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 text-xs bg-white p-3 rounded-lg border border-slate-200">
                  <div>
                    <span className="text-slate-500">Hedef Adres:</span>
                    <p className="font-semibold text-slate-800 break-all">{targetUrl}</p>
                  </div>
                  <div>
                    <span className="text-slate-500">Tarama Türü:</span>
                    <p className="font-semibold text-slate-800">
                      <span className="inline-flex items-center gap-1 text-blue-700">
                        <Shield className="w-3.5 h-3.5 text-blue-600" /> Dış Güvenlik & Konfigürasyon
                      </span>
                    </p>
                  </div>
                  <div>
                    <span className="text-slate-500">Test Modu:</span>
                    <p className="font-semibold text-slate-800 uppercase">{scanMode}</p>
                  </div>
                  <div className="sm:col-span-2">
                    <span className="text-slate-500">Seçili Kapsam ({selectedCategories.length}):</span>
                    <p className="font-semibold text-slate-800">
                      {categories
                        .filter((c) => selectedCategories.includes(c.id))
                        .map((c) => c.title)
                        .join(' • ') || selectedCategories.join(', ')}
                    </p>
                  </div>

                  {customInstructions && (
                    <div className="sm:col-span-2">
                      <span className="text-slate-500">Özel Talimatlar:</span>
                      <p className="text-slate-700 italic">{customInstructions}</p>
                    </div>
                  )}
                </div>
              </div>

              <div className="p-3 rounded-xl bg-slate-50 border border-slate-200 text-xs text-slate-600 space-y-1">
                <p className="font-semibold text-slate-700">Güvenlik, İzolasyon ve Kapsam Garantisi:</p>
                <p>• Tüm istekler yalnızca yetkilendirilmiş hedef URL ve host kapsamına sınırlı, salt-okunur GET kontrolleri olarak yürütülecektir. Dış domainlere yönlendirmeler engellenir.</p>
                <p>• SQL injection, brute-force, form fuzzing veya aktif exploit girişimleri kesinlikle uygulanmaz.</p>
                <p>• Yerel ağ (127.0.0.1, RFC 1918) ve bulut metadata adreslerine erişim SSRF koruması ile engellenmiştir.</p>
                <p>• Bulut AI (ör. Groq) değerlendirmesine gönderilen telemetride Authorization, Cookie, Set-Cookie, X-Api-Key başlıkları ve token/secret kalıpları otomatik maskelenir.</p>
                <p>• Yetki ve onay kaydınız denetim amacıyla veritabanında güvenli olarak saklanacaktır.</p>
              </div>
            </div>
          )}
        </div>

        {/* Modal Footer */}
        <div className="px-6 py-4 border-t border-slate-200 flex items-center justify-between bg-slate-50">
          {step === 'config' ? (
            <>
              <button
                type="button"
                onClick={handleClose}
                className="px-4 py-2 text-xs font-medium text-slate-600 hover:text-slate-800 hover:bg-slate-200/60 rounded-xl transition-colors"
              >
                Vazgeç
              </button>
              <button
                type="button"
                disabled={!canProceedToConfirm}
                onClick={() => setStep('confirm')}
                className="inline-flex items-center gap-1.5 px-5 py-2.5 text-xs font-semibold rounded-xl bg-blue-600 text-white hover:bg-blue-700 transition-colors disabled:opacity-50 disabled:cursor-not-allowed shadow-xs"
              >
                <span>İlerle ve Onayla</span>
                <ChevronRight className="w-4 h-4" />
              </button>
            </>
          ) : (
            <>
              <button
                type="button"
                onClick={() => setStep('config')}
                disabled={isSubmitting}
                className="px-4 py-2 text-xs font-medium text-slate-600 hover:text-slate-800 hover:bg-slate-200/60 rounded-xl transition-colors"
              >
                Geri Dön
              </button>
              <button
                type="button"
                disabled={isSubmitting}
                onClick={handleStartScan}
                className="inline-flex items-center gap-2 px-6 py-2.5 text-xs font-semibold rounded-xl bg-emerald-600 text-white hover:bg-emerald-700 transition-colors disabled:opacity-50 shadow-xs"
              >
                {isSubmitting ? (
                  <>
                    <Loader2 className="w-4 h-4 animate-spin" />
                    <span>Tarama Başlatılıyor...</span>
                  </>
                ) : (
                  <>
                    <Shield className="w-4 h-4" />
                    <span>Güvenlik Taramasını Başlat</span>
                  </>
                )}
              </button>
            </>
          )}
        </div>
      </div>
    </div>
  );
};
