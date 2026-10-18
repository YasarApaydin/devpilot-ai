import { apiClient } from './apiClient';
import { ENDPOINTS } from '../config/endpoints';
import { ENV } from '../config/env';
import { MOCK_DOCUMENTS } from './mockData';
import type { LibraryDocument, ApiResponse, UploadDocumentResponse } from '../types';

let mockDocs = [...MOCK_DOCUMENTS];

export const libraryApi = {
  async getDocuments(search?: string): Promise<ApiResponse<LibraryDocument[]>> {
    try {
      return await apiClient.get<ApiResponse<LibraryDocument[]>>(
        ENDPOINTS.LIBRARY.LIST,
        search ? { search } : undefined
      );
    } catch (error) {
      if (ENV.enableMockFallback) {
        let filtered = [...mockDocs];
        if (search && search.trim()) {
          const q = search.toLowerCase();
          filtered = filtered.filter(
            (d) =>
              d.fileName.toLowerCase().includes(q) ||
              (d.summary && d.summary.toLowerCase().includes(q))
          );
        }
        return {
          success: true,
          data: filtered,
        };
      }
      throw error;
    }
  },

  async uploadDocument(
    file: File,
    onProgress?: (percent: number) => void
  ): Promise<ApiResponse<UploadDocumentResponse>> {
    const formData = new FormData();
    formData.append('file', file);

    try {
      return await apiClient.upload<ApiResponse<UploadDocumentResponse>>(
        ENDPOINTS.LIBRARY.UPLOAD,
        formData,
        onProgress
      );
    } catch (error) {
      if (ENV.enableMockFallback) {
        // Mock upload simulation
        if (onProgress) {
          onProgress(30);
          await new Promise((r) => setTimeout(r, 200));
          onProgress(70);
          await new Promise((r) => setTimeout(r, 200));
          onProgress(100);
        }

        const newDoc: LibraryDocument = {
          id: `doc_${Date.now()}`,
          fileName: file.name,
          size: file.size,
          status: 'Processed',
          fileType: file.type || 'application/pdf',
          createdAt: new Date().toISOString(),
          updatedAt: new Date().toISOString(),
          summary: `Uploaded document: ${file.name} successfully parsed for vector knowledge.`,
        };

        mockDocs = [newDoc, ...mockDocs];

        return {
          success: true,
          data: {
            document: newDoc,
            message: 'Document uploaded and indexed successfully.',
          },
        };
      }
      throw error;
    }
  },

  async deleteDocument(id: string): Promise<ApiResponse<void>> {
    try {
      return await apiClient.delete<ApiResponse<void>>(ENDPOINTS.LIBRARY.DELETE(id));
    } catch (error) {
      if (ENV.enableMockFallback) {
        mockDocs = mockDocs.filter((d) => d.id !== id);
        return {
          success: true,
          data: undefined,
        };
      }
      throw error;
    }
  },

  async getDocumentDetail(id: string): Promise<ApiResponse<import('../types').DocumentDetail>> {
    try {
      return await apiClient.get<ApiResponse<import('../types').DocumentDetail>>(ENDPOINTS.LIBRARY.DETAIL(id));
    } catch (error) {
      if (ENV.enableMockFallback) {
        const found = mockDocs.find((d) => d.id === id);
        return {
          success: true,
          data: {
            id: id,
            fileName: found?.fileName || 'Doküman.pdf',
            size: found?.size || 1024,
            status: 'Processed',
            fileType: 'application/pdf',
            createdAt: found?.createdAt || new Date().toISOString(),
            summary: found?.summary || 'Doküman başarıyla işlendi ve RAG vektör aramasına hazır.',
            totalTokens: 120,
            chunks: [
              {
                id: `chunk_1`,
                chunkIndex: 0,
                content: `${found?.fileName || 'Doküman'} içeriği: Bu belge DevPilot AI sistemine başarıyla indekslenmiş metin ve içerikleri barındırır.`,
                tokenCount: 40,
              },
            ],
            fullContent: `${found?.fileName || 'Doküman'} içeriği:\n\nBu belge DevPilot AI sistemine başarıyla indekslenmiş metin ve içerikleri barındırır. RAG motoru bu metinleri sorularınızda otomatik olarak referans alır.`,
          },
        };
      }
      throw error;
    }
  },
};
