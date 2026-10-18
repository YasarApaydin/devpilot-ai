import React from 'react';
import { Loader2 } from 'lucide-react';

export const TypingIndicator: React.FC = () => {
  return (
    <div className="flex items-center justify-center p-2.5 rounded-xl bg-white border border-slate-200 text-blue-600 w-fit shadow-xs">
      <Loader2 className="w-4 h-4 animate-spin text-blue-600" />
    </div>
  );
};
