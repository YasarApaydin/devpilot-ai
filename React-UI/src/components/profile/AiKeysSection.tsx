import React, { useState, useEffect } from 'react';
import {
  KeyRound,
  CheckCircle2,
  AlertCircle,
  ExternalLink,
  Eye,
  EyeOff,
  Trash2,
  Loader2,
  Sparkles,
  RefreshCw,
  Cpu,
  ShieldCheck,
  Check,
  HelpCircle,
  ChevronDown,
  ChevronUp,
} from 'lucide-react';
import { aiKeyApi } from '../../api/aiKeyApi';
import { useToast } from '../../context/ToastContext';
import type { UserAiKey, AiModelInfo } from '../../types';

interface ProviderMeta {
  provider: string;
  name: string;
  badge: string;
  accentColor: string;
  bgColor: string;
  borderColor: string;
  docUrl: string;
  placeholder: string;
  description: string;
}

const PROVIDERS_META: ProviderMeta[] = [
  {
    provider: 'openai',
    name: 'OpenAI',
    badge: 'GPT-4o & o1',
    accentColor: 'text-emerald-700',
    bgColor: 'bg-emerald-50/60',
    borderColor: 'border-emerald-200',
    docUrl: 'https://platform.openai.com/api-keys',
    placeholder: 'sk-proj-... (Sadece anahtarı yapıştırın)',
    description: 'GPT-4o, GPT-4o Mini ve o1 akıl yürütme modellerine doğrudan erişim.',
  },
  {
    provider: 'gemini',
    name: 'Google Gemini & Gemma',
    badge: 'Gemini 3.8/2.5 & Gemma 4',
    accentColor: 'text-blue-700',
    bgColor: 'bg-blue-50/60',
    borderColor: 'border-blue-200',
    docUrl: 'https://aistudio.google.com/app/apikey',
    placeholder: 'AIzaSy... (Sadece anahtarı yapıştırın, ek parametre gerekmez)',
    description: 'Google Gemini (3.8, 3.5, 3.1 Pro, 2.5 Flash) ve açık ağırlıklı Google Gemma 4 / Gemma 3 modelleri.',
  },
  {
    provider: 'deepseek',
    name: 'DeepSeek',
    badge: 'DeepSeek V3, R1 & V4',
    accentColor: 'text-cyan-700',
    bgColor: 'bg-cyan-50/60',
    borderColor: 'border-cyan-200',
    docUrl: 'https://platform.deepseek.com/api_keys',
    placeholder: 'sk-... (Sadece anahtarı yapıştırın, ek parametre gerekmez)',
    description: 'DeepSeek Chat (V3), DeepSeek Reasoner (R1), DeepSeek V4.1 Flash ve V4 Pro modelleri.',
  },
  {
    provider: 'openrouter',
    name: 'OpenRouter',
    badge: 'Devstral, Gemma 4 & DeepSeek',
    accentColor: 'text-indigo-700',
    bgColor: 'bg-indigo-50/60',
    borderColor: 'border-indigo-200',
    docUrl: 'https://openrouter.ai/settings/keys',
    placeholder: 'sk-or-v1-... (Sadece anahtarı yapıştırın, ek parametre gerekmez)',
    description: 'Tek anahtarla Devstral 2, Gemma 4 (Free), DeepSeek V4/R1, Mistral Large 4 ve yüzlerce modele birleşik erişim.',
  },
  {
    provider: 'mistral',
    name: 'Mistral AI',
    badge: 'Mistral Large 4 & Codestral',
    accentColor: 'text-rose-700',
    bgColor: 'bg-rose-50/60',
    borderColor: 'border-rose-200',
    docUrl: 'https://console.mistral.ai/api-keys',
    placeholder: 'Mistral API Anahtarı (Sadece anahtarı yapıştırın, ek parametre gerekmez)',
    description: 'Mistral Large 4, Mistral Medium 3.5 (Devstral 2 resmi ardılı), Codestral ve Mistral Small modelleri.',
  },
  {
    provider: 'groq',
    name: 'GroqCloud',
    badge: 'GPT-OSS & Llama 3.3',
    accentColor: 'text-orange-700',
    bgColor: 'bg-orange-50/60',
    borderColor: 'border-orange-200',
    docUrl: 'https://console.groq.com/keys',
    placeholder: 'gsk_... (Sadece anahtarı yapıştırın)',
    description: 'GPT-OSS 120B, GPT-OSS 20B ve Llama 3.3 modelleri için ultra hızlı LPU çıkarımı.',
  },
  {
    provider: 'xai',
    name: 'xAI Grok',
    badge: 'Grok 2',
    accentColor: 'text-purple-700',
    bgColor: 'bg-purple-50/60',
    borderColor: 'border-purple-200',
    docUrl: 'https://console.x.ai/',
    placeholder: 'xai-... (Sadece anahtarı yapıştırın)',
    description: 'xAI tarafından geliştirilen Grok 2 ve Grok 2 Vision modelleri.',
  },
  {
    provider: 'anthropic',
    name: 'Anthropic Claude',
    badge: 'Claude 3.7 & 3.5',
    accentColor: 'text-amber-700',
    bgColor: 'bg-amber-50/60',
    borderColor: 'border-amber-200',
    docUrl: 'https://console.anthropic.com/settings/keys',
    placeholder: 'sk-ant-api03-... (Opsiyonel: sk-ant-...#workspace_id)',
    description: 'Claude 3.7 Sonnet ve Claude 3.5 Haiku. (Standart anahtar veya kurumsal anahtar#workspace_id destekler).',
  },
];

