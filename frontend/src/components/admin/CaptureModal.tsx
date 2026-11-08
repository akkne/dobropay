import React, { useState, useEffect } from 'react';
import { Payment } from '../../types/Payment';
import { formatMoney } from '../../utils/formatters';
import { Modal } from '../ui/Modal';
import { Button } from '../ui/Button';

interface CaptureModalProps {
  payment: Payment | null;
  isOpen: boolean;
  onClose: () => void;
  onConfirm: (amountKopecks: number) => void;
}

export const CaptureModal: React.FC<CaptureModalProps> = ({
  payment,
  isOpen,
  onClose,
  onConfirm
}) => {
  const [rubles, setRubles] = useState<string>('');

  useEffect(() => {
    if (payment) {
      setRubles((payment.amount / 100).toFixed(2));
    }
  }, [payment]);

  if (!payment) return null;

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    const val = parseFloat(rubles);
    const kopecks = Math.round(val * 100);
    if (!isNaN(kopecks) && kopecks > 0 && kopecks <= payment.amount) {
      onConfirm(kopecks);
    }
  };

  return (
    <Modal isOpen={isOpen} onClose={onClose}>
      <h3 style={{ fontSize: '20px', fontWeight: 700, color: 'var(--blue)', marginBottom: '6px' }}>
        Подтверждение списания (Capture)
      </h3>
      <p style={{ fontSize: '14px', color: 'rgba(9, 49, 141, 0.7)', marginBottom: '18px' }}>
        Заказ №{payment.externalOrderId || payment.id.substring(0, 8)}
      </p>

      <form onSubmit={handleSubmit}>
        <div className="form-group">
          <label className="form-label" htmlFor="capture-amount">
            Сумма списания (₽)
          </label>
          <input
            type="number"
            id="capture-amount"
            className="form-input mono"
            step="0.01"
            min="0.01"
            max={(payment.amount / 100).toFixed(2)}
            value={rubles}
            onChange={(e) => setRubles(e.target.value)}
            required
          />
          <span className="form-hint">
            Полная сумма заказа: {formatMoney(payment.amount)}
          </span>
        </div>

        <p style={{ fontSize: '13px', color: 'rgba(9, 49, 141, 0.7)', marginBottom: '18px' }}>
          Вы можете списать полную или частичную сумму. Оставшаяся сумма холда будет автоматически
          разблокирована на счёте покупателя.
        </p>

        <Button type="submit" variant="primary" size="lg" full>
          Списать средства
        </Button>
      </form>
    </Modal>
  );
};
