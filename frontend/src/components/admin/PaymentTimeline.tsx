import React from 'react';
import { Payment } from '../../types/Payment';
import { formatDateTime, formatMoney } from '../../utils/formatters';

export interface PaymentTimelineProps {
  payment: Payment;
}

export const PaymentTimeline: React.FC<PaymentTimelineProps> = ({ payment }) => {
  return (
    <div className="space-y-3">
      <h4 className="text-xs font-bold text-dp-blue uppercase tracking-wider">
        Хронология событий (Kafka / Webhook)
      </h4>
      <div className="border-l-2 border-dp-line pl-4 space-y-3.5 ml-2 text-xs">
        <div className="relative">
          <div className="absolute -left-[21px] top-1 w-2.5 h-2.5 rounded-full bg-dp-blue" />
          <div className="font-semibold text-dp-blue">Создан платёж</div>
          <div className="font-mono text-[11px] text-dp-blue/60">{formatDateTime(payment.createdAt)}</div>
        </div>

        {payment.status === 'waiting_for_capture' && (
          <div className="relative">
            <div className="absolute -left-[21px] top-1 w-2.5 h-2.5 rounded-full bg-dp-amber" />
            <div className="font-semibold text-dp-amber">Успешная авторизация (Холд)</div>
            <div className="font-mono text-[11px] text-dp-blue/60">{formatDateTime(payment.updatedAt)}</div>
          </div>
        )}

        {payment.status === 'succeeded' && (
          <div className="relative">
            <div className="absolute -left-[21px] top-1 w-2.5 h-2.5 rounded-full bg-dp-green" />
            <div className="font-semibold text-dp-green">Платеж успешно списан (Captured)</div>
            <div className="font-mono text-[11px] text-dp-blue/60">{formatDateTime(payment.updatedAt)}</div>
          </div>
        )}

        {payment.status === 'canceled' && (
          <div className="relative">
            <div className="absolute -left-[21px] top-1 w-2.5 h-2.5 rounded-full bg-dp-red" />
            <div className="font-semibold text-dp-red">Платёж отменён ({payment.cancellationReason || 'По инициативе мерчанта'})</div>
            <div className="font-mono text-[11px] text-dp-blue/60">{formatDateTime(payment.updatedAt)}</div>
          </div>
        )}

        {payment.refunds && payment.refunds.length > 0 && (
          payment.refunds.map((ref) => (
            <div key={ref.id} className="relative">
              <div className="absolute -left-[21px] top-1 w-2.5 h-2.5 rounded-full bg-dp-pink" />
              <div className="font-semibold text-dp-blue">
                Оформлен возврат: <span className="font-mono">{formatMoney(ref.amount)}</span>
              </div>
              <div className="font-mono text-[11px] text-dp-blue/60">{formatDateTime(ref.createdAt)}</div>
            </div>
          ))
        )}
      </div>
    </div>
  );
};
