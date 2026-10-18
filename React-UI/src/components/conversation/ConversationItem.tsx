import React, { useState, useRef, useEffect } from 'react';
import {
  MessageSquare,
  MoreVertical,
  Edit2,
  Trash2,
  Check,
  X,
  Pin,
  Clock,
} from 'lucide-react';

export interface ConversationItemProps {
  id: string;
  title: string;
  createdAt?: string;
  isPinned?: boolean;
  isActive: boolean;
  onSelect: (id: string) => void;
  onRename: (id: string, newTitle: string) => void;
  onDelete: (id: string) => void;
  onTogglePin?: (id: string) => void;
}

const formatTimestamp = (dateString?: string) => {
  if (!dateString) return null;
  try {
    const d = new Date(dateString);
    if (isNaN(d.getTime())) return null;
    const day = String(d.getDate()).padStart(2, '0');
    const month = String(d.getMonth() + 1).padStart(2, '0');
    const year = d.getFullYear();
    const hours = String(d.getHours()).padStart(2, '0');
    const minutes = String(d.getMinutes()).padStart(2, '0');
    return {
      date: `${day}.${month}.${year}`,
      time: `${hours}:${minutes}`,
      display: `${day}.${month}.${year} • ${hours}:${minutes}`,
    };
  } catch {
    return null;
  }
};

