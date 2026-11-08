import React from 'react';
import { PaymentMethodType } from '../../types/PaymentMethodType';

export interface MethodTabButtonProps {
  type: PaymentMethodType;
  label: string;
  badge?: string;
  isActive: boolean;
  onClick: (type: PaymentMethodType) => void;
}

export const MethodTabButton: React.FC<MethodTabButtonProps> = ({
  type,
  label,
  badge,
  isActive,
  onClick
}) => {
  return (
    <button
      type="button"
      onClick={() => onClick(type)}
      className={`flex-1 py-2.5 px-3 rounded-pill text-xs font-semibold transition-all cursor-pointer text-center relative select-none flex items-center justify-center gap-1.5 ${
        isActive
          ? 'bg-dp-blue text-white shadow-sm'
          : 'bg-transparent text-dp-blue hover:bg-dp-tint'
      }`}
    >
      <span>{label}</span>
      {badge && (
        <span
          className={`text-[10px] px-1.5 py-0.2 rounded-full font-bold ${
            isActive ? 'bg-dp-pink text-dp-blue' : 'bg-dp-blush text-dp-blue'
          }`}
        >
          {badge}
        </span>
      )}
    </button>
  );
};
