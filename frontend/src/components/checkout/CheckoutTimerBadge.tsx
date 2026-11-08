import React from 'react';
import { formatTimer } from '../../utils/formatters';

export interface CheckoutTimerBadgeProps {
  remainingSeconds: number;
}

export const CheckoutTimerBadge: React.FC<CheckoutTimerBadgeProps> = ({ remainingSeconds }) => {
  const isWarning = remainingSeconds < 300;

  return (
    <div
      className={`inline-flex items-center gap-1.5 px-3 py-1.5 rounded-pill font-mono text-xs font-semibold border transition-colors ${
        isWarning
          ? 'bg-dp-amber-blush text-dp-amber border-amber-500/30'
          : 'bg-dp-tint text-dp-blue border-dp-line'
      }`}
      title="Время на завершение оплаты"
    >
      <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
        <circle cx="12" cy="12" r="10" />
        <polyline points="12 6 12 12 16 14" />
      </svg>
      <span>{formatTimer(remainingSeconds)}</span>
    </div>
  );
};
