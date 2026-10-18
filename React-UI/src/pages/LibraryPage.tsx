import React from 'react';
import { useNavigate } from 'react-router-dom';
import { useLibrary } from '../hooks/useLibrary';
import { LibraryPanel } from '../components/library/LibraryPanel';

export const LibraryPage: React.FC = () => {
  const navigate = useNavigate();
  const {
    documents,
    searchTerm,
    setSearchTerm,
    isLoading,
    isUploading,
    uploadProgress,
    uploadError,
    uploadDocument,
    deleteDocument,
  } = useLibrary();

  const handleAnalyze = (id: string, fileName: string) => {
    navigate('/app', { state: { analyzeDoc: { id, fileName } } });
  };

  return (
    <div className="flex-1 h-full p-4 sm:p-6 bg-[#f8fafc] overflow-hidden flex flex-col">
      <div className="max-w-4xl w-full mx-auto h-full flex flex-col">
        <LibraryPanel
          documents={documents}
          searchTerm={searchTerm}
          onSearchChange={setSearchTerm}
          isLoading={isLoading}
          isUploading={isUploading}
          uploadProgress={uploadProgress}
          uploadError={uploadError}
          onUpload={uploadDocument}
          onDelete={deleteDocument}
          onAnalyze={handleAnalyze}
          className="border border-slate-200 rounded-2xl bg-white shadow-sm"
        />
      </div>
    </div>
  );
};
