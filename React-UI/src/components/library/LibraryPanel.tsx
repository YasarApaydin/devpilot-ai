import React, { useState } from 'react';
import { Search, Plus, BookOpen, FileQuestion } from 'lucide-react';
import { LibraryItem } from './LibraryItem';
import { DocumentUploadModal } from './DocumentUploadModal';
import { DocumentViewerModal } from './DocumentViewerModal';
import { EmptyState } from '../ui/EmptyState';
import { Spinner } from '../ui/Spinner';
import { Button } from '../ui/Button';
import type { LibraryDocument } from '../../types';

export interface LibraryPanelProps {
  documents: LibraryDocument[];
  searchTerm: string;
  onSearchChange: (query: string) => void;
  isLoading: boolean;
  isUploading: boolean;
  uploadProgress: number;
  uploadError?: string | null;
  onUpload: (file: File) => Promise<any>;
  onDelete: (id: string) => Promise<void>;
  onOpen?: (id: string) => void;
  onAnalyze?: (id: string, fileName: string) => void;
  className?: string;
}

export const LibraryPanel: React.FC<LibraryPanelProps> = ({
  documents,
  searchTerm,
  onSearchChange,
  isLoading,
  isUploading,
  uploadProgress,
  uploadError,
  onUpload,
  onDelete,
  onOpen,
  onAnalyze,
  className = '',
}) => {
  const [isUploadModalOpen, setIsUploadModalOpen] = useState(false);
  const [viewingDocId, setViewingDocId] = useState<string | null>(null);

  const handleOpenDoc = (id: string) => {
    setViewingDocId(id);
    onOpen?.(id);
  };

  return (
    <aside
      className={`w-full h-full flex flex-col bg-[#f1f5f9] border-l border-slate-200 p-3.5 ${className}`}
    >
      {/* Header */}
      <div className="flex items-center justify-between pb-3 border-b border-slate-200 mb-3 shrink-0">
        <div className="flex items-center gap-2">
          <BookOpen className="w-4 h-4 text-blue-600" />
          <h3 className="text-sm font-semibold text-slate-800">Kütüphanem (Library)</h3>
        </div>

        <Button
          variant="primary"
          size="sm"
          leftIcon={<Plus className="w-3.5 h-3.5" />}
          onClick={() => setIsUploadModalOpen(true)}
        >
          Doküman Yükle
        </Button>
      </div>

      {/* Search Input with Debounce */}
      <div className="relative mb-3 shrink-0">
        <Search className="w-3.5 h-3.5 text-slate-400 absolute left-3 top-1/2 -translate-y-1/2" />
        <input
          type="text"
          value={searchTerm}
          onChange={(e) => onSearchChange(e.target.value)}
          placeholder="Dokümanlarda ara..."
          className="w-full bg-white border border-slate-200 rounded-lg pl-8.5 pr-3 py-1.5 text-xs text-slate-800 placeholder-slate-400 focus:outline-none focus:border-blue-500 shadow-2xs transition-colors"
        />
      </div>

      {/* Documents List */}
      <div className="flex-1 overflow-y-auto space-y-2 pr-1">
        {isLoading ? (
          <div className="py-12 flex flex-col items-center justify-center gap-2 text-slate-400 text-xs">
            <Spinner size="sm" />
            <span>Dokümanlar yükleniyor...</span>
          </div>
        ) : documents.length === 0 ? (
          <EmptyState
            icon={<FileQuestion className="w-5 h-5 text-slate-400" />}
            title={searchTerm ? 'Eşleşen doküman bulunamadı.' : 'Henüz doküman yüklenmedi.'}
            description={
              searchTerm
                ? 'Farklı bir arama terimi deneyin.'
                : 'Yapay zekanın incelemesi için PDF veya metin dosyası yükleyin.'
            }
            action={
              !searchTerm && (
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => setIsUploadModalOpen(true)}
                >
                  İlk Dokümanı Yükle
                </Button>
              )
            }
            className="my-3 py-6"
          />
        ) : (
          documents.map((doc) => (
            <LibraryItem
              key={doc.id}
              id={doc.id}
              fileName={doc.fileName}
              size={doc.size}
              status={doc.status}
              createdAt={doc.createdAt}
              onDelete={onDelete}
              onOpen={handleOpenDoc}
              onAnalyze={onAnalyze}
            />
          ))
        )}
      </div>

      {/* Upload Modal */}
      <DocumentUploadModal
        isOpen={isUploadModalOpen}
        onClose={() => setIsUploadModalOpen(false)}
        onUpload={onUpload}
        isUploading={isUploading}
        uploadProgress={uploadProgress}
        uploadError={uploadError}
      />

      {/* Document Detail & Content Viewer Modal */}
      <DocumentViewerModal
        isOpen={!!viewingDocId}
        documentId={viewingDocId}
        onClose={() => setViewingDocId(null)}
        onAnalyze={onAnalyze}
      />
    </aside>
  );
};
