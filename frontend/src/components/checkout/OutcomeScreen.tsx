import React from 'react';
import { PaymentStatus } from '../../types/PaymentStatus';
import { formatMoney, formatDateTime } from '../../utils/formatters';
import { Button } from '../ui/Button';
import { Badge } from '../ui/Badge';

interface OutcomeScreenProps {
  status: PaymentStatus;
  token: string;
  amount: number;
  methodName: string;
  onOpenReceipt: () => void;
  onRetry: () => void;
}

export const OutcomeScreen: React.FC<OutcomeScreenProps> = ({
  status,
  token,
  amount,
  methodName,
  onOpenReceipt,
  onRetry
}) => {
  if (status === 'succeeded') {
    return (
      <div className="checkout-status-screen">
        <div className="status-icon-circle success">
          <svg
            width="34"
            height="34"
            viewBox="0 0 24 24"
            fill="none"
            stroke="var(--blue)"
            strokeWidth="2.6"
            strokeLinecap="round"
            strokeLinejoin="round"
          >
            <polyline points="20 6 9 17 4 12" />
          </svg>
        </div>
        <h2 className="status-title">Оплата прошла успешно!</h2>
        <p className="status-description">
          Средства зачислены магазину. Чек сформирован и отправлен вам на email.
        </p>

        <div className="status-details-box">
          <div className="status-row">
            <span>Номер операции:</span>
            <span className="mono">tx-{token.substring(0, 10)}</span>
          </div>
          <div className="status-row">
            <span>Способ оплаты:</span>
            <span>{methodName}</span>
          </div>
          <div className="status-row">
            <span>Сумма:</span>
            <span>{formatMoney(amount)}</span>
          </div>
          <div className="status-row">
            <span>Дата и время:</span>
            <span>{formatDateTime(new Date().toISOString())}</span>
          </div>
        </div>

        <div className="status-actions">
          <Button variant="primary" size="lg" full onClick={onOpenReceipt}>
            Посмотреть электронный чек
          </Button>
          <a href="/" className="button outline full">
            Вернуться в магазин
          </a>
        </div>
      </div>
    );
  }

  if (status === 'waiting_for_capture') {
    return (
      <div className="checkout-status-screen">
        <div className="status-icon-circle" style={{ background: 'var(--amber-blush)', color: 'var(--amber)' }}>
          <svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5">
            <circle cx="12" cy="12" r="10" />
            <polyline points="12 6 12 12 16 14" />
          </svg>
        </div>
        <h2 className="status-title">Средства захолдированы</h2>
        <p className="status-description">
          Сумма заморожена на карте. Магазин подтвердит списание после сборки заказа.
        </p>

        <div className="status-details-box">
          <div className="status-row">
            <span>Статус:</span>
            <Badge variant="waiting">Ожидает списания (Hold)</Badge>
          </div>
          <div className="status-row">
            <span>Замороженная сумма:</span>
            <span>{formatMoney(amount)}</span>
          </div>
          <div className="status-row">
            <span>Время холда:</span>
            <span>{formatDateTime(new Date().toISOString())}</span>
          </div>
        </div>

        <div className="status-actions">
          <a href="/" className="button primary full lg">
            Вернуться в магазин
          </a>
        </div>
      </div>
    );
  }

  // Canceled
  return (
    <div className="checkout-status-screen">
      <div className="status-icon-circle canceled">
        <svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.6">
          <line x1="18" y1="6" x2="6" y2="18" />
          <line x1="6" y1="6" x2="18" y2="18" />
        </svg>
      </div>
      <h2 className="status-title">Оплата не завершена</h2>
      <p className="status-description">Операция была отклонена банком или отменена покупателем.</p>

      <div className="status-actions">
        <Button variant="primary" size="lg" full onClick={onRetry}>
          Попробовать снова
        </Button>
        <a href="/" className="button outline full">
          Отменить и вернуться
        </a>
      </div>
    </div>
  );
};
