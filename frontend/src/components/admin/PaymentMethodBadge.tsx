import React from 'react';
import { PaymentMethodType } from '../../types/PaymentMethodType';

export interface PaymentMethodBadgeProps {
  method: PaymentMethodType;
}

export const PaymentMethodBadge: React.FC<PaymentMethodBadgeProps> = ({ method }) => {
  const config = {
    sbp: { label: 'СБП', color: 'bg-dp-pink/20 text-dp-blue border-dp-pink/40' },
    bank_card: { label: 'Карта', color: 'bg-dp-blue/10 text-dp-blue border-dp-blue/20' }
  }[method] || { label: method, color: 'bg-dp-tint text-dp-blue border-dp-line' };

  return (
    <span className={`inline-flex items-center px-2 py-0.5 rounded text-[11px] font-semibold border ${config.color}`}>
      {config.label}
    </span>
  );
};
