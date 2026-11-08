import React, { useState } from 'react';
import { Modal } from '../ui/Modal';
import { Button } from '../ui/Button';
import { PaymentMethodType } from '../../types/PaymentMethodType';

interface CreatePaymentModalProps {
  isOpen: boolean;
  onClose: () => void;
  onCreate: (data: { amountKopecks: number; orderId: string; method: PaymentMethodType; capture: boolean }) => void;
}

export const CreatePaymentModal: React.FC<CreatePaymentModalProps> = ({
  isOpen,
  onClose,
  onCreate
}) => {
  const [rubles, setRubles] = useState<string>('1299.00');
  const [orderId, setOrderId] = useState<string>('');
  const [method, setMethod] = useState<PaymentMethodType>('sbp');
  const [capture, setCapture] = useState<boolean>(true);

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    const val = parseFloat(rubles);
    const kopecks = Math.round(val * 100);
    if (!isNaN(kopecks) && kopecks > 0) {
      onCreate({
        amountKopecks: kopecks,
        orderId: orderId.trim() || `order-${Math.floor(1000 + Math.random() * 9000)}`,
        method,
        capture
      });
    }
  };

  return (
    <Modal isOpen={isOpen} onClose={onClose}>
      <h3 style={{ fontSize: '20px', fontWeight: 700, color: 'var(--blue)', marginBottom: '6px' }}>
        Создать счёт на оплату
      </h3>
      <p style={{ fontSize: '13px', color: 'rgba(9, 49, 141, 0.65)', marginBottom: '18px' }}>
        Генерация ссылки на платёжную форму Hosted Checkout
      </p>

      <form onSubmit={handleSubmit}>
        <div className="form-group">
          <label className="form-label" htmlFor="new-amount">
            Сумма к оплате (в рублях)
          </label>
          <input
            type="number"
            id="new-amount"
            className="form-input"
            placeholder="1299.00"
            step="0.01"
            min="1"
            value={rubles}
            onChange={(e) => setRubles(e.target.value)}
            required
          />
        </div>

        <div className="form-group">
          <label className="form-label" htmlFor="new-order-id">
            Номер заказа магазина (externalOrderId)
          </label>
          <input
            type="text"
            id="new-order-id"
            className="form-input"
            placeholder="order-1045"
            value={orderId}
            onChange={(e) => setOrderId(e.target.value)}
          />
        </div>

        <div className="form-group">
          <label className="form-label" htmlFor="new-method">
            Предпочтительный способ
          </label>
          <select
            id="new-method"
            className="form-select"
            value={method}
            onChange={(e) => setMethod(e.target.value as PaymentMethodType)}
          >
            <option value="sbp">СБП (Система быстрых платежей)</option>
            <option value="bank_card">Банковская карта (Мир, Visa, MC)</option>
            <option value="crypto">Криптовалюта (USDT, TON, BTC)</option>
          </select>
        </div>

        <div className="form-group" style={{ marginTop: '4px', marginBottom: '20px' }}>
          <label className="toggle-row">
            <input
              type="checkbox"
              className="toggle-checkbox"
              checked={capture}
              onChange={(e) => setCapture(e.target.checked)}
            />
            <span>Одностадийный платёж (автосписание без холда)</span>
          </label>
        </div>

        <Button type="submit" variant="primary" size="lg" full>
          Сгенерировать ссылку на оплату →
        </Button>
      </form>
    </Modal>
  );
};
