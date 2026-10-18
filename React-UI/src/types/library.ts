/**
 * Library and Document Types
 */

export type DocumentStatus = 'Ready' | 'Processing' | 'Processed' | 'Failed';

export interface LibraryDocument {
  id: string;
  fileName: string;
  size: number;
  status: DocumentStatus;
  fileType: string;
  createdAt: string;
  updatedAt: string;
  downloadUrl?: string;
  summary?: string;
}

export interface UploadDocumentResponse {
  document: LibraryDocument;
  message?: string;
}

export interface DocumentChunk {
  id: string;
  chunkIndex: number;
  content: string;
  tokenCount?: number;
}

export interface DocumentDetail {
  id: string;
  fileName: string;
  size: number;
  status: DocumentStatus;
  fileType: string;
  createdAt: string;
  summary?: string;
  totalTokens: number;
  chunks: DocumentChunk[];
  fullContent: string;
}
