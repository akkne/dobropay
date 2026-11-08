import React from 'react';
import { OrderShopInfo } from './OrderShopInfo';
import { OrderAmountBadge } from './OrderAmountBadge';

export interface OrderSummaryProps {
  shopName: string;
  orderId: string;
  amount: number;
}

export const OrderSummary: React.FC<OrderSummaryProps> = ({
  shopName,
  orderId,
  amount
}) => {
  return (
    <div className="bg-white border border-dp-line rounded-card p-5 shadow-sm flex items-center justify-between">
      <OrderShopInfo shopName={shopName} />
      <OrderAmountBadge amount={amount} orderId={orderId} />
    </div>
  );
};
