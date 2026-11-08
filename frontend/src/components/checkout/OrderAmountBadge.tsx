import React from 'react';
import { formatMoney } from '../../utils/formatters';

export interface OrderAmountBadgeProps {
  amount: number;
  orderId?: string;
}

export const OrderAmountBadge: React.FC<OrderAmountBadgeProps> = ({ amount, orderId }) => {
  return (
    <div className="text-right">
      <div className="text-2xl font-bold font-mono tracking-tight text-dp-blue">
        {formatMoney(amount)}
      </div>
      {orderId && (
        <div className="text-xs text-dp-blue/60 font-mono mt-0.5">
          Заказ #{orderId}
        </div>
      )}
    </div>
  );
};
