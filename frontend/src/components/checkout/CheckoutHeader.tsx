import React from 'react';
import { Brand } from '../ui/Brand';

interface CheckoutHeaderProps {
  remainingSeconds: number;
}

export const CheckoutHeader: React.FC<CheckoutHeaderProps> = ({ remainingSeconds }) => {
  const minutes = Math.floor(remainingSeconds / 60);
  const seconds = remainingSeconds % 60;
  const isWarning = remainingSeconds < 300;

  return (
    <header className="checkout-header">
      <Brand size={22} />
      <div
        className={`checkout-timer-badge ${isWarning ? 'warning' : ''}`}
        title="Время действия ссылки на оплату"
      >
        <svg
          width="14"
          height="14"
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          strokeWidth="2.2"
          strokeLinecap="round"
          strokeLinejoin="round"
        >
          <circle cx="12" cy="12" r="10" />
          <polyline points="12 6 12 12 16 14" />
        </svg>
        <span>
          {String(minutes).padStart(2, '0')}:{String(seconds).padStart(2, '0')}
        </span>
      </div>
    </header>
  );
};
