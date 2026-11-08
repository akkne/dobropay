import React, { useState, useEffect } from 'react';
import { Payment } from '../../types/Payment';
import { formatMoney } from '../../utils/formatters';
import { Modal } from '../ui/Modal';
import { Button } from '../ui/Button';

export interface CaptureModalProps {
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
    <Modal isOpen={isOpen} onClose={onClose} maxWidth="420px">
      <h3 className="text-base font-bold text-dp-blue mb-1">
        Подтверждение списания холда (Capture)
      </h3>
      <p className="text-xs text-dp-blue/60 mb-4">
        Заказ #{payment.orderId || payment.externalOrderId}
      </p>

      <form onSubmit={handleSubmit} className="space-y-4">
        <div className="space-y-1">
          <label className="text-xs font-semibold text-dp-blue" htmlFor="capture-amount">
            Сумма списания (рубли)
          </label>
          <input
            type="number"
            id="capture-amount"
            step="0.01"
            min="1"
            max={payment.amount / 100}
            className="w-full px-3.5 py-2.5 text-xs rounded-card border border-dp-line bg-white font-mono focus:outline-none focus:border-dp-blue"
            value={rubles}
            onChange={(e) => setRubles(e.target.value)}
            required
          />
          <p className="text-[11px] text-dp-blue/60">
            Максимально доступно для списания: <strong className="font-mono">{formatMoney(payment.amount)}</strong>
          </p>
        </div>

        <div className="flex gap-2 pt-2">
          <Button type="button" variant="outline" size="sm" full onClick={onClose}>
            Отмена
          </Button>
          <Button type="submit" variant="primary-blue" size="sm" full>
            Подтвердить списание
          </Button>
        </div>
      </form>
    </Modal>
  );
};
