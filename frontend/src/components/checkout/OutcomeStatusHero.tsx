import React from 'react';
import { PaymentStatus } from '../../types/PaymentStatus';
import { formatMoney } from '../../utils/formatters';

export interface OutcomeStatusHeroProps {
  status: PaymentStatus;
  amount: number;
}

export const OutcomeStatusHero: React.FC<OutcomeStatusHeroProps> = ({ status, amount }) => {
  if (status === 'succeeded') {
    return (
      <div className="flex flex-col items-center text-center space-y-3">
        {/* Pink circle with blue checkmark from DESIGN.md */}
        <div className="w-16 h-16 rounded-full bg-dp-pink flex items-center justify-center text-dp-blue shadow-card">
          <svg className="w-8 h-8" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="3" strokeLinecap="round" strokeLinejoin="round">
            <polyline points="20 6 9 17 4 12" />
          </svg>
        </div>
        <h2 className="text-xl font-bold text-dp-blue">Оплата прошла успешно!</h2>
        <div className="text-2xl font-bold font-mono text-dp-blue tracking-tight">
          {formatMoney(amount)}
        </div>
        <p className="text-xs text-dp-blue/60 max-w-[300px]">
          Средства успешно списаны. Электронный чек доступен для просмотра.
        </p>
      </div>
    );
  }

  if (status === 'waiting_for_capture') {
    return (
      <div className="flex flex-col items-center text-center space-y-3">
        <div className="w-16 h-16 rounded-full bg-dp-amber-blush text-dp-amber border-2 border-amber-500/40 flex items-center justify-center">
          <svg className="w-8 h-8" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5">
            <circle cx="12" cy="12" r="10" />
            <polyline points="12 6 12 12 16 14" />
          </svg>
        </div>
        <h2 className="text-xl font-bold text-dp-blue">Средства захолдированы</h2>
        <div className="text-2xl font-bold font-mono text-dp-blue tracking-tight">
          {formatMoney(amount)}
        </div>
        <p className="text-xs text-dp-blue/60 max-w-[300px]">
          Двухстадийная авторизация: сумма заблокирована на вашем счете до подтверждения продавцом.
        </p>
      </div>
    );
  }

  return (
    <div className="flex flex-col items-center text-center space-y-3">
      <div className="w-16 h-16 rounded-full bg-dp-red-blush text-dp-red border-2 border-red-500/40 flex items-center justify-center">
        <svg className="w-8 h-8" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5">
          <line x1="18" y1="6" x2="6" y2="18" />
          <line x1="6" y1="6" x2="18" y2="18" />
        </svg>
      </div>
      <h2 className="text-xl font-bold text-dp-blue">Оплата отменена</h2>
      <div className="text-2xl font-bold font-mono text-dp-blue tracking-tight">
        {formatMoney(amount)}
      </div>
      <p className="text-xs text-dp-blue/60 max-w-[300px]">
        Транзакция была отменена или отклонена банком-эмитентом. Деньги не списаны.
      </p>
    </div>
  );
};