export const AiKeysSection: React.FC = () => {
  const { showToast } = useToast();
  const [keys, setKeys] = useState<UserAiKey[]>([]);
  const [isLoading, setIsLoading] = useState<boolean>(true);

  // Per-provider input states
  const [inputValues, setInputValues] = useState<Record<string, string>>({});
  const [showKey, setShowKey] = useState<Record<string, boolean>>({});
  const [isSaving, setIsSaving] = useState<Record<string, boolean>>({});
  const [isTesting, setIsTesting] = useState<Record<string, boolean>>({});
  const [isDeleting, setIsDeleting] = useState<Record<string, boolean>>({});
  const [selectedPreferredModel, setSelectedPreferredModel] = useState<Record<string, string>>({});
  const [showGuide, setShowGuide] = useState<boolean>(false);

  const loadKeys = async () => {
    setIsLoading(true);
    try {
      const res = await aiKeyApi.getUserAiKeys();
      if (res.data) {
        setKeys(res.data);
        const preferredMap: Record<string, string> = {};
        res.data.forEach((k) => {
          if (k.preferredModel) {
            preferredMap[k.provider] = k.preferredModel;
          }
        });
        setSelectedPreferredModel((prev) => ({ ...prev, ...preferredMap }));
      }
    } catch (err: any) {
      showToast(err?.message || 'AI anahtarları yüklenemedi.', 'error');
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    loadKeys();
  }, []);

  const handleSave = async (provider: string) => {
    const rawKey = inputValues[provider]?.trim();
    if (!rawKey) {
      showToast('Lütfen kaydedilecek API anahtarını girin.', 'error');
      return;
    }

    setIsSaving((prev) => ({ ...prev, [provider]: true }));
    try {
      const preferred = selectedPreferredModel[provider];
      const res = await aiKeyApi.saveAiKey({
        provider,
        apiKey: rawKey,
        preferredModel: preferred,
      });

      if (res.data) {
        showToast(res.message || `${provider} anahtarı kaydedildi.`, 'success');
        setInputValues((prev) => ({ ...prev, [provider]: '' }));
        await loadKeys();
      }
    } catch (err: any) {
      showToast(err?.message || 'Anahtar kaydedilemedi.', 'error');
    } finally {
      setIsSaving((prev) => ({ ...prev, [provider]: false }));
    }
  };

  const handleTest = async (provider: string) => {
    const rawKey = inputValues[provider]?.trim();
    setIsTesting((prev) => ({ ...prev, [provider]: true }));
    try {
      const res = await aiKeyApi.testAiKey({
        provider,
        apiKey: rawKey || undefined,
      });

      if (res.data?.success) {
        showToast(res.data.message || 'Bağlantı başarılı!', 'success');
        await loadKeys();
      } else {
        showToast(res.data?.message || 'Bağlantı testi başarısız oldu.', 'error');
      }
    } catch (err: any) {
      showToast(err?.message || 'Bağlantı testi başarısız oldu.', 'error');
    } finally {
      setIsTesting((prev) => ({ ...prev, [provider]: false }));
    }
  };

  const handleDelete = async (provider: string) => {
    if (!window.confirm(`${provider.toUpperCase()} API anahtarını kaldırmak istediğinize emin misiniz?`)) {
      return;
    }

    setIsDeleting((prev) => ({ ...prev, [provider]: true }));
    try {
      await aiKeyApi.deleteAiKey(provider);
      showToast('API anahtarı başarıyla kaldırıldı.', 'success');
      setInputValues((prev) => ({ ...prev, [provider]: '' }));
      await loadKeys();
    } catch (err: any) {
      showToast(err?.message || 'Anahtar kaldırılamadı.', 'error');
    } finally {
      setIsDeleting((prev) => ({ ...prev, [provider]: false }));
    }
  };

  if (isLoading) {
    return (
      <div className="bg-white border border-slate-200 rounded-2xl p-6 shadow-sm flex items-center justify-center gap-3 text-slate-500 py-12">
        <Loader2 className="w-5 h-5 animate-spin text-blue-600" />
        <span className="text-sm font-medium">AI Sağlayıcı ayarları yükleniyor...</span>
      </div>
    );
  }

  return (
    <div className="bg-white border border-slate-200 rounded-2xl p-5 sm:p-6 shadow-sm space-y-6 animate-scale-in">
      {/* Header Info */}
      <div className="border-b border-slate-100 pb-4">
        <div className="flex items-center gap-2.5">
          <div className="p-2 bg-gradient-to-tr from-blue-600 to-indigo-600 rounded-xl text-white shadow-xs">
            <Cpu className="w-5 h-5" />
          </div>
          <div>
            <h2 className="text-base font-semibold text-slate-900">
              AI Modellerim / API Anahtarlarım (BYOK)
            </h2>
            <p className="text-xs text-slate-500 mt-0.5">
              Kendi API anahtarınızı ekleyerek OpenAI, Google Gemini &amp; Gemma, DeepSeek, OpenRouter, Mistral AI, Groq, xAI ve Claude modellerini sohbet ekranında doğrudan kullanabilirsiniz.
            </p>
          </div>
        </div>
      </div>

      {/* Quick Guide & Instructions Banner */}
      <div className="bg-gradient-to-br from-slate-50 via-blue-50/25 to-indigo-50/20 border border-slate-200/90 rounded-2xl p-4 sm:p-5 shadow-2xs">
        <div
          className="flex items-center justify-between gap-3 cursor-pointer select-none"
          onClick={() => setShowGuide(!showGuide)}
        >
          <div className="flex items-center gap-3">
            <div className="p-2 bg-blue-600 text-white rounded-xl shadow-xs shrink-0">
              <HelpCircle className="w-4 h-4" />
            </div>
            <div>
              <div className="flex items-center gap-2">
                <h3 className="text-sm font-bold text-slate-900">
                  Modelleri Ekleme ve API Anahtarı Kılavuzu (BYOK)
                </h3>
                <span className="text-[10px] font-semibold uppercase px-2 py-0.5 rounded-full bg-blue-100 text-blue-700">
                  Rehber
                </span>
              </div>
              <p className="text-xs text-slate-500 mt-0.5">
                OpenAI, Gemini &amp; Gemma, DeepSeek, OpenRouter, Mistral, Claude, Grok ve Groq anahtarlarınızı nasıl alacağınızı ve hangi formatta gireceğinizi öğrenin. (Görmek için tıklayın)
              </p>
            </div>
          </div>
          <button
            type="button"
            className="text-xs font-semibold text-blue-600 hover:text-blue-800 flex items-center gap-1 px-3 py-1.5 rounded-lg hover:bg-blue-50/80 border border-transparent hover:border-blue-200 transition-all shrink-0 cursor-pointer"
          >
            {showGuide ? (
              <>
                <span>Kılavuzu Kapat</span>
                <ChevronUp className="w-3.5 h-3.5" />
              </>
            ) : (
              <>
                <span>Kılavuzu Aç &amp; İncele</span>
                <ChevronDown className="w-3.5 h-3.5" />
              </>
            )}
          </button>
        </div>

        {showGuide && (
          <div className="mt-4 pt-4 border-t border-slate-200/80 space-y-4 text-xs text-slate-600 animate-fade-in">
            {/* 3 Adımda Kolay Kurulum */}
            <div>
              <h4 className="text-xs font-bold text-slate-900 uppercase tracking-wider mb-2 flex items-center gap-1.5">
                <Sparkles className="w-3.5 h-3.5 text-blue-600" />
                3 Adımda Kolay Başlangıç
              </h4>
              <div className="grid grid-cols-1 md:grid-cols-3 gap-3">
                <div className="bg-white border border-slate-200/80 rounded-xl p-3.5 space-y-1.5 shadow-2xs">
                  <div className="flex items-center gap-2 font-bold text-slate-900">
                    <span className="w-5 h-5 rounded-full bg-blue-600 text-white flex items-center justify-center text-[11px] font-bold shrink-0">
                      1
                    </span>
                    <span>Resmi Konsoldan Alın</span>
                  </div>
                  <p className="text-slate-500 leading-relaxed text-[11.5px]">
                    Aşağıdaki listeden dilediğiniz sağlayıcının yanındaki <strong>"API Anahtarı Al"</strong> bağlantısına tıklayarak resmi geliştirici konsolundan anahtarınızı oluşturun.
                  </p>
                </div>

                <div className="bg-white border border-slate-200/80 rounded-xl p-3.5 space-y-1.5 shadow-2xs">
                  <div className="flex items-center gap-2 font-bold text-slate-900">
                    <span className="w-5 h-5 rounded-full bg-blue-600 text-white flex items-center justify-center text-[11px] font-bold shrink-0">
                      2
                    </span>
                    <span>Sadece Ham Anahtarı Yapıştırın</span>
                  </div>
                  <p className="text-slate-500 leading-relaxed text-[11.5px]">
                    Kutuya <strong>yalnızca anahtar metnini</strong> yapıştırın. Sonuna URL, model adı, tırnak işareti veya boşluk <strong>eklemeyin</strong>.
                  </p>
                </div>

                <div className="bg-white border border-slate-200/80 rounded-xl p-3.5 space-y-1.5 shadow-2xs">
                  <div className="flex items-center gap-2 font-bold text-slate-900">
                    <span className="w-5 h-5 rounded-full bg-blue-600 text-white flex items-center justify-center text-[11px] font-bold shrink-0">
                      3
                    </span>
                    <span>Kaydet &amp; Test Et</span>
                  </div>
                  <p className="text-slate-500 leading-relaxed text-[11.5px]">
                    <strong>"Kaydet"</strong> butonuna basıp ardından <strong>"Bağlantıyı Test Et"</strong> ile doğrulayın. Sohbet penceresinde dilediğiniz modeli seçerek günlük sistem kotasından düşmeden kullanın!
                  </p>
                </div>
              </div>
            </div>

            {/* Önemli Format Kuralı: #workspace_id ve Ek Bilgi Tablosu */}
            <div className="bg-white border border-indigo-200 rounded-xl p-3.5 space-y-2 shadow-2xs">
              <div className="flex items-center gap-2 font-bold text-indigo-950 text-xs">
                <KeyRound className="w-4 h-4 text-indigo-600 shrink-0" />
                <span>Önemli Kural: API Anahtarının Sonuna Ek Bilgi (#workspace_id, URL vb.) Yazılır mı?</span>
              </div>
              <p className="text-[11.5px] text-slate-600 leading-relaxed">
                <strong>Hayır — tüm sağlayıcılarda standart kullanım yalnızca ham API anahtarını tek başına yapıştırmaktır.</strong> Endpoint URL adresleri, <code className="bg-slate-100 px-1 py-0.5 rounded font-mono text-[10.5px]">HTTP-Referer</code> veya <code className="bg-slate-100 px-1 py-0.5 rounded font-mono text-[10.5px]">X-OpenRouter-Title</code> gibi başlıklar DevPilot AI sunucusu tarafından arka planda otomatik olarak eklenir:
              </p>
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-2 text-[11px]">
                <div className="p-2 rounded-lg bg-slate-50 border border-slate-200">
                  <span className="font-bold text-slate-900">DeepSeek, OpenRouter, Mistral, Gemini, OpenAI, Groq, xAI:</span>
                  <p className="text-slate-600 mt-0.5">
                    Anahtarın sonuna <strong>kesinlikle hiçbir ek bilgi (<code className="font-mono">#...</code>, URL veya model adı) yazılmaz</strong>. Yalnızca konsoldan kopyaladığınız API anahtarını doğrudan yapıştırın.
                  </p>
                </div>
                <div className="p-2 rounded-lg bg-amber-50/70 border border-amber-200">
                  <span className="font-bold text-amber-950">Anthropic Claude (<code className="font-mono">#workspace_id</code> İstisnası):</span>
                  <p className="text-amber-900 mt-0.5">
                    Standart Claude anahtarları da <strong>tek başına</strong> girilir. Yalnızca kurumsal (Organization) anahtarı kullanıyorsanız ve Anthropic konsolunuz özel çalışma alanı başlığı zorunlu kılıyorsa opsiyonel olarak <code className="font-mono bg-white px-1 rounded">sk-ant-...#wrksp_id</code> formatında girebilirsiniz.
                  </p>
                </div>
              </div>
            </div>

            {/* Ayrıntılı Sağlayıcı Rehberi */}
            <div>
              <h4 className="text-xs font-bold text-slate-900 uppercase tracking-wider mb-2.5 flex items-center gap-1.5">
                <Cpu className="w-3.5 h-3.5 text-indigo-600" />
                Sağlayıcıya Özel API Alma, Format &amp; Kota Rehberi
              </h4>
              <div className="grid grid-cols-1 lg:grid-cols-2 gap-3">
                {/* DeepSeek Guide */}
                <div className="bg-white border border-cyan-200 rounded-xl p-3.5 space-y-2 shadow-2xs">
                  <div className="flex items-center justify-between">
                    <div className="flex items-center gap-2 font-bold text-cyan-950">
                      <span className="px-1.5 py-0.5 rounded bg-cyan-100 text-cyan-800 text-[11px]">DeepSeek</span>
                      <span>DeepSeek V3, R1 &amp; V4</span>
                    </div>
                    <a
                      href="https://platform.deepseek.com/api_keys"
                      target="_blank"
                      rel="noopener noreferrer"
                      className="text-[11px] text-blue-600 hover:underline flex items-center gap-1 font-medium"
                    >
                      <span>platform.deepseek.com</span>
                      <ExternalLink className="w-3 h-3" />
                    </a>
                  </div>
                  <div className="text-[11.5px] text-slate-600 space-y-1.5">
                    <p>
                      <strong>Anahtar Biçimi:</strong> <code className="bg-slate-100 px-1 py-0.5 rounded font-mono text-[11px] text-slate-800">sk-...</code> (Ek parametre/son ek <strong>gerekmez</strong>; sadece anahtarı girin).
                    </p>
                    <p>
                      <strong>Açılan Modeller:</strong> <code className="font-mono text-[10.5px]">deepseek-chat</code> (DeepSeek V3), <code className="font-mono text-[10.5px]">deepseek-reasoner</code> (DeepSeek R1 CoT), <code className="font-mono text-[10.5px]">deepseek-flash</code> (V4.1 Flash) ve <code className="font-mono text-[10.5px]">deepseek-v4-pro</code>.
                    </p>
                    <div className="p-2 rounded-lg bg-cyan-50/70 border border-cyan-200/80 text-cyan-900 text-[11px]">
                      <span className="font-semibold text-cyan-950">Bakiye Bilgisi (HTTP 402):</span> DeepSeek API kullanmak için <em>platform.deepseek.com &gt; Top up</em> menüsünden ön ödemeli bakiye yüklemeniz gerekir. Bakiye sıfır olduğunda API <strong>402 Insufficient Balance</strong> döndürür.
                    </div>
                  </div>
                </div>

                {/* OpenRouter Guide */}
                <div className="bg-white border border-indigo-200 rounded-xl p-3.5 space-y-2 shadow-2xs">
                  <div className="flex items-center justify-between">
                    <div className="flex items-center gap-2 font-bold text-indigo-950">
                      <span className="px-1.5 py-0.5 rounded bg-indigo-100 text-indigo-800 text-[11px]">OpenRouter</span>
                      <span>Birleşik Model Ağ Geçidi</span>
                    </div>
                    <a
                      href="https://openrouter.ai/settings/keys"
                      target="_blank"
                      rel="noopener noreferrer"
                      className="text-[11px] text-blue-600 hover:underline flex items-center gap-1 font-medium"
                    >
                      <span>openrouter.ai/settings/keys</span>
                      <ExternalLink className="w-3 h-3" />
                    </a>
                  </div>
                  <div className="text-[11.5px] text-slate-600 space-y-1.5">
                    <p>
                      <strong>Anahtar Biçimi:</strong> <code className="bg-slate-100 px-1 py-0.5 rounded font-mono text-[11px] text-slate-800">sk-or-v1-...</code> (Ek parametre/son ek <strong>gerekmez</strong>; Referer ve Title başlıkları otomatik gönderilir).
                    </p>
                    <p>
                      <strong>Açılan Modeller:</strong> <code className="font-mono text-[10.5px]">mistralai/devstral-2512</code> (Devstral 2), <code className="font-mono text-[10.5px]">google/gemma-4-31b-it:free</code>, <code className="font-mono text-[10.5px]">deepseek/deepseek-v4.1-flash</code>, <code className="font-mono text-[10.5px]">deepseek/deepseek-r1</code>, <code className="font-mono text-[10.5px]">mistralai/mistral-large-4-0</code>.
                    </p>
                    <div className="p-2 rounded-lg bg-indigo-50/70 border border-indigo-200/80 text-indigo-900 text-[11px]">
                      <span className="font-semibold text-indigo-950">Ücretsiz &amp; Ücretli Modeller:</span> OpenRouter üzerinde sonu <code className="font-mono">:free</code> ile biten modelleri (örn. Gemma 4 31B Free) ücretsiz test edebilir veya kredi yükleyerek tüm sağlayıcıların modellerine tek anahtarla erişebilirsiniz.
                    </div>
                  </div>
                </div>

                {/* Mistral AI Guide */}
                <div className="bg-white border border-rose-200 rounded-xl p-3.5 space-y-2 shadow-2xs">
                  <div className="flex items-center justify-between">
                    <div className="flex items-center gap-2 font-bold text-rose-950">
                      <span className="px-1.5 py-0.5 rounded bg-rose-100 text-rose-800 text-[11px]">Mistral AI</span>
                      <span>Mistral Large 4, Medium 3.5 &amp; Codestral</span>
                    </div>
                    <a
                      href="https://console.mistral.ai/api-keys"
                      target="_blank"
                      rel="noopener noreferrer"
                      className="text-[11px] text-blue-600 hover:underline flex items-center gap-1 font-medium"
                    >
                      <span>console.mistral.ai</span>
                      <ExternalLink className="w-3 h-3" />
                    </a>
                  </div>
                  <div className="text-[11.5px] text-slate-600 space-y-1.5">
                    <p>
                      <strong>Anahtar Biçimi:</strong> 32 karakterli alfanümerik anahtar (Ön ek veya son ek <strong>yoktur</strong>; La Plateforme'dan kopyaladığınız anahtarı tek başına yapıştırın).
                    </p>
                    <p>
                      <strong>Açılan Modeller:</strong> <code className="font-mono text-[10.5px]">mistral-large-4</code>, <code className="font-mono text-[10.5px]">mistral-medium-3-5</code> (Devstral 2'nin resmi aktif API ardılı), <code className="font-mono text-[10.5px]">codestral-2508</code>, <code className="font-mono text-[10.5px]">mistral-small-2603</code>.
                    </p>
                    <div className="p-2 rounded-lg bg-rose-50/70 border border-rose-200/80 text-rose-900 text-[11px]">
                      <span className="font-semibold text-rose-950">Devstral Notu:</span> Mistral resmi API'sinde <code className="font-mono">devstral-2512</code> emekliye ayrılarak yerini <strong>Mistral Medium 3.5</strong> ve <strong>Codestral</strong> modellerine bırakmıştır. Doğrudan <code className="font-mono">mistralai/devstral-2512</code> kullanmak isterseniz <strong>OpenRouter</strong> üzerinden erişebilirsiniz.
                    </div>
                  </div>
                </div>

                {/* Google Gemini & Gemma Guide */}
                <div className="bg-white border border-blue-200 rounded-xl p-3.5 space-y-2 shadow-2xs">
                  <div className="flex items-center justify-between">
                    <div className="flex items-center gap-2 font-bold text-blue-950">
                      <span className="px-1.5 py-0.5 rounded bg-blue-100 text-blue-800 text-[11px]">Google</span>
                      <span>Google Gemini &amp; Gemma (Açık Ağırlıklı)</span>
                    </div>
                    <a
                      href="https://aistudio.google.com/app/apikey"
                      target="_blank"
                      rel="noopener noreferrer"
                      className="text-[11px] text-blue-600 hover:underline flex items-center gap-1 font-medium"
                    >
                      <span>aistudio.google.com</span>
                      <ExternalLink className="w-3 h-3" />
                    </a>
                  </div>
                  <div className="text-[11.5px] text-slate-600 space-y-1.5">
                    <p>
                      <strong>Anahtar Biçimi:</strong> <code className="bg-slate-100 px-1 py-0.5 rounded font-mono text-[11px] text-slate-800">AIzaSy...</code> (Ek parametre/son ek <strong>gerekmez</strong>; sadece anahtarı girin).
                    </p>
                    <p>
                      <strong>Gemini vs. Gemma Farkı:</strong> Aynı tek Google AI Studio anahtarı hem ticari <strong>Gemini</strong> modellerini (Gemini 3.8 Flash, 3.5 Flash, 3.1 Pro, 2.5 Flash) hem de Google'ın açık ağırlıklı (open-weight) <strong>Gemma</strong> modellerini (<code className="font-mono text-[10.5px]">gemma-4-31b-it</code>, <code className="font-mono text-[10.5px]">gemma-4-26b-a4b-it</code>, <code className="font-mono text-[10.5px]">gemma-3-27b-it</code>) ücretsiz katmanda çalıştırır.
                    </p>
                    <div className="p-2 rounded-lg bg-blue-50 border border-blue-200/80 text-blue-900 text-[11px]">
                      <span className="font-semibold text-blue-950">Ücretsiz Başlangıç:</span> Google AI Studio, kredi kartı gerektirmeden hem Gemini hem de Gemma 4 / Gemma 3 modelleri için ücretsiz kullanım kotası sunar.
                    </div>
                  </div>
                </div>

                {/* OpenAI Guide */}
                <div className="bg-white border border-emerald-200 rounded-xl p-3.5 space-y-2 shadow-2xs">
                  <div className="flex items-center justify-between">
                    <div className="flex items-center gap-2 font-bold text-emerald-950">
                      <span className="px-1.5 py-0.5 rounded bg-emerald-100 text-emerald-800 text-[11px]">OpenAI</span>
                      <span>ChatGPT &amp; GPT-4o / o1</span>
                    </div>
                    <a
                      href="https://platform.openai.com/api-keys"
                      target="_blank"
                      rel="noopener noreferrer"
                      className="text-[11px] text-blue-600 hover:underline flex items-center gap-1 font-medium"
                    >
                      <span>platform.openai.com</span>
                      <ExternalLink className="w-3 h-3" />
                    </a>
                  </div>
                  <div className="text-[11.5px] text-slate-600 space-y-1.5">
                    <p>
                      <strong>Anahtar Biçimi:</strong> <code className="bg-slate-100 px-1 py-0.5 rounded font-mono text-[11px] text-slate-800">sk-proj-...</code> (Ek parametre/son ek <strong>gerekmez</strong>).
                    </p>
                    <p>
                      <strong>Açılan Modeller:</strong> GPT-4o, GPT-4o Mini, o1, o1-mini, o3-mini, GPT-4 Turbo.
                    </p>
                    <div className="p-2 rounded-lg bg-amber-50 border border-amber-200/80 text-amber-900 text-[11px] space-y-1">
                      <span className="font-semibold flex items-center gap-1 text-amber-950">
                        <AlertCircle className="w-3 h-3 text-amber-600 shrink-0" />
                        Önemli Kota Hatası (HTTP 429) Uyarısı:
                      </span>
                      <p className="leading-relaxed">
                        OpenAI API hesabı, normal ChatGPT Plus aboneliğinizden ayrıdır. <em>platform.openai.com &gt; Settings &gt; Billing</em> sekmesinden en az 5$ ön ödemeli bakiye eklemeniz gereklidir.
                      </p>
                    </div>
                  </div>
                </div>

                {/* Anthropic Claude Guide */}
                <div className="bg-white border border-amber-200 rounded-xl p-3.5 space-y-2 shadow-2xs">
                  <div className="flex items-center justify-between">
                    <div className="flex items-center gap-2 font-bold text-amber-950">
                      <span className="px-1.5 py-0.5 rounded bg-amber-100 text-amber-800 text-[11px]">Anthropic</span>
                      <span>Claude 3.7 &amp; 3.5</span>
                    </div>
                    <a
                      href="https://console.anthropic.com/settings/keys"
                      target="_blank"
                      rel="noopener noreferrer"
                      className="text-[11px] text-blue-600 hover:underline flex items-center gap-1 font-medium"
                    >
                      <span>console.anthropic.com</span>
                      <ExternalLink className="w-3 h-3" />
                    </a>
                  </div>
                  <div className="text-[11.5px] text-slate-600 space-y-1.5">
                    <p>
                      <strong>Anahtar Biçimi:</strong> <code className="bg-slate-100 px-1 py-0.5 rounded font-mono text-[11px] text-slate-800">sk-ant-api03-...</code> (Standart kullanımda tek başına girilir; opsiyonel kurumsal workspace için: <code className="bg-slate-100 px-1 py-0.5 rounded font-mono text-[10.5px]">sk-ant-...#wrksp_...</code>).
                    </p>
                    <p>
                      <strong>Açılan Modeller:</strong> Claude 3.7 Sonnet, Claude 3.5 Sonnet, Claude 3.5 Haiku, Claude 3 Opus.
                    </p>
                    <div className="p-2 rounded-lg bg-amber-50/70 border border-amber-200/70 text-amber-900 text-[11px]">
                      <span className="font-semibold text-amber-950">Ön Ödemeli Bakiye:</span> Anthropic Console &gt; Plans &amp; Billing bölümünde bakiye bulunduğundan emin olun. Normal bireysel anahtarlarda sonuna <code className="font-mono">#workspace_id</code> eklemenize gerek yoktur.
                    </div>
                  </div>
                </div>

                {/* xAI Grok Guide */}
                <div className="bg-white border border-purple-200 rounded-xl p-3.5 space-y-2 shadow-2xs">
                  <div className="flex items-center justify-between">
                    <div className="flex items-center gap-2 font-bold text-purple-950">
                      <span className="px-1.5 py-0.5 rounded bg-purple-100 text-purple-800 text-[11px]">xAI</span>
                      <span>Grok 2 &amp; Grok Vision</span>
                    </div>
                    <a
                      href="https://console.x.ai/"
                      target="_blank"
                      rel="noopener noreferrer"
                      className="text-[11px] text-blue-600 hover:underline flex items-center gap-1 font-medium"
                    >
                      <span>console.x.ai</span>
                      <ExternalLink className="w-3 h-3" />
                    </a>
                  </div>
                  <div className="text-[11.5px] text-slate-600 space-y-1.5">
                    <p>
                      <strong>Anahtar Biçimi:</strong> <code className="bg-slate-100 px-1 py-0.5 rounded font-mono text-[11px] text-slate-800">xai-...</code> (Ek parametre/son ek <strong>gerekmez</strong>).
                    </p>
                    <p>
                      <strong>Açılan Modeller:</strong> Grok 2 (Latest), Grok 2 Vision, Grok Beta.
                    </p>
                    <div className="p-2 rounded-lg bg-purple-50/70 border border-purple-200/70 text-purple-900 text-[11px]">
                      <span className="font-semibold text-purple-950">X (Twitter) ile Giriş:</span> console.x.ai adresine X hesabınızla giriş yaparak "API Keys" sayfasından anahtarınızı oluşturabilirsiniz.
                    </div>
                  </div>
                </div>

                {/* GroqCloud Guide */}
                <div className="bg-white border border-orange-200 rounded-xl p-3.5 space-y-2 shadow-2xs">
                  <div className="flex items-center justify-between">
                    <div className="flex items-center gap-2 font-bold text-orange-950">
                      <span className="px-1.5 py-0.5 rounded bg-orange-100 text-orange-800 text-[11px]">GroqCloud</span>
                      <span>Ultra Hızlı LPU Çıkarımı (GPT-OSS &amp; Llama 3.3)</span>
                    </div>
                    <a
                      href="https://console.groq.com/keys"
                      target="_blank"
                      rel="noopener noreferrer"
                      className="text-[11px] text-blue-600 hover:underline flex items-center gap-1 font-medium"
                    >
                      <span>console.groq.com</span>
                      <ExternalLink className="w-3 h-3" />
                    </a>
                  </div>
                  <div className="text-[11.5px] text-slate-600 space-y-1.5">
                    <p>
                      <strong>Anahtar Biçimi:</strong> <code className="bg-slate-100 px-1 py-0.5 rounded font-mono text-[11px] text-slate-800">gsk_...</code> (Ek parametre/son ek <strong>gerekmez</strong>).
                    </p>
                    <p>
                      <strong>Açılan Modeller:</strong> GPT-OSS 120B, GPT-OSS 20B, Llama 3.3 70B Versatile.
                    </p>
                    <div className="p-2 rounded-lg bg-orange-50/70 border border-orange-200/70 text-orange-900 text-[11px]">
                      <span className="font-semibold text-orange-950">Işık Hızında Çıkarım:</span> Groq LPU altyapısı ücretsiz geliştirici katmanı sunar ve saniyede yüzlerce token hızında anlık yanıt üretir.
                    </div>
                  </div>
                </div>
              </div>
            </div>

            {/* Security Vault Assurance */}
            <div className="flex items-start gap-2.5 bg-emerald-50/80 border border-emerald-200 rounded-xl p-3.5 text-emerald-950 shadow-2xs">
              <ShieldCheck className="w-4 h-4 text-emerald-600 mt-0.5 shrink-0" />
              <div className="space-y-0.5">
                <span className="font-bold text-emerald-950 text-xs">Uçtan Uca Donanım Destekli Şifreleme (AES-256-GCM):</span>
                <p className="text-emerald-900/90 leading-relaxed text-[11.5px]">
                  Kaydettiğiniz tüm API anahtarları veritabanında en yüksek endüstriyel güvenlik standardı olan <strong>AES-256-GCM</strong> ile şifrelenir. Anahtarınız yalnızca sizin sohbet isteklerinizde doğrudan ilgili resmi yapay zeka servisine aktarılır; loglara yazılmaz ve asla üçüncü taraflarla paylaşılmaz.
                </p>
              </div>
            </div>
          </div>
        )}
      </div>

      {/* Provider Cards */}
      <div className="space-y-4">
        {PROVIDERS_META.map((meta) => {
          const userKey = keys.find((k) => k.provider.toLowerCase() === meta.provider.toLowerCase());
          const isConfigured = !!userKey?.isConfigured;
          const isValid = userKey?.isValid ?? true;
          const lastTested = userKey?.lastTestedAt;
          const availableModels: AiModelInfo[] = userKey?.availableModels || [];

          return (
            <div
              key={meta.provider}
              className={`rounded-xl border p-4 sm:p-5 transition-all ${isConfigured
                ? 'border-slate-300 bg-white shadow-xs'
                : 'border-slate-200/80 bg-slate-50/40 hover:bg-white'
                }`}
            >
              <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 border-b border-slate-100 pb-3.5">
                <div className="flex items-center gap-3">
                  <div
                    className={`w-9 h-9 rounded-lg flex items-center justify-center font-bold text-sm ${meta.bgColor} ${meta.accentColor} border ${meta.borderColor}`}
                  >
                    {meta.name.substring(0, 2).toUpperCase()}
                  </div>
                  <div>
                    <div className="flex items-center gap-2">
                      <h3 className="text-sm font-semibold text-slate-900">{meta.name}</h3>
                      <span className="text-[11px] font-medium px-2 py-0.5 rounded-full bg-slate-100 text-slate-600 border border-slate-200">
                        {meta.badge}
                      </span>
                    </div>
                    <p className="text-xs text-slate-500 mt-0.5">{meta.description}</p>
                  </div>
                </div>

                {/* Status Badges */}
                <div className="flex items-center gap-2 shrink-0">
                  {isConfigured ? (
                    isValid ? (
                      <span className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-medium bg-emerald-50 text-emerald-700 border border-emerald-200">
                        <CheckCircle2 className="w-3.5 h-3.5" />
                        Kayıtlı & Aktif ({userKey.keyHint})
                      </span>
                    ) : (
                      <span className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-medium bg-amber-50 text-amber-700 border border-amber-200">
                        <AlertCircle className="w-3.5 h-3.5" />
                        Test Gerekiyor
                      </span>
                    )
                  ) : (
                    <span className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-medium bg-slate-100 text-slate-500 border border-slate-200">
                      Anahtar Yok
                    </span>
                  )}

                  <a
                    href={meta.docUrl}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="inline-flex items-center gap-1 text-xs font-medium text-blue-600 hover:text-blue-700 hover:underline px-2 py-1 rounded-lg hover:bg-blue-50 transition-colors"
                  >
                    <span>API Anahtarı Al</span>
                    <ExternalLink className="w-3 h-3" />
                  </a>
                </div>
              </div>

              {/* Input Form & Actions */}
              <div className="mt-4 space-y-3">
                <div className="flex flex-col sm:flex-row items-stretch sm:items-center gap-2">
                  <div className="relative flex-1">
                    <input
                      type={showKey[meta.provider] ? 'text' : 'password'}
                      value={inputValues[meta.provider] || ''}
                      onChange={(e) =>
                        setInputValues((prev) => ({ ...prev, [meta.provider]: e.target.value }))
                      }
                      placeholder={
                        isConfigured
                          ? `Kayıtlı: ${userKey?.keyHint || '••••••••'} (Güncellemek için yeni anahtar girin)`
                          : `API anahtarınızı girin (${meta.placeholder})`
                      }
                      className="w-full text-xs sm:text-sm pl-3 pr-10 py-2 border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 bg-white"
                    />
                    <button
                      type="button"
                      onClick={() =>
                        setShowKey((prev) => ({ ...prev, [meta.provider]: !prev[meta.provider] }))
                      }
                      className="absolute right-2.5 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-600 p-1"
                      title={showKey[meta.provider] ? 'Gizle' : 'Göster'}
                    >
                      {showKey[meta.provider] ? (
                        <EyeOff className="w-3.5 h-3.5" />
                      ) : (
                        <Eye className="w-3.5 h-3.5" />
                      )}
                    </button>
                  </div>

                  <div className="flex items-center gap-2 shrink-0">
                    <button
                      type="button"
                      onClick={() => handleSave(meta.provider)}
                      disabled={isSaving[meta.provider] || !inputValues[meta.provider]?.trim()}
                      className="px-3.5 py-2 bg-blue-600 hover:bg-blue-700 disabled:opacity-40 text-white rounded-lg text-xs font-medium flex items-center gap-1.5 transition-colors cursor-pointer"
                    >
                      {isSaving[meta.provider] ? (
                        <Loader2 className="w-3.5 h-3.5 animate-spin" />
                      ) : (
                        <Check className="w-3.5 h-3.5" />
                      )}
                      <span>{isConfigured ? 'Güncelle' : 'Kaydet'}</span>
                    </button>

                    <button
                      type="button"
                      onClick={() => handleTest(meta.provider)}
                      disabled={
                        isTesting[meta.provider] ||
                        (!isConfigured && !inputValues[meta.provider]?.trim())
                      }
                      className="px-3.5 py-2 bg-white border border-slate-200 hover:bg-slate-50 text-slate-700 disabled:opacity-40 rounded-lg text-xs font-medium flex items-center gap-1.5 transition-colors cursor-pointer"
                      title="API bağlantısını test et"
                    >
                      {isTesting[meta.provider] ? (
                        <Loader2 className="w-3.5 h-3.5 animate-spin text-blue-600" />
                      ) : (
                        <RefreshCw className="w-3.5 h-3.5 text-slate-500" />
                      )}
                      <span>Bağlantıyı Test Et</span>
                    </button>

                    {isConfigured && (
                      <button
                        type="button"
                        onClick={() => handleDelete(meta.provider)}
                        disabled={isDeleting[meta.provider]}
                        className="p-2 border border-rose-200 text-rose-600 hover:bg-rose-50 rounded-lg text-xs transition-colors cursor-pointer"
                        title="API anahtarını sil"
                      >
                        {isDeleting[meta.provider] ? (
                          <Loader2 className="w-3.5 h-3.5 animate-spin text-rose-600" />
                        ) : (
                          <Trash2 className="w-3.5 h-3.5" />
                        )}
                      </button>
                    )}
                  </div>
                </div>

                {/* Available models list & last tested info */}
                {isConfigured && availableModels.length > 0 && (
                  <div className="pt-2 flex flex-wrap items-center gap-1.5 text-xs text-slate-600">
                    <span className="text-[11px] font-semibold text-slate-500 mr-1 flex items-center gap-1">
                      <Sparkles className="w-3 h-3 text-purple-600" />
                      Kullanılabilir Modeller:
                    </span>
                    {availableModels.map((m) => (
                      <span
                        key={m.id}
                        className="px-2 py-0.5 rounded-md bg-slate-100 text-slate-700 border border-slate-200 text-[11px] font-medium"
                        title={m.description}
                      >
                        {m.name}
                      </span>
                    ))}
                    {lastTested && (
                      <span className="ml-auto text-[11px] text-slate-400">
                        Son test: {new Date(lastTested).toLocaleDateString('tr-TR')} {new Date(lastTested).toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' })}
                      </span>
                    )}
                  </div>
                )}
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
};
