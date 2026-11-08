import React from 'react';
import { Button } from '../ui/Button';
import { Brand } from '../ui/Brand';

export interface CheckoutErrorStateProps {
  message: string;
  onRetry?: () => void;
}

export const CheckoutErrorState: React.FC<CheckoutErrorStateProps> = ({ message, onRetry }) => {
  return (
    <div className="w-full max-w-[480px] mx-auto p-8 bg-white border border-dp-line rounded-card shadow-card text-center space-y-5">
      <Brand size={24} className="justify-center" />

      <div className="w-14 h-14 mx-auto rounded-full bg-dp-red-blush text-dp-red border-2 border-red-500/40 flex items-center justify-center">
        <svg className="w-7 h-7" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5">
          <circle cx="12" cy="12" r="10" />
          <line x1="12" y1="8" x2="12" y2="12" />
          <line x1="12" y1="16" x2="12.01" y2="16" />
        </svg>
      </div>

      <div className="space-y-2">
        <h2 className="text-lg font-bold text-dp-blue">Ошибка сессии оплаты</h2>
        <p className="text-xs text-dp-blue/70 max-w-[340px] mx-auto leading-relaxed">
          {message}
        </p>
      </div>

      <div className="pt-2 flex flex-col gap-2">
        {onRetry && (
          <Button variant="primary-blue" size="md" full onClick={onRetry}>
            Повторить попытку
          </Button>
        )}
        <Button
          variant="outline"
          size="md"
          full
          onClick={() => {
            window.location.href = '/';
          }}
        >
          Перейти на главную
        </Button>
      </div>
    </div>
  );
};
