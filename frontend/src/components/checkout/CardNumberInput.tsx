import React from 'react';
import { formatCardNumber } from '../../utils/formatters';
import { detectCardBrand, validateCardNumberLuhn } from '../../utils/validation';

export interface CardNumberInputProps {
  value: string;
  onChange: (value: string) => void;
}

export const CardNumberInput: React.FC<CardNumberInputProps> = ({ value, onChange }) => {
  const brandInfo = detectCardBrand(value);
  const isValid = value.replace(/\s/g, '').length >= 16 ? validateCardNumberLuhn(value) : true;

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const raw = e.target.value.replace(/\D/g, '').slice(0, 16);
    onChange(formatCardNumber(raw));
  };

  return (
    <div className="space-y-1">
      <div className="flex items-center justify-between">
        <label className="text-xs font-semibold text-dp-blue">Номер карты</label>
        {brandInfo.brand !== 'generic' && (
          <span className="text-[11px] font-bold uppercase tracking-wider px-2 py-0.5 rounded bg-dp-tint text-dp-blue border border-dp-line">
            {brandInfo.name}
          </span>
        )}
      </div>
      <div className="relative">
        <input
          type="text"
          inputMode="numeric"
          autoComplete="cc-number"
          placeholder="0000 0000 0000 0000"
          value={value}
          onChange={handleChange}
          className={`w-full px-4 py-3 rounded-card border font-mono text-base tracking-wider bg-white focus:outline-none transition-colors ${
            !isValid && value.replace(/\s/g, '').length >= 16
              ? 'border-dp-red focus:border-dp-red ring-1 ring-dp-red/30'
              : 'border-dp-line focus:border-dp-blue focus:ring-1 focus:ring-dp-blue/30'
          }`}
        />
        <div className="absolute right-3.5 top-1/2 -translate-y-1/2 text-dp-blue/40">
          <svg className="w-5 h-5" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
            <rect x="1" y="4" width="22" height="16" rx="2" ry="2" />
            <line x1="1" y1="10" x2="23" y2="10" />
          </svg>
        </div>
      </div>
      {!isValid && value.replace(/\s/g, '').length >= 16 && (
        <p className="text-[11px] text-dp-red font-medium">Неверный контрольный номер карты</p>
      )}
    </div>
  );
};
