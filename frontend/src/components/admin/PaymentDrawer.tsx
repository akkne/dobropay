import React from 'react';
import { Payment } from '../../types/Payment';
import { formatMoney } from '../../utils/formatters';
import { PaymentStatusBadge } from './PaymentStatusBadge';
import { PaymentMethodBadge } from './PaymentMethodBadge';
import { PaymentTimeline } from './PaymentTimeline';
import { PaymentMetadataTable } from './PaymentMetadataTable';
import { Button } from '../ui/Button';

export interface PaymentDrawerProps {
  isOpen: boolean;
  payment: Payment | null;
  onClose: () => void;
  onCapture: (payment: Payment) => void;
  onCancel: (payment: Payment) => void;
  onRefund: (payment: Payment) => void;
}

export const PaymentDrawer: React.FC<PaymentDrawerProps> = ({
  isOpen,
  payment,
  onClose,
  onCapture,
  onCancel,
  onRefund
}) => {
  if (!isOpen || !payment) return null;

  return (
    <div className="fixed inset-0 z-50 overflow-hidden" onClick={onClose}>
      <div className="absolute inset-0 bg-black/30 backdrop-blur-xs transition-opacity" />

      <div
        className="fixed inset-y-0 right-0 max-w-full flex pl-10"
        onClick={(e) => e.stopPropagation()}
      >
        <div className="w-screen max-w-md bg-white border-l border-dp-line shadow-popover p-6 flex flex-col justify-between overflow-y-auto">
          <div className="space-y-6">
            <div className="flex items-center justify-between pb-4 border-b border-dp-line">
              <div>
                <h3 className="text-base font-bold text-dp-blue">
                  Детали платежа #{payment.orderId || payment.externalOrderId}
                </h3>
                <p className="text-xs text-dp-blue/60">{payment.description || 'Оплата заказа'}</p>
              </div>
              <button
                type="button"
                onClick={onClose}
                className="w-8 h-8 rounded-full flex items-center justify-center text-xl text-dp-blue/60 hover:text-dp-blue hover:bg-dp-tint transition-colors"
              >
                &times;
              </button>
            </div>

            <div className="bg-dp-tint rounded-card p-4 flex items-center justify-between">
              <div>
                <div className="text-xs text-dp-blue/60">Сумма платежа</div>
                <div className="text-2xl font-bold font-mono text-dp-blue">
                  {formatMoney(payment.amount)}
                </div>
              </div>
              <div className="space-y-1 text-right">
                <PaymentStatusBadge status={payment.status} />
                <div>
                  <PaymentMethodBadge method={payment.paymentMethodType} />
                </div>
              </div>
            </div>

            <PaymentTimeline payment={payment} />
            <PaymentMetadataTable payment={payment} />
          </div>

          <div className="pt-6 border-t border-dp-line flex gap-2">
            {payment.status === 'waiting_for_capture' && (
              <>
                <Button
                  variant="primary-blue"
                  size="md"
                  full
                  onClick={() => onCapture(payment)}
                >
                  Списать холд
                </Button>
                <Button
                  variant="outline"
                  size="md"
                  onClick={() => onCancel(payment)}
                >
                  Отменить
                </Button>
              </>
            )}
            {payment.status === 'succeeded' && (
              <Button
                variant="outline"
                size="md"
                full
                onClick={() => onRefund(payment)}
              >
                Оформить возврат
              </Button>
            )}
          </div>
        </div>
      </div>
    </div>
  );
};
