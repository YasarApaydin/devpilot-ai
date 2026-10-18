import React, { useState, useRef, useEffect } from 'react';
import { Send, Paperclip, Sparkles, X, FileText, Loader2, Shield } from 'lucide-react';
import { VoiceRecorder } from './VoiceRecorder';
import { ModelSelector, DEFAULT_SYSTEM_MODEL, type SelectedModel } from './ModelSelector';
import { libraryApi } from '../../api/libraryApi';
import { useCredit } from '../../context/CreditContext';
import { cookieConsentService } from '../../services/cookieConsentService';
import type { VoiceState } from '../../hooks/useChat';
import type { LibraryDocument } from '../../types';

export interface ChatComposerProps {
  onSendMessage: (text: string, documentId?: string, provider?: string, model?: string) => void;
  onSendVoice: (audioBlob: Blob, fileName?: string) => void;
  onOpenSecurityScan?: (targetUrl?: string) => void;
  isSending: boolean;
  isStreaming: boolean;
  voiceState: VoiceState;
  voiceError?: string | null;
  initialDocument?: { id: string; fileName: string };
}

export const ChatComposer: React.FC<ChatComposerProps> = ({
  onSendMessage,
  onSendVoice,
  onOpenSecurityScan,
  isSending,
  isStreaming,
  voiceState,
  voiceError,
  initialDocument,
}) => {
  const { creditsRemaining, hasCredits, consumeCredit, timeUntilResetFormatted, setActiveProvider } = useCredit();
  const [inputText, setInputText] = useState('');
  const [selectedDocument, setSelectedDocument] = useState<{ id: string; fileName: string } | null>(null);
  const [isDocPickerOpen, setIsDocPickerOpen] = useState(false);
  const [documents, setDocuments] = useState<LibraryDocument[]>([]);
  const [isLoadingDocs, setIsLoadingDocs] = useState(false);

  // Model Selection (BYOK & System Default) - Enforces functional cookie consent
  const [selectedModel, setSelectedModel] = useState<SelectedModel>(() => {
    try {
      if (cookieConsentService.hasConsent('functional')) {
        const saved = localStorage.getItem('devpilot_selected_model');
        if (saved) return JSON.parse(saved);
      }
    } catch {
      // fallback
    }
    return DEFAULT_SYSTEM_MODEL;
  });

  useEffect(() => {
    setActiveProvider(selectedModel.provider);
  }, [selectedModel.provider, setActiveProvider]);

  const handleModelChange = (model: SelectedModel) => {
    setSelectedModel(model);
    setActiveProvider(model.provider);
    try {
      if (cookieConsentService.hasConsent('functional')) {
        localStorage.setItem('devpilot_selected_model', JSON.stringify(model));
      } else {
        localStorage.removeItem('devpilot_selected_model');
      }
    } catch {
      // ignore
    }
  };

  const textareaRef = useRef<HTMLTextAreaElement>(null);
  const pickerRef = useRef<HTMLDivElement>(null);

  const isByok = selectedModel.provider !== 'system';
  const canSend = hasCredits || isByok;
  const isVoiceActive =
    voiceState === 'recording' ||
    voiceState === 'uploading' ||
    voiceState === 'transcribing' ||
    voiceState === 'processing';
  const isBusy = isSending || isStreaming || isVoiceActive;
  const isDisabled = isBusy || !canSend;

  // If initialDocument passed from navigation, set it and prefill prompt
  useEffect(() => {
    if (initialDocument) {
      setSelectedDocument(initialDocument);
      setInputText(
        `Lütfen kütüphanemdeki '${initialDocument.fileName}' dosyasını incele. Dokümanın konusunu, ana başlıklarını ve özetini detaylı olarak analiz et.`
      );
      if (textareaRef.current) {
        textareaRef.current.focus();
      }
    }
  }, [initialDocument]);

  // Click outside listener for doc picker popover
  useEffect(() => {
    const handleClickOutside = (e: MouseEvent) => {
      if (pickerRef.current && !pickerRef.current.contains(e.target as Node)) {
        setIsDocPickerOpen(false);
      }
    };
    if (isDocPickerOpen) {
      document.addEventListener('mousedown', handleClickOutside);
    }
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, [isDocPickerOpen]);

  // Auto-resize textarea height
  useEffect(() => {
    if (textareaRef.current) {
      textareaRef.current.style.height = 'auto';
      textareaRef.current.style.height = `${Math.min(textareaRef.current.scrollHeight, 140)}px`;
    }
  }, [inputText]);

  const handleOpenDocPicker = async () => {
    if (isDocPickerOpen) {
      setIsDocPickerOpen(false);
      return;
    }
    setIsDocPickerOpen(true);
    setIsLoadingDocs(true);
    try {
      const res = await libraryApi.getDocuments();
      if (res.data) {
        setDocuments(res.data);
      }
    } catch {
      // Ignore or empty
    } finally {
      setIsLoadingDocs(false);
    }
  };

  const handleSelectDoc = (doc: LibraryDocument) => {
    setSelectedDocument({ id: doc.id, fileName: doc.fileName });
    setIsDocPickerOpen(false);
    if (!inputText.trim()) {
      setInputText(`'${doc.fileName}' dosyasını incele ve özetle.`);
    }
    if (textareaRef.current) {
      textareaRef.current.focus();
    }
  };

  const extractSecurityScanTargetUrl = (text: string): string | null => {
    const trimmed = text.trim();
    if (!trimmed) return null;

    // Document/code file extensions blacklist for bare domain matches
    const bareFileExtensionPattern =
      /\.(pdf|docx?|txt|rtf|odt|csv|xlsx?|pptx?|json|xml|yaml|yml|css|js|ts|tsx|jsx|py|cs|java|c|cpp|h|zip|rar|7z|tar|gz|png|jpe?g|gif|webp|svg)$/i;

    // Look for genuine web URLs (https://, http://, www.)
    const explicitUrlMatch =
      trimmed.match(/https?:\/\/[^\s"'<>]+/i) ||
      trimmed.match(/www\.[a-zA-Z0-9-]+\.[a-zA-Z]{2,}[^\s"'<>]*/i);

    let candidateUrl = explicitUrlMatch ? explicitUrlMatch[0] : null;
    const isExplicitWebUrl = Boolean(explicitUrlMatch);

    if (!candidateUrl) {
      const domainMatch = trimmed.match(
        /\b([a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?\.(?:com\.tr|org\.tr|edu\.tr|gov\.tr|net\.tr|bel\.tr|k12\.tr|gen\.tr|web\.tr|com|net|org|edu|gov|io|dev|app|co|xyz|ai|info|biz|tech|site|online|me|cc|de|uk|fr|ru|nl|eu|us|ca|tr|tv|gg|sh|cloud|security|cyber|digital|store|shop|blog|news|academy))\b(?:\/[^\s"'<>]*)?/i
      );
      if (domainMatch) {
        candidateUrl = domainMatch[0];
      }
    }

    if (!candidateUrl) return null;

    candidateUrl = candidateUrl.replace(/[.,;!?)"']+$/, '');

    // Reject bare matches that are actually local filenames (e.g. rapor.pdf, config.json)
    if (!isExplicitWebUrl && bareFileExtensionPattern.test(candidateUrl)) {
      return null;
    }

    // Trigger security scan modal when user provides a URL with any analyze / test / scan / inspect / security intent, or pastes a standalone URL
    const urlTestOrAnalyzeKeywords =
      /(analiz|analyze|analysis|test|tara|tarama|scan|incele|inspect|kontrol|check|denetle|denetim|audit|değerlendir|güvenlik|security|zafiyet|vulnerability|pentest|sızma|siber|açık|exploit|rapor|header|ssl|tls|siteyi|url)/i;

    if (urlTestOrAnalyzeKeywords.test(trimmed) || trimmed === candidateUrl) {
      if (!candidateUrl.startsWith('http://') && !candidateUrl.startsWith('https://')) {
        candidateUrl = `https://${candidateUrl}`;
      }
      return candidateUrl;
    }

    return null;
  };

  const handleSubmit = (e?: React.FormEvent) => {
    if (e) e.preventDefault();
    if (isDisabled) return;

    const rawText = inputText.trim();
    if (!rawText) return;

    // 1. If a document is explicitly selected or attached, it is ALWAYS a document analysis query.
    // It must NEVER be redirected to the web security scanner!
    const isDocSelected = !!selectedDocument;

    // 2. Check if the message explicitly refers to a library document or file
    const isDocumentIntent =
      isDocSelected ||
      /(dosya|dosyayı|dosyasını|dosyasının|belge|belgeyi|belgesini|doküman|dokümanı|dokümanını|kütüphane|kütüphanemdeki|\.pdf\b|\.docx?\b|\.xlsx?\b|\.pptx?\b)/i.test(
        rawText
      );

    // 3. Open SecurityScanModal when user asks to analyze, test, scan, or inspect a target URL
    if (!isDocumentIntent) {
      const detectedUrl = extractSecurityScanTargetUrl(rawText);
      if (detectedUrl) {
        setInputText('');
        if (textareaRef.current) {
          textareaRef.current.style.height = 'auto';
        }
        onOpenSecurityScan?.(detectedUrl);
        return;
      }
    }

    if (!isByok && !hasCredits) {
      return;
    }
    onSendMessage(rawText, selectedDocument?.id, selectedModel.provider, selectedModel.modelId);
    setInputText('');
    if (textareaRef.current) {
      textareaRef.current.style.height = 'auto';
    }
  };

  const handleSendVoice = (audioBlob: Blob, fileName?: string) => {
    if (!hasCredits) return;
    if (!consumeCredit()) return;
    onSendVoice(audioBlob, fileName);
  };

  const handleKeyDown = (e: React.KeyboardEvent<HTMLTextAreaElement>) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      handleSubmit();
    }
  };

  return (
    <div className="w-full bg-white border border-slate-200 rounded-2xl p-2.5 sm:p-3.5 shadow-sm relative">
      {/* Out of Credits Alert Banner */}
      {!hasCredits && (
        isByok ? (
          <div className="flex items-center justify-between gap-2 px-3 py-2 mb-2 rounded-xl bg-blue-50 border border-blue-200 text-blue-900 text-xs">
            <div className="flex items-center gap-2">
              <span className="text-base">🔑</span>
              <span>
                <strong>BYOK Modu:</strong> Kendi API anahtarınız (<strong>{selectedModel.displayName}</strong>) kullanılıyor, sistem kredisi harcanmaz.
              </span>
            </div>
          </div>
        ) : (
          <div className="flex items-center justify-between gap-2 px-3 py-2 mb-2 rounded-xl bg-amber-50 border border-amber-200 text-amber-900 text-xs">
            <div className="flex items-center gap-2">
              <span className="text-base">⏳</span>
              <span>
                <strong>Günlük 15/15 mesaj limitinize ulaştınız.</strong> Yeni kredileriniz{' '}
                <strong>{timeUntilResetFormatted}</strong> sonra tanımlanacaktır. Sol alttaki model seçiciden kendi API anahtarınızı seçerek sınırsız sohbete devam edebilirsiniz.
              </span>
            </div>
          </div>
        )
      )}

      {/* Selected Document Attachment Pill */}
      {selectedDocument && (
        <div className="flex items-center gap-2 px-3 py-1.5 mb-2 rounded-xl bg-purple-50 border border-purple-200 text-purple-900 text-xs w-fit">
          <Sparkles className="w-3.5 h-3.5 text-purple-600 shrink-0" />
          <span className="font-semibold text-slate-800 truncate max-w-xs">{selectedDocument.fileName}</span>
          <span className="text-[11px] text-purple-600 font-medium">aktif incelemede</span>
          <button
            type="button"
            onClick={() => setSelectedDocument(null)}
            className="ml-1 text-slate-400 hover:text-rose-600 p-0.5 rounded transition-colors"
            title="Dokümanı kaldır"
          >
            <X className="w-3.5 h-3.5" />
          </button>
        </div>
      )}

      <form onSubmit={handleSubmit} className="flex flex-col gap-2">
        <textarea
          ref={textareaRef}
          rows={1}
          disabled={isDisabled}
          value={inputText}
          onChange={(e) => setInputText(e.target.value)}
          onKeyDown={handleKeyDown}
          placeholder={
            !canSend
              ? `Günlük 15/15 mesaj limitiniz doldu (${timeUntilResetFormatted} sonra yenilenecek). Kendi API anahtarınızı seçerek devam edebilirsiniz.`
              : isStreaming
              ? 'Yapay zeka yanıt oluşturuyor...'
              : isByok
              ? `${selectedModel.displayName} (Kendi Anahtarınız) ile sohbet edin...`
              : selectedDocument
              ? `'${selectedDocument.fileName}' hakkında soru sorun veya incelemesini isteyin...`
              : 'Bir soru sorun, kod yazdırın veya kütüphaneden doküman inceletin...'
          }
          className="w-full bg-transparent text-slate-800 placeholder-slate-400 text-xs sm:text-sm px-2 py-1 resize-none focus:outline-none disabled:opacity-50"
        />

        <div className="flex items-center justify-between pt-2 border-t border-slate-100">
          <div className="flex items-center gap-1 sm:gap-1.5 relative flex-wrap sm:flex-nowrap min-w-0">
            {/* 0. Model Seçici (BYOK & Sistem Default) */}
            <ModelSelector
              selectedModel={selectedModel}
              onSelectModel={handleModelChange}
              disabled={isBusy}
            />

            {/* 1. Attach Document from Library Button */}
            <div ref={pickerRef} className="relative">
              <button
                type="button"
                onClick={handleOpenDocPicker}
                disabled={isDisabled}
                title="Kütüphaneden Doküman Seç ve İncelet"
                className={`p-2 rounded-lg border transition-all flex items-center gap-1 text-xs font-medium cursor-pointer ${
                  selectedDocument
                    ? 'bg-purple-50 text-purple-700 border-purple-200'
                    : 'text-slate-600 hover:text-slate-900 hover:bg-slate-100 border-slate-200 bg-white'
                }`}
              >
                <Paperclip className="w-3.5 h-3.5 text-purple-600" />
                <span className="hidden md:inline">Doküman İncelet</span>
              </button>

              {/* Popover List of Library Documents */}
              {isDocPickerOpen && (
                <div className="absolute bottom-full left-0 mb-2 w-72 max-h-64 overflow-y-auto bg-white border border-slate-200 rounded-xl shadow-xl p-2 z-40 space-y-1">
                  <div className="px-2 py-1 text-[11px] font-semibold text-slate-500 uppercase tracking-wider border-b border-slate-100">
                    Kütüphanenizdeki Dokümanlar
                  </div>

                  {isLoadingDocs ? (
                    <div className="py-4 flex items-center justify-center gap-2 text-slate-400 text-xs">
                      <Loader2 className="w-3.5 h-3.5 animate-spin text-purple-600" />
                      <span>Dokümanlar yükleniyor...</span>
                    </div>
                  ) : documents.length === 0 ? (
                    <div className="py-3 px-2 text-center text-xs text-slate-500">
                      Kütüphanenizde henüz doküman yok. Kütüphane sayfasından dosya yükleyebilirsiniz.
                    </div>
                  ) : (
                    documents.map((doc) => (
                      <button
                        key={doc.id}
                        type="button"
                        onClick={() => handleSelectDoc(doc)}
                        className={`w-full flex items-center gap-2 px-2.5 py-1.5 rounded-lg text-left text-xs transition-colors cursor-pointer ${
                          selectedDocument?.id === doc.id
                            ? 'bg-purple-50 text-purple-800 font-semibold'
                            : 'text-slate-700 hover:bg-slate-50'
                        }`}
                      >
                        <FileText className="w-3.5 h-3.5 text-purple-600 shrink-0" />
                        <span className="truncate flex-1">{doc.fileName}</span>
                      </button>
                    ))
                  )}
                </div>
              )}
            </div>

            {/* 2. Security Scan Modal Trigger Button */}
            <button
              type="button"
              onClick={() => onOpenSecurityScan?.()}
              disabled={isDisabled}
              title="Hedef URL için Güvenlik Taraması Başlat"
              className="p-2 rounded-lg border border-slate-200 hover:bg-slate-100 text-slate-600 hover:text-blue-700 bg-white transition-all flex items-center gap-1 text-xs font-medium cursor-pointer"
            >
              <Shield className="w-3.5 h-3.5 text-blue-600" />
              <span className="hidden md:inline">Güvenlik Testi</span>
            </button>

            {/* 3. Kredi Rozeti */}
            <span className={`text-[11px] font-medium px-2 py-0.5 rounded-full border ${
              creditsRemaining > 3
                ? 'text-blue-700 bg-blue-50 border-blue-200'
                : creditsRemaining > 0
                ? 'text-amber-700 bg-amber-50 border-amber-200'
                : 'text-rose-700 bg-rose-50 border-rose-200'
            }`}>
              <span className="hidden sm:inline">Kalan Kredi: </span>
              <span className="sm:hidden">Kredi: </span>
              {creditsRemaining}/15
            </span>
          </div>

          {/* Right Group: Voice Recorder (ChatGPT Mode) + Send Button */}
          <div className="flex items-center gap-1.5 shrink-0 ml-auto">
            <VoiceRecorder
              voiceState={voiceState}
              voiceError={voiceError}
              onSendVoice={handleSendVoice}
              disabled={isSending || isStreaming || !hasCredits}
            />

            <button
              type="submit"
              disabled={!inputText.trim() || isDisabled}
              className="h-8 px-3 rounded-full bg-blue-600 hover:bg-blue-700 active:scale-95 text-white disabled:opacity-40 disabled:cursor-not-allowed transition-all flex items-center justify-center gap-1.5 text-xs font-semibold shadow-xs cursor-pointer"
            >
              <Send className="w-3.5 h-3.5" />
              <span className="hidden sm:inline">Gönder</span>
            </button>
          </div>
        </div>
      </form>
    </div>
  );
};
