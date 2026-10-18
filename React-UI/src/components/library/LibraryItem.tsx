import React, { useState, useRef, useEffect } from 'react';
import { FileText, MoreVertical, Trash2, ExternalLink, Sparkles } from 'lucide-react';
import { formatFileSize } from '../../utils/formatFileSize';
import { formatDate } from '../../utils/formatDate';
import { Badge } from '../ui/Badge';
import type { DocumentStatus } from '../../types';

export interface LibraryItemProps {
  id: string;
  fileName: string;
  size: number;
  status: DocumentStatus;
  createdAt: string;
  onDelete: (id: string) => void;
  onOpen?: (id: string) => void;
  onAnalyze?: (id: string, fileName: string) => void;
}

export const LibraryItem: React.FC<LibraryItemProps> = ({
  id,
  fileName,
  size,
  status,
  createdAt,
  onDelete,
  onOpen,
  onAnalyze,
}) => {
  const [menuOpen, setMenuOpen] = useState(false);
  const menuRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const handleClickOutside = (e: MouseEvent) => {
      if (menuRef.current && !menuRef.current.contains(e.target as Node)) {
        setMenuOpen(false);
      }
    };
    if (menuOpen) {
      document.addEventListener('mousedown', handleClickOutside);
    }
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, [menuOpen]);

  return (
    <div className="group relative flex items-start justify-between p-3 rounded-xl bg-white border border-slate-200 shadow-2xs hover:border-slate-300 transition-all">
      <div
        className="flex items-start gap-2.5 min-w-0 flex-1 cursor-pointer"
        onClick={() => onOpen?.(id)}
      >
        <div className="w-8 h-8 rounded-lg bg-blue-50 border border-blue-100 text-blue-600 flex items-center justify-center shrink-0 mt-0.5">
          <FileText className="w-4 h-4" />
        </div>

        <div className="min-w-0 flex-1">
          <div className="flex items-center gap-2">
            <h4 className="text-xs font-semibold text-slate-800 truncate group-hover:text-blue-600 transition-colors">
              {fileName}
            </h4>
          </div>

          <div className="flex items-center gap-2 mt-1.5 flex-wrap">
            <Badge status={status} />
            <span className="text-[11px] text-slate-500 font-mono">
              {formatFileSize(size)}
            </span>
            <span className="text-[11px] text-slate-400">
              {formatDate(createdAt)}
            </span>

            {onAnalyze && (
              <button
                type="button"
                onClick={() => onAnalyze(id, fileName)}
                className="inline-flex items-center gap-1 px-2 py-0.5 text-[11px] font-medium text-purple-700 bg-purple-50 hover:bg-purple-100 border border-purple-200/80 rounded-md transition-colors"
                title="Bu dokümanı yapay zekaya incelet"
              >
                <Sparkles className="w-3 h-3 text-purple-600" />
                <span>AI ile İncele</span>
              </button>
            )}
          </div>
        </div>
      </div>

      {/* Options Menu */}
      <div className="relative shrink-0 ml-1" ref={menuRef}>
        <button
          onClick={() => setMenuOpen(!menuOpen)}
          className="p-1 rounded-md text-slate-400 hover:text-slate-700 hover:bg-slate-100 transition-colors"
        >
          <MoreVertical className="w-3.5 h-3.5" />
        </button>

        {menuOpen && (
          <div className="absolute right-0 top-full mt-1 w-36 bg-white border border-slate-200 rounded-lg shadow-lg py-1 z-30">
            {onAnalyze && (
              <button
                onClick={() => {
                  setMenuOpen(false);
                  onAnalyze(id, fileName);
                }}
                className="w-full flex items-center gap-2 px-3 py-1.5 text-xs text-purple-700 hover:bg-purple-50 text-left transition-colors font-medium"
              >
                <Sparkles className="w-3 h-3 text-purple-600" />
                AI ile İncele
              </button>
            )}
            {onOpen && (
              <button
                onClick={() => {
                  setMenuOpen(false);
                  onOpen(id);
                }}
                className="w-full flex items-center gap-2 px-3 py-1.5 text-xs text-slate-700 hover:bg-slate-50 text-left transition-colors"
              >
                <ExternalLink className="w-3 h-3 text-slate-400" />
                Görüntüle
              </button>
            )}
            <button
              onClick={() => {
                setMenuOpen(false);
                onDelete(id);
              }}
              className="w-full flex items-center gap-2 px-3 py-1.5 text-xs text-rose-600 hover:bg-rose-50 text-left transition-colors"
            >
              <Trash2 className="w-3 h-3 text-rose-500" />
              Sil
            </button>
          </div>
        )}
      </div>
    </div>
  );
};
