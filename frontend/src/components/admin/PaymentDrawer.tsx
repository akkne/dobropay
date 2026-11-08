import React from 'react';
import Link from 'next/link';
import { Payment } from '../../types/Payment';
import { formatMoney, formatDateTime } from '../../utils/formatters';
import { Badge } from '../ui/Badge';
import { Button } from '../ui/Button';

interface PaymentDrawerProps {
  payment: Payment | null;
  onClose: () => void;
  onCapture: (payment: Payment) => void;
  onCancel: (payment: Payment) => void;
  onRefund: (payment: Payment) => void;
}

export const PaymentDrawer: React.FC<PaymentDrawerProps> = ({
  payment,
  onClose,
  onCapture,
  onCancel,
  onRefund
}) => {
  if (!payment) return null;

  const remaining = payment.amount - (payment.refundedAmount || 0);

  return (
    <div className={`drawer-overlay active`} onClick={onClose}>
      <div className="drawer-panel" onClick={(e) => e.stopPropagation()}>
        <div className="drawer-head">
          <div>
            <h3 style={{ fontSize: '18px', fontWeight: 700, color: 'var(--blue)' }}>Детали платежа</h3>
            <div className="mono" style={{ fontSize: '13px', color: 'rgba(9, 49, 141, 0.6)' }}>
              {payment.externalOrderId || payment.id}
            </div>
          </div>
          <button type="button" className="modal-close" onClick={onClose}>
            &times;
          </button>
        </div>

        <div className="drawer-body">
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            <span style={{ fontSize: '14px', color: 'rgba(9, 49, 141, 0.7)' }}>Статус:</span>
            <Badge variant={payment.status === 'succeeded' ? 'succeeded' : payment.status === 'waiting_for_capture' ? 'waiting' : 'neutral'}>
              {payment.status}
            </Badge>
          </div>

          <div style={{ background: 'var(--tint)', borderRadius: 'var(--radius-sm)', padding: '16px', display: 'flex', flexDirection: 'column', gap: '10px' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between' }}>
              <span style={{ fontSize: '13px', color: 'rgba(9, 49, 141, 0.65)' }}>Сумма платежа:</span>
              <strong style={{ fontSize: '18px', color: 'var(--blue)' }}>{formatMoney(payment.amount)}</strong>
            </div>
            <div style={{ display: 'flex', justifyContent: 'space-between' }}>
              <span style={{ fontSize: '13px', color: 'rgba(9, 49, 141, 0.65)' }}>Доступный остаток:</span>
              <span className="mono" style={{ fontWeight: 600, color: 'var(--blue)' }}>
                {formatMoney(remaining)}
              </span>
            </div>
            <div style={{ display: 'flex', justifyContent: 'space-between' }}>
              <span style={{ fontSize: '13px', color: 'rgba(9, 49, 141, 0.65)' }}>Способ оплаты:</span>
              <span style={{ fontWeight: 600 }}>{payment.paymentMethodType}</span>
            </div>
            <div style={{ display: 'flex', justifyContent: 'space-between' }}>
              <span style={{ fontSize: '13px', color: 'rgba(9, 49, 141, 0.65)' }}>Дата создания:</span>
              <span className="mono" style={{ fontSize: '12px' }}>{formatDateTime(payment.createdAt)}</span>
            </div>
            <div style={{ display: 'flex', justifyContent: 'space-between' }}>
              <span style={{ fontSize: '13px', color: 'rgba(9, 49, 141, 0.65)' }}>ID транзакции:</span>
              <span className="mono" style={{ fontSize: '11px' }}>{payment.id}</span>
            </div>
          </div>

          <div>
            <h4 style={{ fontSize: '14px', fontWeight: 700, color: 'var(--blue)', marginBottom: '12px' }}>
              Хронология событий:
            </h4>
            <div className="timeline">
              <div className="timeline-item">
                <strong style={{ fontSize: '13px' }}>Платёж зарегистрирован</strong>
                <span style={{ fontSize: '11px', color: 'rgba(9, 49, 141, 0.5)' }}>
                  Получен запрос POST /v1/payments
                </span>
              </div>
              <div className="timeline-item">
                <strong style={{ fontSize: '13px' }}>Создана попытка оплаты</strong>
                <span style={{ fontSize: '11px', color: 'rgba(9, 49, 141, 0.5)' }}>
                  Генерация реквизитов провайдера
                </span>
              </div>
              <div className="timeline-item">
                <strong style={{ fontSize: '13px' }}>Событие провайдера обработано</strong>
                <span style={{ fontSize: '11px', color: 'rgba(9, 49, 141, 0.5)' }}>
                  Состояние обновлено, опубликован Kafka event
                </span>
              </div>
              <div className="timeline-item">
                <strong style={{ fontSize: '13px' }}>Webhook отправлен мерчанту</strong>
                <span style={{ fontSize: '11px', color: 'var(--green)' }}>HTTP 200 OK</span>
              </div>
            </div>
          </div>
        </div>

        <div className="drawer-footer">
          {payment.status === 'waiting_for_capture' && (
            <>
              <Button variant="primary" style={{ flex: 1 }} onClick={() => onCapture(payment)}>
                Подтвердить списание (Capture)
              </Button>
              <Button variant="outline" style={{ flex: 1 }} onClick={() => onCancel(payment)}>
                Отменить холд (Cancel)
              </Button>
            </>
          )}

          {payment.status === 'succeeded' && remaining > 0 && (
            <>
              <Button variant="primary" style={{ flex: 1 }} onClick={() => onRefund(payment)}>
                Оформить возврат (Refund)
              </Button>
              <Link
                href={`/checkout/${payment.id}?status=succeeded`}
                target="_blank"
                className="button outline"
                style={{ flex: 1, textAlign: 'center' }}
              >
                Открыть чек
              </Link>
            </>
          )}

          {payment.status !== 'waiting_for_capture' && (payment.status !== 'succeeded' || remaining <= 0) && (
            <Button variant="outline" full onClick={onClose}>
              Закрыть
            </Button>
          )}
        </div>
      </div>
    </div>
  );
};
