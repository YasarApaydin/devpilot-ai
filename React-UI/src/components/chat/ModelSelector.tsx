import React, { useState, useEffect, useRef } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Sparkles,
  ChevronDown,
  Check,
  Lock,
  ExternalLink,
  Cpu,
  Layers,
  ArrowRight,
} from 'lucide-react';
import { aiKeyApi } from '../../api/aiKeyApi';
import type { UserAiKey, AiModelInfo } from '../../types';

export interface SelectedModel {
  provider: string; // 'system', 'openai', 'gemini', 'groq', 'xai', 'anthropic'
  modelId: string;
  displayName: string;
  providerName: string;
}

export const DEFAULT_SYSTEM_MODEL: SelectedModel = {
  provider: 'system',
  modelId: 'default',
  displayName: 'DevPilot Default (Qwen 3.8)',
  providerName: 'DevPilot AI',
};

interface ModelSelectorProps {
  selectedModel: SelectedModel;
  onSelectModel: (model: SelectedModel) => void;
  disabled?: boolean;
}

export const ModelSelector: React.FC<ModelSelectorProps> = ({
  selectedModel,
  onSelectModel,
  disabled = false,
}) => {
  const navigate = useNavigate();
  const [isOpen, setIsOpen] = useState(false);
  const [keys, setKeys] = useState<UserAiKey[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const dropdownRef = useRef<HTMLDivElement>(null);

  // Close when clicking outside
  useEffect(() => {
    const handleClickOutside = (e: MouseEvent) => {
      if (dropdownRef.current && !dropdownRef.current.contains(e.target as Node)) {
        setIsOpen(false);
      }
    };
    if (isOpen) {
      document.addEventListener('mousedown', handleClickOutside);
    }
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, [isOpen]);

  const loadProviderKeys = async () => {
    setIsLoading(true);
    try {
      const res = await aiKeyApi.getUserAiKeys();
      if (res.data) {
        setKeys(res.data);
      }
    } catch {
      // ignore
    } finally {
      setIsLoading(false);
    }
  };

  const handleToggle = () => {
    if (disabled) return;
    if (!isOpen) {
      loadProviderKeys();
    }
    setIsOpen(!isOpen);
  };

  const handleSelect = (model: SelectedModel) => {
    onSelectModel(model);
    setIsOpen(false);
  };

  const handleGoToKeys = () => {
    setIsOpen(false);
    navigate('/app/profile?tab=ai-keys');
  };

  // Provider brand badges
  const getProviderBadge = (provider: string) => {
    switch (provider.toLowerCase()) {
      case 'openai':
        return { text: 'OpenAI', bg: 'bg-emerald-50 text-emerald-700 border-emerald-200' };
      case 'gemini':
        return { text: 'Gemini / Gemma', bg: 'bg-blue-50 text-blue-700 border-blue-200' };
      case 'deepseek':
        return { text: 'DeepSeek', bg: 'bg-cyan-50 text-cyan-700 border-cyan-200' };
      case 'openrouter':
        return { text: 'OpenRouter', bg: 'bg-indigo-50 text-indigo-700 border-indigo-200' };
      case 'mistral':
        return { text: 'Mistral', bg: 'bg-rose-50 text-rose-700 border-rose-200' };
      case 'groq':
        return { text: 'Groq', bg: 'bg-orange-50 text-orange-700 border-orange-200' };
      case 'xai':
        return { text: 'xAI', bg: 'bg-purple-50 text-purple-700 border-purple-200' };
      case 'anthropic':
        return { text: 'Claude', bg: 'bg-amber-50 text-amber-700 border-amber-200' };
      default:
        return { text: 'DevPilot', bg: 'bg-slate-100 text-slate-700 border-slate-200' };
    }
  };

  const getModelStatusBadge = (provider: string, modelId: string, isValid: boolean) => {
    if (!isValid) {
      return { text: 'Anahtar Hatalı', bg: 'bg-rose-50 text-rose-700 border-rose-200' };
    }
    const lowerId = modelId.toLowerCase();
    if (provider === 'gemini') {
      if (lowerId.startsWith('gemma')) {
        return { text: 'Açık Ağırlıklı (Gemma)', bg: 'bg-indigo-50 text-indigo-700 border-indigo-200' };
      }
      if (lowerId.includes('3.8')) {
        return { text: 'Yeni Nesil', bg: 'bg-blue-50 text-blue-700 border-blue-200' };
      }
      if (lowerId.includes('3.5')) {
        return { text: 'Ultra Hızlı', bg: 'bg-emerald-50 text-emerald-700 border-emerald-200' };
      }
      if (lowerId.includes('pro')) {
        return { text: 'Derin Muhakeme', bg: 'bg-blue-50 text-blue-700 border-blue-200' };
      }
      return { text: 'Kullanılabilir', bg: 'bg-emerald-50 text-emerald-700 border-emerald-200' };
    }
    if (provider === 'deepseek') {
      if (lowerId.includes('reasoner') || lowerId.includes('r1')) {
        return { text: 'Derin Muhakeme (R1)', bg: 'bg-cyan-50 text-cyan-700 border-cyan-200' };
      }
      return { text: 'Kullanılabilir', bg: 'bg-emerald-50 text-emerald-700 border-emerald-200' };
    }
    if (provider === 'openrouter') {
      if (lowerId.endsWith(':free')) {
        return { text: 'Ücretsiz (:free)', bg: 'bg-emerald-50 text-emerald-700 border-emerald-200' };
      }
      if (lowerId.includes('gemma')) {
        return { text: 'Açık Ağırlıklı', bg: 'bg-indigo-50 text-indigo-700 border-indigo-200' };
      }
      if (lowerId.includes('devstral')) {
        return { text: 'Kod Ajanı', bg: 'bg-purple-50 text-purple-700 border-purple-200' };
      }
      return { text: 'Kullanılabilir', bg: 'bg-emerald-50 text-emerald-700 border-emerald-200' };
    }
    if (provider === 'mistral') {
      if (lowerId.includes('codestral') || lowerId.includes('medium-3-5')) {
        return { text: 'Kod & Ajan', bg: 'bg-rose-50 text-rose-700 border-rose-200' };
      }
      return { text: 'Kullanılabilir', bg: 'bg-emerald-50 text-emerald-700 border-emerald-200' };
    }
    if (provider === 'groq') {
      if (lowerId.includes('gpt-oss')) {
        return { text: 'Yüksek Hızlı LPU', bg: 'bg-emerald-50 text-emerald-700 border-emerald-200' };
      }
      return { text: 'Kullanılabilir', bg: 'bg-emerald-50 text-emerald-700 border-emerald-200' };
    }
    return { text: 'Kullanılabilir', bg: 'bg-emerald-50 text-emerald-700 border-emerald-200' };
  };

  const badge = getProviderBadge(selectedModel.provider);

  return (
    <div ref={dropdownRef} className="relative inline-block text-left">
      {/* Selector Trigger Button */}
      <button
        type="button"
        onClick={handleToggle}
        disabled={disabled}
        title="Yapay zeka modelini değiştirin"
        className={`px-2.5 py-1.5 rounded-lg border text-xs font-medium transition-all flex items-center gap-1.5 cursor-pointer shadow-2xs ${selectedModel.provider === 'system'
          ? 'bg-slate-50 border-slate-200 text-slate-700 hover:bg-slate-100'
          : 'bg-white border-blue-200 text-blue-700 hover:bg-blue-50/60 shadow-xs'
          } disabled:opacity-40 disabled:cursor-not-allowed`}
      >
        <Sparkles className="w-3.5 h-3.5 text-blue-600 shrink-0" />
        <span className="font-semibold truncate max-w-[140px] sm:max-w-[180px]">
          {selectedModel.displayName}
        </span>
        <span className={`text-[10px] px-1.5 py-0.2 rounded border font-medium hidden sm:inline ${badge.bg}`}>
          {badge.text}
        </span>
        <ChevronDown className={`w-3.5 h-3.5 text-slate-400 transition-transform ${isOpen ? 'rotate-180' : ''}`} />
      </button>

      {/* Dropdown Popover */}
      {isOpen && (
        <div className="absolute bottom-full left-0 mb-2 w-[calc(100vw-2.5rem)] max-w-sm sm:w-96 max-h-96 overflow-y-auto bg-white border border-slate-200 rounded-2xl shadow-2xl p-2.5 z-50 space-y-3 animate-scale-in">
          {/* Header */}
          <div className="px-2 py-1.5 border-b border-slate-100 flex items-center justify-between">
            <div>
              <div className="text-xs font-bold text-slate-900 flex items-center gap-1.5">
                <Cpu className="w-3.5 h-3.5 text-blue-600" />
                <span>Yapay Zeka Modeli Seç</span>
              </div>
              <p className="text-[11px] text-slate-400 mt-0.5">
                İstediğiniz bulut sağlayıcısı veya varsayılan model ile konuşun.
              </p>
            </div>

          </div>

          {/* 1. Default System Model (Always Available) */}
          <div className="space-y-1">
            <div className="px-2 text-[10px] font-semibold text-slate-400 uppercase tracking-wider">
              Dahili Sistem Modeli (Varsayılan)
            </div>
            <button
              type="button"
              onClick={() => handleSelect(DEFAULT_SYSTEM_MODEL)}
              className={`w-full flex items-start justify-between p-2.5 rounded-xl border text-left transition-all cursor-pointer ${selectedModel.provider === 'system'
                ? 'bg-blue-50/70 border-blue-300 ring-1 ring-blue-500/20'
                : 'bg-white border-slate-200 hover:bg-slate-50'
                }`}
            >
              <div className="space-y-0.5">
                <div className="flex items-center gap-2">
                  <span className="text-xs font-bold text-slate-900">DevPilot Default (Qwen 3.8 / Groq)</span>
                  <span className="text-[10px] font-semibold px-1.5 py-0.2 rounded bg-blue-100 text-blue-700">
                    Önerilen
                  </span>
                  <span className="text-[10px] font-semibold px-1.5 py-0.2 rounded bg-emerald-50 text-emerald-700 border border-emerald-200">
                    Aktif &amp; Kesintisiz
                  </span>
                </div>
                <p className="text-[11px] text-slate-500">
                  DevPilot AI'nin dahili, yüksek hızlı LPU çıkarımı ve Türkçe akıl yürütme motoru. Ek API anahtarı gerektirmez.
                </p>
              </div>
              {selectedModel.provider === 'system' && (
                <Check className="w-4 h-4 text-blue-600 shrink-0 mt-0.5" />
              )}
            </button>
          </div>

          {/* 2. Configured and Unconfigured BYOK Providers */}
          <div className="space-y-2">
            <div className="px-2 text-[10px] font-semibold text-slate-400 uppercase tracking-wider">
              Harici Sağlayıcılar (BYOK)
            </div>

            {keys.map((k) => {
              const isConfigured = k.isConfigured && k.isValid;
              const providerBadge = getProviderBadge(k.provider);

              return (
                <div key={k.provider} className="rounded-xl border border-slate-200/90 p-2 space-y-1.5 bg-slate-50/40">
                  {/* Provider Title Bar */}
                  <div className="flex items-center justify-between px-1">
                    <div className="flex items-center gap-1.5">
                      <span className="text-xs font-bold text-slate-800">{k.displayName}</span>
                      <span className={`text-[10px] px-1.5 py-0.2 rounded border font-medium ${providerBadge.bg}`}>
                        {providerBadge.text}
                      </span>
                    </div>

                    {isConfigured ? (
                      <span className="text-[10px] font-semibold text-emerald-700 bg-emerald-50 border border-emerald-200 px-1.5 py-0.2 rounded flex items-center gap-1">
                        <Check className="w-3 h-3 text-emerald-600" />
                        <span>Kullanıma Hazır</span>
                      </span>
                    ) : (
                      <span className="text-[10px] text-slate-500 bg-slate-100 border border-slate-200 px-1.5 py-0.2 rounded font-medium">
                        Anahtar Eksik
                      </span>
                    )}
                  </div>

                  {/* Models list if configured */}
                  {isConfigured ? (
                    <div className="grid grid-cols-1 gap-1">
                      {k.availableModels.map((m) => {
                        const isSelected =
                          selectedModel.provider === k.provider && selectedModel.modelId === m.id;
                        const statusBadge = getModelStatusBadge(k.provider, m.id, k.isValid);

                        return (
                          <button
                            key={m.id}
                            type="button"
                            onClick={() =>
                              handleSelect({
                                provider: k.provider,
                                modelId: m.id,
                                displayName: m.name,
                                providerName: k.displayName,
                              })
                            }
                            className={`w-full flex items-center justify-between px-2.5 py-1.5 rounded-lg text-left text-xs transition-colors cursor-pointer ${isSelected
                              ? 'bg-blue-600 text-white font-medium shadow-2xs'
                              : 'bg-white hover:bg-slate-100 text-slate-700 border border-slate-200/60'
                              }`}
                          >
                            <div className="flex items-center gap-1.5 truncate">
                              <span className="font-semibold">{m.name}</span>
                              <span
                                className={`text-[9px] font-semibold px-1.5 py-0.2 rounded border shrink-0 ${isSelected
                                  ? 'bg-blue-500 text-white border-blue-400'
                                  : statusBadge.bg
                                  }`}
                              >
                                {statusBadge.text}
                              </span>
                            </div>
                            {isSelected && <Check className="w-3.5 h-3.5 text-white shrink-0 ml-1.5" />}
                          </button>
                        );
                      })}
                    </div>
                  ) : (
                    <div className="p-2.5 rounded-lg bg-white border border-dashed border-slate-200 text-center text-xs text-slate-500 flex items-center justify-between">
                      <span className="text-[11px] text-slate-400">Bu sağlayıcı için API anahtarı eklenmedi.</span>

                    </div>
                  )}
                </div>
              );
            })}
          </div>
        </div>
      )}
    </div>
  );
};
