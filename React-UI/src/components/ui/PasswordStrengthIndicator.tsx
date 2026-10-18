import React from 'react';
import { Check, Circle } from 'lucide-react';
import { evaluatePassword } from '../../utils/validation';

interface PasswordStrengthIndicatorProps {
  password?: string;
  className?: string;
}

export const PasswordStrengthIndicator: React.FC<PasswordStrengthIndicatorProps> = ({
  password = '',
  className = '',
}) => {
  const { criteria, metCount, total, strengthLabel, strengthColor } = evaluatePassword(password);

  if (!password) {
    return null;
  }

  return (
    <div className={`mt-2 p-3 rounded-xl bg-slate-50 border border-slate-200/90 space-y-2.5 transition-all text-xs ${className}`}>
      {/* Strength Bar & Label */}
      <div className="flex items-center justify-between text-[11px]">
        <span className="text-slate-500 font-medium">Şifre Gücü:</span>
        <span className="font-semibold text-slate-700">
          {strengthLabel} ({metCount}/{total})
        </span>
      </div>

      {/* Segmented Progress Bar */}
      <div className="grid grid-cols-5 gap-1.5 h-1.5 w-full">
        {Array.from({ length: total }).map((_, index) => {
          const isFilled = index < metCount;
          return (
            <div
              key={index}
              className={`h-full rounded-full transition-all duration-300 ${
                isFilled ? strengthColor : 'bg-slate-200'
              }`}
            />
          );
        })}
      </div>

      {/* Criteria Checklist */}
      <div className="grid grid-cols-1 sm:grid-cols-2 gap-1.5 pt-1">
        {criteria.map((item) => (
          <div
            key={item.id}
            className={`flex items-center gap-1.5 text-[11px] transition-colors ${
              item.met ? 'text-emerald-700 font-medium' : 'text-slate-400'
            }`}
          >
            {item.met ? (
              <Check className="w-3.5 h-3.5 text-emerald-600 shrink-0" />
            ) : (
              <Circle className="w-3 h-3 text-slate-300 shrink-0" />
            )}
            <span>{item.label}</span>
          </div>
        ))}
      </div>
    </div>
  );
};
