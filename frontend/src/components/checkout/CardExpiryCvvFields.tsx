import React from 'react';
import { formatExpiryDate } from '../../utils/formatters';

export interface CardExpiryCvvFieldsProps {
  expiry: string;
  cvv: string;
  onExpiryChange: (val: string) => void;
  onCvvChange: (val: string) => void;
}

export const CardExpiryCvvFields: React.FC<CardExpiryCvvFieldsProps> = ({
  expiry,
  cvv,
  onExpiryChange,
  onCvvChange
}) => {
  const handleExpiryChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const raw = e.target.value.replace(/\D/g, '').slice(0, 4);
    onExpiryChange(formatExpiryDate(raw));
  };

  const handleCvvChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const raw = e.target.value.replace(/\D/g, '').slice(0, 4);
    onCvvChange(raw);
  };

  return (
    <div className="grid grid-cols-2 gap-4">
      <div className="space-y-1">
        <label className="text-xs font-semibold text-dp-blue">Срок действия</label>
        <input
          type="text"
          inputMode="numeric"
          autoComplete="cc-exp"
          placeholder="ММ/ГГ"
          value={expiry}
          onChange={handleExpiryChange}
          className="w-full px-4 py-3 rounded-card border border-dp-line font-mono text-base tracking-wider bg-white focus:outline-none focus:border-dp-blue focus:ring-1 focus:ring-dp-blue/30 transition-colors"
        />
      </div>

      <div className="space-y-1">
        <div className="flex items-center justify-between">
          <label className="text-xs font-semibold text-dp-blue">CVC / CVV</label>
          <span className="text-[10px] text-dp-blue/50 font-medium">3 цифры сзади</span>
        </div>
        <div className="relative">
          <input
            type="password"
            inputMode="numeric"
            autoComplete="cc-csc"
            placeholder="•••"
            maxLength={4}
            value={cvv}
            onChange={handleCvvChange}
            className="w-full px-4 py-3 rounded-card border border-dp-line font-mono text-base tracking-widest bg-white focus:outline-none focus:border-dp-blue focus:ring-1 focus:ring-dp-blue/30 transition-colors"
          />
          <div className="absolute right-3.5 top-1/2 -translate-y-1/2 text-dp-blue/40">
            <svg className="w-4 h-4" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
              <rect x="3" y="11" width="18" height="11" rx="2" ry="2" />
              <path d="M7 11V7a5 5 0 0 1 10 0v4" />
            </svg>
          </div>
        </div>
      </div>
    </div>
  );
};
