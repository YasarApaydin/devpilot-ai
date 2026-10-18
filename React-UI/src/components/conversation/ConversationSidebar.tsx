import React, { useState } from 'react';
import { Link } from 'react-router-dom';
import { ConversationList } from './ConversationList';
import { NewConversationButton } from './NewConversationButton';
import { Modal } from '../ui/Modal';
import { Button } from '../ui/Button';
import { AlertTriangle, Sparkles, MessageSquarePlus, ShieldCheck } from 'lucide-react';
import { FounderModal } from '../founder/FounderModal';
import { FeedbackModal } from '../feedback/FeedbackModal';
import type { Conversation } from '../../types';

export interface ConversationSidebarProps {
  conversations: Conversation[];
  activeConversationId?: string;
  isLoading: boolean;
  onSelect: (id: string) => void;
  onNewConversation: () => void;
  onRename: (id: string, newTitle: string) => void;
  onDelete: (id: string) => Promise<void>;
  onTogglePin: (id: string) => Promise<any> | any;
  isCreatingNew?: boolean;
}

export const ConversationSidebar: React.FC<ConversationSidebarProps> = ({
  conversations,
  activeConversationId,
  isLoading,
  onSelect,
  onNewConversation,
  onRename,
  onDelete,
  onTogglePin,
  isCreatingNew = false,
}) => {
  const [deleteTargetId, setDeleteTargetId] = useState<string | null>(null);
  const [isDeleting, setIsDeleting] = useState(false);
  const [isFounderModalOpen, setIsFounderModalOpen] = useState(false);
  const [isFeedbackModalOpen, setIsFeedbackModalOpen] = useState(false);

  const targetConv = conversations.find((c) => c.id === deleteTargetId);

  const confirmDelete = async () => {
    if (!deleteTargetId) return;
    setIsDeleting(true);
    try {
      await onDelete(deleteTargetId);
      setDeleteTargetId(null);
    } finally {
      setIsDeleting(false);
    }
  };

  return (
    <aside className="w-full h-full flex flex-col p-3.5 bg-[#f1f5f9] border-r border-slate-200">
      <div className="mb-3">
        <NewConversationButton onClick={onNewConversation} isLoading={isCreatingNew} />
      </div>

      <div className="flex-1 overflow-hidden flex flex-col">
        <div className="text-[11px] font-semibold uppercase tracking-wider text-slate-400 px-2 mb-1.5">
          Conversations
        </div>
        <ConversationList
          conversations={conversations}
          activeConversationId={activeConversationId}
          isLoading={isLoading}
          onSelect={onSelect}
          onRename={onRename}
          onDelete={(id) => setDeleteTargetId(id)}
          onTogglePin={onTogglePin}
        />
      </div>

      {/* Footer: User Feedback & Founder Card */}
      <div className="pt-2 mt-auto border-t border-slate-200 space-y-2">
        {/* User Feedback & Bug Report Button */}
        <button
          onClick={() => setIsFeedbackModalOpen(true)}
          className="w-full flex items-center gap-2.5 p-2 rounded-xl bg-blue-50/70 hover:bg-blue-100/70 border border-blue-200/70 hover:border-blue-300 text-left transition-all group shadow-2xs hover:shadow-xs cursor-pointer"
          title="Kullanıcı Görüşleri, Hata & Bug Bildirimi"
        >
          <div className="w-8 h-8 rounded-lg bg-blue-600 text-white flex items-center justify-center shrink-0 shadow-2xs group-hover:scale-105 transition-transform">
            <MessageSquarePlus className="w-4 h-4" />
          </div>
          <div className="min-w-0 flex-1">
            <div className="text-[11px] font-semibold text-slate-800 group-hover:text-blue-700 truncate">
              Görüş &amp; Hata Bildir
            </div>
            <div className="text-[10px] text-slate-500 truncate">
              Öneri, hata veya dosya ilet
            </div>
          </div>
        </button>

        {/* Founder Card */}
        <button
          onClick={() => setIsFounderModalOpen(true)}
          className="w-full flex items-center gap-2.5 p-2 rounded-xl bg-white/80 hover:bg-white border border-slate-200 hover:border-blue-300 text-left transition-all group shadow-2xs hover:shadow-xs cursor-pointer"
          title="DevPilot AI Geliştiricisi & Sistem Mimarı: Yaşar Apaydın"
        >
          <img
            src="/yasar-apaydin.jpg"
            alt="Yaşar Apaydın"
            className="w-8 h-8 rounded-lg object-cover object-top border border-blue-400 shrink-0"
            onError={(e) => {
              (e.target as HTMLElement).style.display = 'none';
            }}
          />
          <div className="min-w-0 flex-1">
            <div className="text-[11px] font-semibold text-slate-800 group-hover:text-blue-600 truncate flex items-center gap-1">
              <span>Yaşar Apaydın</span>
              <Sparkles className="w-2.5 h-2.5 text-blue-500 shrink-0" />
            </div>
            <div className="text-[10px] text-slate-500 truncate">
              Geliştirici &amp; Sistem Mimarı
            </div>
          </div>
        </button>

        {/* Privacy & Cookie Preferences shortcut */}
        <div className="pt-1.5 flex items-center justify-between text-[10px] text-slate-400 px-1 border-t border-slate-100">
          <Link
            to="/app/profile?tab=privacy"
            className="hover:text-blue-600 transition-colors flex items-center gap-1"
            title="Gizlilik ve Çerez Tercihlerini Yönet"
          >
            <ShieldCheck className="w-3 h-3 text-slate-400" />
            <span>Gizlilik &amp; Çerezler</span>
          </Link>
          <Link
            to="/cookies"
            className="hover:text-blue-600 transition-colors"
            title="Çerez Politikası"
          >
            Politika
          </Link>
        </div>
      </div>

      {/* User Feedback Modal */}
      <FeedbackModal
        isOpen={isFeedbackModalOpen}
        onClose={() => setIsFeedbackModalOpen(false)}
      />

      {/* Founder CV / Profile Modal */}
      <FounderModal
        isOpen={isFounderModalOpen}
        onClose={() => setIsFounderModalOpen(false)}
      />

      {/* Delete Confirmation Modal */}
      <Modal
        isOpen={!!deleteTargetId}
        onClose={() => setDeleteTargetId(null)}
        title="Delete Conversation"
      >
        <div className="space-y-4">
          <div className="flex items-start gap-3 p-3 rounded-lg bg-rose-50 border border-rose-200 text-xs text-rose-800">
            <AlertTriangle className="w-4 h-4 text-rose-600 shrink-0 mt-0.5" />
            <div>
              Are you sure you want to delete{' '}
              <strong className="text-slate-900 font-semibold">
                "{targetConv?.title || 'this conversation'}"
              </strong>
              ? This action cannot be undone.
            </div>
          </div>

          <div className="flex items-center justify-end gap-2 pt-2">
            <Button
              variant="outline"
              size="sm"
              onClick={() => setDeleteTargetId(null)}
              disabled={isDeleting}
            >
              Cancel
            </Button>
            <Button
              variant="danger"
              size="sm"
              onClick={confirmDelete}
              isLoading={isDeleting}
              loadingText="Deleting..."
            >
              Delete Conversation
            </Button>
          </div>
        </div>
      </Modal>
    </aside>
  );
};
