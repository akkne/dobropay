import React, { useState, useEffect } from 'react';
import { Payment } from '../../types/Payment';
import { formatMoney } from '../../utils/formatters';
import { Modal } from '../ui/Modal';
import { Button } from '../ui/Button';

interface RefundModalProps {
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
    <Modal isOpen={isOpen} onClose={onClose}>
      <h3 style={{ fontSize: '20px', fontWeight: 700, color: 'var(--blue)', marginBottom: '6px' }}>
        Оформление возврата средств (Refund)
      </h3>
      <p style={{ fontSize: '14px', color: 'rgba(9, 49, 141, 0.7)', marginBottom: '18px' }}>
        Заказ №{payment.externalOrderId || payment.id.substring(0, 8)}
      </p>

      <form onSubmit={handleSubmit}>
        <div className="form-group">
          <label className="form-label" htmlFor="refund-amount">
            Сумма возврата (₽)
          </label>
          <input
            type="number"
            id="refund-amount"
            className="form-input mono"
            step="0.01"
            min="0.01"
            max={(remaining / 100).toFixed(2)}
            value={rubles}
            onChange={(e) => setRubles(e.target.value)}
            required
          />
          <span className="form-hint">
            Доступно для возврата: {formatMoney(remaining)}
          </span>
        </div>

        <div className="form-group">
          <label className="form-label" htmlFor="refund-reason">
            Причина возврата
          </label>
          <input
            type="text"
            id="refund-reason"
            className="form-input"
            value={reason}
            onChange={(e) => setReason(e.target.value)}
            required
          />
        </div>

        <Button type="submit" variant="primary" size="lg" full>
          Подтвердить возврат средств
        </Button>
      </form>
    </Modal>
  );
};
