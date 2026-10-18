import React, { useState, useEffect } from 'react';
import { Modal } from '../ui/Modal';
import { Button } from '../ui/Button';
import { Badge } from '../ui/Badge';
import { Spinner } from '../ui/Spinner';
import { libraryApi } from '../../api/libraryApi';
import { formatFileSize } from '../../utils/formatFileSize';
import { formatDate } from '../../utils/formatDate';
import { FileText, Copy, Check, Sparkles, BookOpen, Layers } from 'lucide-react';
import type { DocumentDetail } from '../../types';

interface DocumentViewerModalProps {
  documentId: string | null;
  isOpen: boolean;
  onClose: () => void;
  onAnalyze?: (id: string, fileName: string) => void;
}

export const DocumentViewerModal: React.FC<DocumentViewerModalProps> = ({
  documentId,
  isOpen,
  onClose,
  onAnalyze,
}) => {
  const [detail, setDetail] = useState<DocumentDetail | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [activeTab, setActiveTab] = useState<'content' | 'chunks'>('content');
  const [copied, setCopied] = useState(false);

  useEffect(() => {
    if (isOpen && documentId) {
      setIsLoading(true);
      setDetail(null);
      libraryApi
        .getDocumentDetail(documentId)
        .then((res) => {
          if (res.data) {
            setDetail(res.data);
          }
        })
        .finally(() => {
          setIsLoading(false);
        });
    }
  }, [isOpen, documentId]);

  const handleCopy = () => {
    if (detail?.fullContent) {
      navigator.clipboard.writeText(detail.fullContent);
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    }
  };

  if (!isOpen) return null;

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title={detail?.fileName || 'Doküman İçeriği'}
      description="Yüklenen dokümanın içeriği, parçaları ve yapay zeka özeti"
      maxWidth="lg"
    >
      <div className="space-y-4">
        {isLoading ? (
          <div className="py-16 flex flex-col items-center justify-center gap-2 text-slate-400 text-xs">
            <Spinner size="md" />
            <span>Doküman içeriği yükleniyor...</span>
          </div>
        ) : detail ? (
          <>
            {/* Meta info bar */}
            <div className="flex flex-wrap items-center justify-between gap-2 p-3 rounded-xl bg-slate-50 border border-slate-200 text-xs">
              <div className="flex items-center gap-2 flex-wrap">
                <Badge status={detail.status} />
                <span className="text-slate-500 font-mono">{formatFileSize(detail.size)}</span>
                <span className="text-slate-400">&bull;</span>
                <span className="text-slate-500">{formatDate(detail.createdAt)}</span>
                {detail.totalTokens > 0 && (
                  <>
                    <span className="text-slate-400">&bull;</span>
                    <span className="text-slate-500">{detail.totalTokens} token</span>
                  </>
                )}
              </div>

              <div className="flex items-center gap-1.5">
                <button
                  type="button"
                  onClick={handleCopy}
                  className="inline-flex items-center gap-1 px-2.5 py-1 rounded-md text-xs font-medium text-slate-600 hover:text-slate-900 hover:bg-slate-200/60 border border-slate-200 bg-white transition-colors"
                  title="Metni kopyala"
                >
                  {copied ? (
                    <>
                      <Check className="w-3.5 h-3.5 text-emerald-600" />
                      <span className="text-emerald-700 font-semibold">Kopyalandı</span>
                    </>
                  ) : (
                    <>
                      <Copy className="w-3.5 h-3.5" />
                      <span>Kopyala</span>
                    </>
                  )}
                </button>

                {onAnalyze && (
                  <Button
                    variant="primary"
                    size="sm"
                    leftIcon={<Sparkles className="w-3.5 h-3.5" />}
                    onClick={() => {
                      onClose();
                      onAnalyze(detail.id, detail.fileName);
                    }}
                  >
                    AI ile İncele
                  </Button>
                )}
              </div>
            </div>

            {/* AI Summary card (if exists) */}
            {detail.summary && (
              <div className="p-3.5 rounded-xl bg-purple-50/70 border border-purple-200/80 text-xs space-y-1">
                <div className="flex items-center gap-1.5 text-purple-700 font-semibold">
                  <BookOpen className="w-3.5 h-3.5 text-purple-600" />
                  <span>Yapay Zeka Doküman Özeti</span>
                </div>
                <p className="text-slate-700 leading-relaxed">{detail.summary}</p>
              </div>
            )}

            {/* View Tabs */}
            <div className="flex items-center gap-2 border-b border-slate-200 pb-1">
              <button
                type="button"
                onClick={() => setActiveTab('content')}
                className={`flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-xs font-medium transition-colors ${
                  activeTab === 'content'
                    ? 'bg-blue-50 text-blue-700 border border-blue-200'
                    : 'text-slate-600 hover:text-slate-900'
                }`}
              >
                <FileText className="w-3.5 h-3.5" />
                <span>Tam Metin</span>
              </button>

              <button
                type="button"
                onClick={() => setActiveTab('chunks')}
                className={`flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-xs font-medium transition-colors ${
                  activeTab === 'chunks'
                    ? 'bg-blue-50 text-blue-700 border border-blue-200'
                    : 'text-slate-600 hover:text-slate-900'
                }`}
              >
                <Layers className="w-3.5 h-3.5" />
                <span>RAG İndeks Parçaları ({detail.chunks.length})</span>
              </button>
            </div>

            {/* Content area */}
            <div className="max-h-80 overflow-y-auto rounded-xl border border-slate-200 bg-white p-4 text-xs text-slate-800 leading-relaxed font-sans shadow-inner">
              {activeTab === 'content' ? (
                <div className="whitespace-pre-wrap break-words space-y-2">
                  {detail.fullContent || 'Bu dokümandan çıkarılan metin içeriği bulunamadı.'}
                </div>
              ) : (
                <div className="space-y-3">
                  {detail.chunks.map((chunk, idx) => (
                    <div
                      key={chunk.id || idx}
                      className="p-3 rounded-lg bg-slate-50 border border-slate-200 space-y-1.5"
                    >
                      <div className="flex items-center justify-between text-[11px] font-semibold text-slate-500">
                        <span>Parça #{chunk.chunkIndex + 1}</span>
                        {chunk.tokenCount && <span>{chunk.tokenCount} kelime/token</span>}
                      </div>
                      <p className="whitespace-pre-wrap text-slate-700">{chunk.content}</p>
                    </div>
                  ))}
                </div>
              )}
            </div>

            <div className="flex items-center justify-end pt-2 border-t border-slate-100">
              <Button variant="ghost" size="sm" onClick={onClose}>
                Kapat
              </Button>
            </div>
          </>
        ) : (
          <div className="py-8 text-center text-xs text-slate-500">
            Doküman detayları yüklenemedi.
          </div>
        )}
      </div>
    </Modal>
  );
};
