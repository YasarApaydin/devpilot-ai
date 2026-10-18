import React from 'react';
import { ConversationItem } from './ConversationItem';
import { EmptyState } from '../ui/EmptyState';
import { Spinner } from '../ui/Spinner';
import { MessageSquareDashed, Pin } from 'lucide-react';
import type { Conversation } from '../../types';

export interface ConversationListProps {
  conversations: Conversation[];
  activeConversationId?: string;
  isLoading: boolean;
  onSelect: (id: string) => void;
  onRename: (id: string, newTitle: string) => void;
  onDelete: (id: string) => void;
  onTogglePin?: (id: string) => void;
}

export const ConversationList: React.FC<ConversationListProps> = ({
  conversations,
  activeConversationId,
  isLoading,
  onSelect,
  onRename,
  onDelete,
  onTogglePin,
}) => {
  if (isLoading) {
    return (
      <div className="py-8 flex flex-col items-center justify-center gap-2 text-slate-500 text-xs">
        <Spinner size="sm" />
        <span>Sohbetler yükleniyor...</span>
      </div>
    );
  }

  if (conversations.length === 0) {
    return (
      <EmptyState
        icon={<MessageSquareDashed className="w-5 h-5 text-slate-400" />}
        title="Henüz sohbet yok"
        description="Yukarıdaki '+ Yeni Sohbet' butonuna tıklayarak DevPilot AI ile çalışmaya başlayabilirsiniz."
        className="my-3 py-6"
      />
    );
  }

  const pinnedConversations = conversations.filter((c) => !!c.isPinned);
  const unpinnedConversations = conversations.filter((c) => !c.isPinned);

  return (
    <div className="space-y-1 overflow-y-auto max-h-[calc(100vh-270px)] pr-1 pb-16 scrollbar-thin">
      {/* Pinned Section */}
      {pinnedConversations.length > 0 && (
        <div className="space-y-1 mb-2">
          <div className="text-[10px] font-bold uppercase tracking-wider text-amber-700/90 px-2 pt-1 pb-0.5 flex items-center gap-1 select-none">
            <Pin className="w-2.5 h-2.5 rotate-45 fill-amber-500 text-amber-600" />
            <span>Sabitlenenler ({pinnedConversations.length})</span>
          </div>
          {pinnedConversations.map((conv) => (
            <ConversationItem
              key={conv.id}
              id={conv.id}
              title={conv.title}
              createdAt={conv.createdAt}
              isPinned={true}
              isActive={conv.id === activeConversationId}
              onSelect={onSelect}
              onRename={onRename}
              onDelete={onDelete}
              onTogglePin={onTogglePin}
            />
          ))}
        </div>
      )}

      {/* Unpinned / All Conversations Section */}
      {pinnedConversations.length > 0 && unpinnedConversations.length > 0 && (
        <div className="text-[10px] font-bold uppercase tracking-wider text-slate-400 px-2 pt-1 pb-0.5 select-none">
          <span>Diğer Sohbetler</span>
        </div>
      )}

      {unpinnedConversations.map((conv) => (
        <ConversationItem
          key={conv.id}
          id={conv.id}
          title={conv.title}
          createdAt={conv.createdAt}
          isPinned={false}
          isActive={conv.id === activeConversationId}
          onSelect={onSelect}
          onRename={onRename}
          onDelete={onDelete}
          onTogglePin={onTogglePin}
        />
      ))}
    </div>
  );
};
