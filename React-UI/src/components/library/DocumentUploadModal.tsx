import React from 'react';
import { Modal } from '../ui/Modal';
import { DocumentDropzone } from './DocumentDropzone';

interface DocumentUploadModalProps {
  isOpen: boolean;
  onClose: () => void;
  onUpload: (file: File) => Promise<void>;
  isUploading: boolean;
  uploadProgress: number;
  uploadError?: string | null;
}

export const DocumentUploadModal: React.FC<DocumentUploadModalProps> = ({
  isOpen,
  onClose,
  onUpload,
  isUploading,
  uploadProgress,
  uploadError,
}) => {
  const handleFileSelect = async (file: File) => {
    await onUpload(file);
    if (!uploadError) {
      onClose();
    }
  };

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title="Upload Document"
      description="Add files to your personal knowledge library for AI grounding and search."
      maxWidth="md"
    >
      <DocumentDropzone
        onFileSelected={handleFileSelect}
        isUploading={isUploading}
        uploadProgress={uploadProgress}
        uploadError={uploadError}
      />
    </Modal>
  );
};
