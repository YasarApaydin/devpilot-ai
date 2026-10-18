import React from 'react';
import type { DocumentStatus } from '../../types';

interface BadgeProps {
  status?: DocumentStatus | 'active' | 'inactive' | 'verified' | 'unverified';
  children?: React.ReactNode;
  className?: string;
}

export const Badge: React.FC<BadgeProps> = ({ status, children, className = '' }) => {
  let style = 'bg-slate-100 text-slate-700 border-slate-200';

  if (status === 'Ready' || status === 'active' || status === 'verified') {
    style = 'bg-emerald-50 text-emerald-700 border-emerald-200';
  } else if (status === 'Processed') {
    style = 'bg-blue-50 text-blue-700 border-blue-200';
  } else if (status === 'Processing') {
    style = 'bg-amber-50 text-amber-700 border-amber-200';
  } else if (status === 'Failed' || status === 'unverified') {
    style = 'bg-rose-50 text-rose-700 border-rose-200';
  }

  return (
    <span
      className={`inline-flex items-center px-2 py-0.5 rounded-md text-[11px] font-medium border ${style} ${className}`}
    >
      {children || status}
    </span>
  );
};
