import React from 'react';
import { Plus } from 'lucide-react';
import { Button } from '../ui/Button';

interface NewConversationButtonProps {
  onClick: () => void;
  isLoading?: boolean;
}

export const NewConversationButton: React.FC<NewConversationButtonProps> = ({
  onClick,
  isLoading = false,
}) => {
  return (
    <Button
      variant="secondary"
      size="md"
      className="w-full justify-start bg-white hover:bg-slate-50 border border-slate-300/90 text-slate-700 hover:text-slate-900 font-medium shadow-2xs"
      leftIcon={<Plus className="w-4 h-4 text-blue-600" />}
      onClick={onClick}
      isLoading={isLoading}
    >
      New Conversation
    </Button>
  );
};
