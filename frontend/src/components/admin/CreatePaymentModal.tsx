import React, { useState } from 'react';
import { Modal } from '../ui/Modal';
import { Button } from '../ui/Button';
import { PaymentMethodType } from '../../types/PaymentMethodType';

export interface CreatePaymentModalProps {
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
    <Modal isOpen={isOpen} onClose={onClose} maxWidth="440px">
      <h3 className="text-base font-bold text-dp-blue mb-1">
        Создать счёт на оплату
      </h3>
      <p className="text-xs text-dp-blue/60 mb-4">
        Генерация ссылки на платёжную форму Hosted Checkout
      </p>

      <form onSubmit={handleSubmit} className="space-y-4">
        <div className="space-y-1">
          <label className="text-xs font-semibold text-dp-blue" htmlFor="new-amount">
            Сумма к оплате (в рублях)
          </label>
          <input
            type="number"
            id="new-amount"
            step="0.01"
            min="1"
            className="w-full px-3.5 py-2.5 text-xs rounded-card border border-dp-line bg-white font-mono focus:outline-none focus:border-dp-blue"
            value={rubles}
            onChange={(e) => setRubles(e.target.value)}
            required
          />
        </div>

        <div className="space-y-1">
          <label className="text-xs font-semibold text-dp-blue" htmlFor="new-order-id">
            Номер заказа в магазине (необязательно)
          </label>
          <input
            type="text"
            id="new-order-id"
            placeholder="Например: 1042"
            className="w-full px-3.5 py-2.5 text-xs rounded-card border border-dp-line bg-white focus:outline-none focus:border-dp-blue"
            value={orderId}
            onChange={(e) => setOrderId(e.target.value)}
          />
        </div>

        <div className="space-y-1">
          <label className="text-xs font-semibold text-dp-blue" htmlFor="new-method">
            Предпочтительный способ оплаты
          </label>
          <select
            id="new-method"
            className="w-full px-3.5 py-2.5 text-xs rounded-card border border-dp-line bg-white text-dp-blue focus:outline-none focus:border-dp-blue"
            value={method}
            onChange={(e) => setMethod(e.target.value as PaymentMethodType)}
          >
            <option value="sbp">Система быстрых платежей (СБП)</option>
            <option value="bank_card">Банковская карта (МИР / Visa / MC)</option>
            <option value="crypto">Криптовалюта (USDT / TON / BTC)</option>
          </select>
        </div>

        <div className="space-y-1 pt-1">
          <label className="flex items-center gap-2 cursor-pointer text-xs text-dp-blue font-medium">
            <input
              type="checkbox"
              checked={capture}
              onChange={(e) => setCapture(e.target.checked)}
              className="w-4 h-4 rounded text-dp-blue border-dp-line focus:ring-dp-pink"
            />
            <span>Мгновенное списание (без холдирования)</span>
          </label>
          <p className="text-[11px] text-dp-blue/50 pl-6">
            Если выключено, средства будут захолдированы (`waiting_for_capture`)
          </p>
        </div>

        <div className="flex gap-2 pt-2">
          <Button type="button" variant="outline" size="sm" full onClick={onClose}>
            Отмена
          </Button>
          <Button type="submit" variant="primary-blue" size="sm" full>
            Создать ссылку
          </Button>
        </div>
      </form>
    </Modal>
  );
};
