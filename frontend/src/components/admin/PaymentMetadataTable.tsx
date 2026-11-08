import React from 'react';
import { Payment } from '../../types/Payment';

export interface PaymentMetadataTableProps {
  payment: Payment;
}

export const PaymentMetadataTable: React.FC<PaymentMetadataTableProps> = ({ payment }) => {
  return (
    <div className="space-y-3">
      <h4 className="text-xs font-bold text-dp-blue uppercase tracking-wider">
        Технические параметры
      </h4>
      <div className="bg-dp-tint rounded-card p-3.5 space-y-2 text-xs border border-dp-line">
        <div className="flex justify-between items-center py-1 border-b border-dp-line/50">
          <span className="text-dp-blue/60">ID платежа</span>
          <span className="font-mono font-medium text-right select-all">{payment.id}</span>
        </div>
        <div className="flex justify-between items-center py-1 border-b border-dp-line/50">
          <span className="text-dp-blue/60">ID магазина</span>
          <span className="font-mono text-right select-all">{payment.shopId}</span>
        </div>
        <div className="flex justify-between items-center py-1 border-b border-dp-line/50">
          <span className="text-dp-blue/60">Двухстадийный (Холд)</span>
          <span className="font-medium text-right">{payment.capture ? 'Да (Авторизация)' : 'Нет (Мгновенный)'}</span>
        </div>
        {payment.checkoutUrl && (
          <div className="flex justify-between items-center py-1">
            <span className="text-dp-blue/60">Ссылка на оплату</span>
            <a
              href={payment.checkoutUrl}
              target="_blank"
              rel="noopener noreferrer"
              className="text-xs font-semibold text-blue-600 hover:underline truncate max-w-[200px]"
            >
              {payment.checkoutUrl}
            </a>
          </div>
        )}
      </div>
    </div>
  );
};
