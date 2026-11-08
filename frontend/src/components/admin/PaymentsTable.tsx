import React, { useState } from 'react';
import { Payment } from '../../types/Payment';
import { formatMoney, formatDateTime } from '../../utils/formatters';
import { PaymentStatusBadge } from './PaymentStatusBadge';
import { PaymentMethodBadge } from './PaymentMethodBadge';
import { PaymentsTableToolbar } from './PaymentsTableToolbar';
import { PaymentsTableEmptyState } from './PaymentsTableEmptyState';

export interface PaymentsTableProps {
  payments: Payment[];
  onSelectPayment: (payment: Payment) => void;
  onCapture: (payment: Payment) => void;
  onCancel: (payment: Payment) => void;
  onRefund: (payment: Payment) => void;
  onCreatePayment: () => void;
}

export const PaymentsTable: React.FC<PaymentsTableProps> = ({
  payments,
  onSelectPayment,
  onCapture,
  onCancel,
  onRefund,
  onCreatePayment
}) => {
  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState('all');
  const [methodFilter, setMethodFilter] = useState('all');

  const filtered = payments.filter((p) => {
    if (statusFilter !== 'all' && p.status !== statusFilter) return false;
    if (methodFilter !== 'all' && p.paymentMethodType !== methodFilter) return false;
    if (search.trim()) {
      const q = search.toLowerCase();
      const orderNumber = p.orderId || p.externalOrderId || '';
      return (
        p.id.toLowerCase().includes(q) ||
        orderNumber.toLowerCase().includes(q) ||
        (p.description ? p.description.toLowerCase().includes(q) : false)
      );
    }
    return true;
  });

  return (
    <div className="bg-white border border-dp-line rounded-card p-5 shadow-sm space-y-4">
      <PaymentsTableToolbar
        searchQuery={search}
        statusFilter={statusFilter}
        methodFilter={methodFilter}
        onSearchChange={setSearch}
        onStatusChange={setStatusFilter}
        onMethodChange={setMethodFilter}
        onCreatePayment={onCreatePayment}
      />

      <div className="overflow-x-auto">
        <table className="w-full text-left text-xs border-collapse">
          <thead>
            <tr className="border-b border-dp-line text-dp-blue/60 font-semibold uppercase tracking-wider text-[10px]">
              <th className="py-3 px-3">Заказ / ID</th>
              <th className="py-3 px-3">Сумма</th>
              <th className="py-3 px-3">Статус</th>
              <th className="py-3 px-3">Метод</th>
              <th className="py-3 px-3">Дата создания</th>
              <th className="py-3 px-3 text-right">Действия</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-dp-line/50">
            {filtered.map((payment) => (
              <tr
                key={payment.id}
                onClick={() => onSelectPayment(payment)}
                className="hover:bg-dp-tint/60 transition-colors cursor-pointer group"
              >
                <td className="py-3 px-3">
                  <div className="font-bold text-dp-blue group-hover:text-blue-700">
                    Заказ #{payment.orderId || payment.externalOrderId}
                  </div>
                  <div className="font-mono text-[10px] text-dp-blue/50 truncate max-w-[140px]">
                    {payment.id}
                  </div>
                </td>
                <td className="py-3 px-3">
                  <div className="font-bold font-mono text-sm text-dp-blue">
                    {formatMoney(payment.amount)}
                  </div>
                  {payment.refundedAmount && payment.refundedAmount > 0 ? (
                    <div className="text-[10px] text-dp-red font-medium">
                      Возврат: -{formatMoney(payment.refundedAmount)}
                    </div>
                  ) : null}
                </td>
                <td className="py-3 px-3">
                  <PaymentStatusBadge status={payment.status} />
                </td>
                <td className="py-3 px-3">
                  <PaymentMethodBadge method={payment.paymentMethodType} />
                </td>
                <td className="py-3 px-3 font-mono text-dp-blue/70">
                  {formatDateTime(payment.createdAt)}
                </td>
                <td className="py-3 px-3 text-right" onClick={(e) => e.stopPropagation()}>
                  <div className="flex items-center justify-end gap-1.5">
                    {payment.status === 'waiting_for_capture' && (
                      <>
                        <button
                          type="button"
                          onClick={() => onCapture(payment)}
                          className="px-2.5 py-1 text-[11px] font-semibold bg-dp-green-blush text-dp-green rounded-pill hover:bg-emerald-100 transition-colors"
                        >
                          Списать
                        </button>
                        <button
                          type="button"
                          onClick={() => onCancel(payment)}
                          className="px-2.5 py-1 text-[11px] font-semibold bg-dp-red-blush text-dp-red rounded-pill hover:bg-red-100 transition-colors"
                        >
                          Отменить
                        </button>
                      </>
                    )}
                    {payment.status === 'succeeded' && (
                      <button
                        type="button"
                        onClick={() => onRefund(payment)}
                        className="px-2.5 py-1 text-[11px] font-semibold bg-dp-tint text-dp-blue rounded-pill hover:bg-dp-blush transition-colors"
                      >
                        Возврат
                      </button>
                    )}
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>

        {filtered.length === 0 && <PaymentsTableEmptyState />}
      </div>
    </div>
  );
};
