import React, { useState, useRef } from 'react';
import { UploadCloud, AlertCircle, Loader2 } from 'lucide-react';
import { ENV } from '../../config/env';

interface DocumentDropzoneProps {
  onFileSelected: (file: File) => Promise<void>;
  isUploading: boolean;
  uploadProgress: number;
  uploadError?: string | null;
}

export const DocumentDropzone: React.FC<DocumentDropzoneProps> = ({
  onFileSelected,
  isUploading,
  uploadProgress,
  uploadError,
}) => {
  const [isDragOver, setIsDragOver] = useState(false);
  const [localError, setLocalError] = useState<string | null>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const maxBytes = ENV.maxUploadMb * 1024 * 1024;

  const validateAndUpload = async (file: File) => {
    setLocalError(null);

    if (file.size > maxBytes) {
      setLocalError(`File size exceeds maximum allowed size of ${ENV.maxUploadMb} MB.`);
      return;
    }

    try {
      await onFileSelected(file);
    } catch {
      // Handled by parent
    }
  };

  const handleDragOver = (e: React.DragEvent) => {
    e.preventDefault();
    setIsDragOver(true);
  };

  const handleDragLeave = (e: React.DragEvent) => {
    e.preventDefault();
    setIsDragOver(false);
  };

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault();
    setIsDragOver(false);

    if (e.dataTransfer.files && e.dataTransfer.files.length > 0) {
      validateAndUpload(e.dataTransfer.files[0]);
    }
  };

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files && e.target.files.length > 0) {
      validateAndUpload(e.target.files[0]);
    }
  };

  const errorMessage = localError || uploadError;

  return (
    <div className="space-y-4">
      <div
        onDragOver={handleDragOver}
        onDragLeave={handleDragLeave}
        onDrop={handleDrop}
        onClick={() => !isUploading && fileInputRef.current?.click()}
        className={`border-2 border-dashed rounded-2xl p-8 text-center cursor-pointer transition-all ${
          isDragOver
            ? 'border-blue-500 bg-blue-50/50'
            : 'border-slate-300 hover:border-slate-400 bg-slate-50/50'
        } ${isUploading ? 'opacity-70 pointer-events-none' : ''}`}
      >
        <input
          ref={fileInputRef}
          type="file"
          accept=".pdf,.doc,.docx,.txt,.md,.json,.csv,.cs,.js,.ts,.py,.html,.css,.xml,.yaml,.yml,.sql,.sh,.png,.jpg,.jpeg,.webp"
          onChange={handleFileChange}
          className="hidden"
          disabled={isUploading}
        />

        <div className="flex flex-col items-center justify-center space-y-3">
          <div className="w-12 h-12 rounded-xl bg-blue-50 border border-blue-100 text-blue-600 flex items-center justify-center">
            {isUploading ? (
              <Loader2 className="w-6 h-6 animate-spin text-blue-600" />
            ) : (
              <UploadCloud className="w-6 h-6" />
            )}
          </div>

          <div>
            <p className="text-sm font-semibold text-slate-800">
              {isUploading
                ? 'Belge yükleniyor ve indeksleniyor...'
                : 'Belge yüklemek için tıklayın veya sürükleyip bırakın'}
            </p>
            <p className="text-xs text-slate-500 mt-1">
              Desteklenen formatlar: PDF, Word (DOCX/DOC), Metin & Kod (.txt, .md, .py, .js, .json vb.), Görsel (.png, .jpg, .webp) (Maks {ENV.maxUploadMb} MB)
            </p>
          </div>
        </div>

        {/* Upload Progress Bar */}
        {isUploading && (
          <div className="mt-5 w-full max-w-xs mx-auto">
            <div className="flex justify-between text-xs text-slate-600 mb-1">
              <span>Uploading & indexing...</span>
              <span className="font-semibold">{uploadProgress}%</span>
            </div>
            <div className="w-full bg-slate-200 rounded-full h-1.5 overflow-hidden">
              <div
                className="bg-blue-600 h-1.5 rounded-full transition-all duration-300"
                style={{ width: `${uploadProgress}%` }}
              />
            </div>
          </div>
        )}
      </div>

      {errorMessage && (
        <div className="p-3 rounded-lg bg-rose-50 border border-rose-200 text-rose-700 text-xs flex items-center gap-2">
          <AlertCircle className="w-4 h-4 shrink-0 text-rose-600" />
          <span>{errorMessage}</span>
        </div>
      )}
    </div>
  );
};