export const ConversationItem: React.FC<ConversationItemProps> = ({
  id,
  title,
  createdAt,
  isPinned = false,
  isActive,
  onSelect,
  onRename,
  onDelete,
  onTogglePin,
}) => {
  const [isEditing, setIsEditing] = useState(false);
  const [editedTitle, setEditedTitle] = useState(title);
  const [menuOpen, setMenuOpen] = useState(false);
  const menuRef = useRef<HTMLDivElement>(null);
  const inputRef = useRef<HTMLInputElement>(null);

  const formattedTime = formatTimestamp(createdAt);

  useEffect(() => {
    setEditedTitle(title);
  }, [title]);

  useEffect(() => {
    if (isEditing) {
      inputRef.current?.focus();
      inputRef.current?.select();
    }
  }, [isEditing]);

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

  const handleSaveRename = (e: React.FormEvent) => {
    e.preventDefault();
    if (editedTitle.trim() && editedTitle !== title) {
      onRename(id, editedTitle.trim());
    }
    setIsEditing(false);
  };

  const handleCancelRename = () => {
    setEditedTitle(title);
    setIsEditing(false);
  };

  return (
    <div
      className={`group relative flex flex-col px-3 py-2 rounded-xl text-xs transition-all cursor-pointer select-none ${
        menuOpen ? 'z-50 shadow-md ring-1 ring-slate-300' : 'z-0'
      } ${
        isActive
          ? 'bg-blue-50/90 text-blue-900 border border-blue-200 shadow-2xs font-medium'
          : isPinned
          ? 'bg-amber-50/50 text-slate-700 hover:bg-amber-50/90 border border-amber-200/60'
          : 'text-slate-600 hover:bg-slate-200/70 hover:text-slate-900 border border-transparent'
      }`}
      onClick={() => {
        if (!isEditing) onSelect(id);
      }}
    >
      {/* Top Row: Icon + Title + Quick Actions */}
      <div className="flex items-center justify-between gap-1.5 min-w-0 w-full">
        <div className="flex items-center gap-2 min-w-0 flex-1">
          <MessageSquare
            className={`w-3.5 h-3.5 shrink-0 ${
              isActive
                ? 'text-blue-600'
                : isPinned
                ? 'text-amber-600'
                : 'text-slate-400 group-hover:text-slate-600'
            }`}
          />

          {isEditing ? (
            <form
              onSubmit={handleSaveRename}
              className="flex items-center gap-1 flex-1 min-w-0"
              onClick={(e) => e.stopPropagation()}
            >
              <input
                ref={inputRef}
                type="text"
                value={editedTitle}
                onChange={(e) => setEditedTitle(e.target.value)}
                className="bg-white border border-slate-300 text-slate-800 rounded px-2 py-0.5 text-xs w-full focus:outline-none focus:border-blue-500 shadow-2xs"
              />
              <button
                type="submit"
                className="p-1 hover:text-emerald-600 text-slate-500 transition-colors"
                title="Kaydet"
              >
                <Check className="w-3.5 h-3.5" />
              </button>
              <button
                type="button"
                onClick={handleCancelRename}
                className="p-1 hover:text-rose-600 text-slate-500 transition-colors"
                title="İptal"
              >
                <X className="w-3.5 h-3.5" />
              </button>
            </form>
          ) : (
            <div className="flex items-center gap-1.5 truncate flex-1">
              <span className="truncate">{title}</span>
              {isPinned && (
                <span title="Yukarı Sabitlendi" className="inline-flex items-center shrink-0">
                  <Pin className="w-3 h-3 text-amber-600 fill-amber-500 rotate-45" />
                </span>
              )}
            </div>
          )}
        </div>

        {/* Action Buttons (Pin, More) */}
        {!isEditing && (
          <div
            className="flex items-center gap-0.5 shrink-0"
            onClick={(e) => e.stopPropagation()}
          >
            {/* Quick Pin Button (Only shown on hover when NOT pinned, ensuring strictly 1 pin icon when pinned!) */}
            {onTogglePin && !isPinned && (
              <button
                onClick={() => onTogglePin(id)}
                className="p-1 rounded-md transition-all text-slate-400 hover:text-slate-700 hover:bg-slate-200/60 opacity-0 group-hover:opacity-100"
                title="Yukarı Sabitle"
              >
                <Pin className="w-3 h-3" />
              </button>
            )}

            {/* Menu Dropdown Trigger */}
            <div className="relative" ref={menuRef}>
              <button
                onClick={() => setMenuOpen(!menuOpen)}
                className={`p-1 rounded-md text-slate-400 hover:text-slate-700 transition-opacity cursor-pointer ${
                  menuOpen || isActive ? 'opacity-100 text-slate-800 bg-slate-200/50' : 'opacity-0 group-hover:opacity-100'
                }`}
                title="Seçenekler"
              >
                <MoreVertical className="w-3.5 h-3.5" />
              </button>

              {menuOpen && (
                <div className="absolute right-0 top-full mt-1.5 w-40 bg-white border border-slate-200 rounded-xl shadow-2xl py-1 z-50 ring-1 ring-black/5 divide-y divide-slate-100">
                  <div className="py-0.5">
                    {onTogglePin && (
                      <button
                        onClick={() => {
                          setMenuOpen(false);
                          onTogglePin(id);
                        }}
                        className="w-full flex items-center gap-2 px-3 py-1.5 text-xs text-slate-700 hover:bg-slate-100 text-left transition-colors cursor-pointer"
                      >
                        <Pin className={`w-3.5 h-3.5 ${isPinned ? 'fill-amber-500 rotate-45 text-amber-600' : 'text-slate-500'}`} />
                        <span>{isPinned ? 'Sabitlemeyi Kaldır' : 'Yukarı Sabitle'}</span>
                      </button>
                    )}

                    <button
                      onClick={() => {
                        setMenuOpen(false);
                        setIsEditing(true);
                      }}
                      className="w-full flex items-center gap-2 px-3 py-1.5 text-xs text-slate-700 hover:bg-slate-100 text-left transition-colors cursor-pointer"
                    >
                      <Edit2 className="w-3.5 h-3.5 text-slate-500" />
                      <span>Yeniden Adlandır</span>
                    </button>
                  </div>

                  <div className="py-0.5">
                    <button
                      onClick={() => {
                        setMenuOpen(false);
                        onDelete(id);
                      }}
                      className="w-full flex items-center gap-2 px-3 py-1.5 text-xs text-rose-600 hover:bg-rose-50 text-left transition-colors cursor-pointer"
                    >
                      <Trash2 className="w-3.5 h-3.5 text-rose-600" />
                      <span>Sohbeti Sil</span>
                    </button>
                  </div>
                </div>
              )}
            </div>
          </div>
        )}
      </div>

      {/* Bottom Sub-row: Timestamp Display (DD.MM.YYYY • HH:mm) */}
      {formattedTime && !isEditing && (
        <div className="flex items-center text-[10px] text-slate-400 group-hover:text-slate-500 mt-1 pl-5.5">
          <div className="flex items-center gap-1">
            <Clock className="w-2.5 h-2.5 opacity-70" />
            <span>{formattedTime.date}</span>
            <span>•</span>
            <span className="font-mono">{formattedTime.time}</span>
          </div>
        </div>
      )}
    </div>
  );
};
