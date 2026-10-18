import { useState, useEffect, useCallback } from 'react';
import { libraryApi } from '../api/libraryApi';
import { ENV } from '../config/env';
import { useDebounce } from './useDebounce';
import type { LibraryDocument } from '../types';

export function useLibrary(enabled: boolean = true) {
  const [documents, setDocuments] = useState<LibraryDocument[]>([]);
  const [searchTerm, setSearchTerm] = useState<string>('');
  const debouncedSearch = useDebounce(searchTerm, 350);

  const [isLoading, setIsLoading] = useState<boolean>(enabled);
  const [isUploading, setIsUploading] = useState<boolean>(false);
  const [uploadProgress, setUploadProgress] = useState<number>(0);
  const [uploadError, setUploadError] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const fetchDocuments = useCallback(async (searchQuery?: string) => {
    setIsLoading(true);
    setError(null);
    try {
      const res = await libraryApi.getDocuments(searchQuery);
      if (res.data) {
        setDocuments(res.data);
      }
    } catch (err: any) {
      setError(err?.message || 'Failed to load documents.');
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    if (!enabled) {
      setIsLoading(false);
      return;
    }
    fetchDocuments(debouncedSearch);
  }, [enabled, debouncedSearch, fetchDocuments]);

  const uploadDocument = async (file: File): Promise<LibraryDocument> => {
    // Check max file size
    const maxBytes = ENV.maxUploadMb * 1024 * 1024;
    if (file.size > maxBytes) {
      const msg = `File exceeds maximum allowed size of ${ENV.maxUploadMb} MB.`;
      setUploadError(msg);
      throw new Error(msg);
    }

    setIsUploading(true);
    setUploadProgress(0);
    setUploadError(null);

    try {
      const res = await libraryApi.uploadDocument(file, (percent) => {
        setUploadProgress(percent);
      });

      if (res.data?.document) {
        setDocuments((prev) => [res.data.document, ...prev]);
        return res.data.document;
      }
      throw new Error('Upload failed: no document returned.');
    } catch (err: any) {
      const msg = err?.message || 'Failed to upload document.';
      setUploadError(msg);
      throw new Error(msg);
    } finally {
      setIsUploading(false);
      setUploadProgress(0);
    }
  };

  const deleteDocument = async (id: string): Promise<void> => {
    await libraryApi.deleteDocument(id);
    setDocuments((prev) => prev.filter((d) => d.id !== id));
  };

  return {
    documents,
    searchTerm,
    setSearchTerm,
    isLoading,
    isUploading,
    uploadProgress,
    uploadError,
    error,
    refresh: () => fetchDocuments(searchTerm),
    uploadDocument,
    deleteDocument,
  };
}
