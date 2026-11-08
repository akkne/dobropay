import React from 'react';
import { formatMoney } from '../../utils/formatters';

interface OrderSummaryProps {
  shopName: string;
  orderId: string;
  amount: number;
}

export const OrderSummary: React.FC<OrderSummaryProps> = ({ shopName, orderId, amount }) => {
  return (
    <section className="order-summary">
      <div className="order-shop-row">
        <span className="order-shop-name">{shopName}</span>
        <span className="order-order-id">Заказ №{orderId}</span>
      </div>
      <div className="order-amount-label">К оплате</div>
      <div className="order-amount-val">{formatMoney(amount)}</div>
    </section>
  );
};
