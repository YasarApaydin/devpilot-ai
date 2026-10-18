import React, { useState } from 'react';
import { FileText, Loader2, Printer } from 'lucide-react';
import { pdfApi } from '../../api/pdfApi';
import { useToast } from '../../context/ToastContext';
import type { ChatMessage } from '../../types';

interface PdfButtonProps {
  type: 'message' | 'conversation';
  id: string;
  label?: string;
  customTitle?: string;
  content?: string;
  userPrompt?: string;
  messages?: ChatMessage[];
  className?: string;
  modelUsed?: string;
  fallbackReason?: string;
  originalModel?: string;
}

export const PdfButton: React.FC<PdfButtonProps> = ({
  type,
  id,
  label = 'PDF İndir',
  customTitle,
  content,
  userPrompt,
  messages,
  className = '',
  modelUsed,
  fallbackReason,
  originalModel,
}) => {
  const [isDownloading, setIsDownloading] = useState(false);
  const { showToast } = useToast();

  const handleDownload = async (e: React.MouseEvent) => {
    e.stopPropagation();
    setIsDownloading(true);
    try {
      if (type === 'message') {
        await pdfApi.downloadMessagePdf(
          id,
          customTitle,
          content,
          userPrompt,
          modelUsed,
          fallbackReason,
          originalModel
        );
      } else {
        await pdfApi.downloadConversationPdf(id, customTitle, messages);
      }
      showToast('PDF başarıyla indirildi.', 'success');
    } catch {
      showToast('PDF oluşturulurken bir hata oluştu.', 'error');
    } finally {
      setIsDownloading(false);
    }
  };

  const handlePrint = (e: React.MouseEvent) => {
    e.stopPropagation();
    try {
      if (type === 'message') {
        pdfApi.printMessageReport(
          customTitle,
          content,
          userPrompt,
          modelUsed,
          fallbackReason,
          originalModel
        );
      }
    } catch {
      showToast('Yazdırma penceresi açılamadı.', 'error');
    }
  };

  return (
    <div className={`inline-flex items-center gap-1.5 ${className}`}>
      <button
        onClick={handleDownload}
        disabled={isDownloading}
        title="PDF Olarak İndir"
        className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-md text-xs font-medium text-slate-600 hover:text-slate-900 hover:bg-slate-100 border border-slate-200 bg-white transition-colors disabled:opacity-50 shadow-xs"
      >
        {isDownloading ? (
          <Loader2 className="w-3.5 h-3.5 animate-spin text-blue-600" />
        ) : (
          <FileText className="w-3.5 h-3.5 text-rose-500" />
        )}
        <span>{label}</span>
      </button>

      {type === 'message' && (
        <button
          onClick={handlePrint}
          type="button"
          title="Arial Yazı Tipiyle Yazdır / PDF Olarak Kaydet"
          className="inline-flex items-center gap-1 px-2.5 py-1 rounded-md text-xs font-medium text-slate-600 hover:text-slate-900 hover:bg-slate-100 border border-slate-200 bg-white transition-colors shadow-xs"
        >
          <Printer className="w-3.5 h-3.5 text-blue-600" />
          <span>Yazdır / Arial PDF</span>
        </button>
      )}
    </div>
  );
};
