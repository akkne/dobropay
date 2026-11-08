import React, { useState, useEffect } from 'react';
import { Payment } from '../../types/Payment';
import { formatMoney } from '../../utils/formatters';
import { Modal } from '../ui/Modal';
import { Button } from '../ui/Button';

export interface RefundModalProps {
  payment: Payment | null;
  isOpen: boolean;
  onClose: () => void;
  onConfirm: (amountKopecks: number, reason: string) => void;
}

export const RefundModal: React.FC<RefundModalProps> = ({
  payment,
  isOpen,
  onClose,
  onConfirm
}) => {
  const [rubles, setRubles] = useState<string>('');
  const [reason, setReason] = useState<string>('Возврат товара покупателем');

  const remaining = payment ? payment.amount - (payment.refundedAmount || 0) : 0;

  useEffect(() => {
    if (payment) {
      setRubles((remaining / 100).toFixed(2));
    }
  }, [payment, remaining]);

  if (!payment) return null;

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    const val = parseFloat(rubles);
    const kopecks = Math.round(val * 100);
    if (!isNaN(kopecks) && kopecks > 0 && kopecks <= remaining) {
      onConfirm(kopecks, reason);
    }
  };

  return (
    <Modal isOpen={isOpen} onClose={onClose} maxWidth="420px">
      <h3 className="text-base font-bold text-dp-blue mb-1">
        Оформление возврата средств (Refund)
      </h3>
      <p className="text-xs text-dp-blue/60 mb-4">
        Заказ #{payment.orderId || payment.externalOrderId}
      </p>

      <form onSubmit={handleSubmit} className="space-y-4">
        <div className="space-y-1">
          <label className="text-xs font-semibold text-dp-blue" htmlFor="refund-amount">
            Сумма возврата (рубли)
          </label>
          <input
            type="number"
            id="refund-amount"
            step="0.01"
            min="1"
            max={remaining / 100}
            className="w-full px-3.5 py-2.5 text-xs rounded-card border border-dp-line bg-white font-mono focus:outline-none focus:border-dp-blue"
            value={rubles}
            onChange={(e) => setRubles(e.target.value)}
            required
          />
          <p className="text-[11px] text-dp-blue/60">
            Доступный остаток для возврата: <strong className="font-mono">{formatMoney(remaining)}</strong>
          </p>
        </div>

        <div className="space-y-1">
          <label className="text-xs font-semibold text-dp-blue" htmlFor="refund-reason">
            Причина возврата
          </label>
          <input
            type="text"
            id="refund-reason"
            className="w-full px-3.5 py-2.5 text-xs rounded-card border border-dp-line bg-white focus:outline-none focus:border-dp-blue"
            value={reason}
            onChange={(e) => setReason(e.target.value)}
            required
          />
        </div>

        <div className="flex gap-2 pt-2">
          <Button type="button" variant="outline" size="sm" full onClick={onClose}>
            Отмена
          </Button>
          <Button type="submit" variant="primary-blue" size="sm" full>
            Подтвердить возврат
          </Button>
        </div>
      </form>
    </Modal>
  );
};
