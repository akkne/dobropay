import React, { useState } from 'react';
import { Payment } from '../../types/Payment';
import { PaymentStatus } from '../../types/PaymentStatus';
import { PaymentMethodType } from '../../types/PaymentMethodType';
import { formatMoney, formatDateTime } from '../../utils/formatters';
import { Badge } from '../ui/Badge';
import { Button } from '../ui/Button';

interface PaymentsTableProps {
  payments: Payment[];
  onOpenCreate: () => void;
  onOpenDetails: (payment: Payment) => void;
  onOpenCapture: (payment: Payment) => void;
  onOpenRefund: (payment: Payment) => void;
}

export const PaymentsTable: React.FC<PaymentsTableProps> = ({
  payments,
  onOpenCreate,
  onOpenDetails,
  onOpenCapture,
  onOpenRefund
}) => {
  const [statusFilter, setStatusFilter] = useState<string>('all');
  const [methodFilter, setMethodFilter] = useState<string>('all');
  const [searchQuery, setSearchQuery] = useState<string>('');

  const filtered = payments.filter((p) => {
    if (statusFilter !== 'all' && p.status !== statusFilter) return false;
    if (methodFilter !== 'all' && p.paymentMethodType !== methodFilter) return false;
    if (searchQuery) {
      const q = searchQuery.toLowerCase();
      const matchOrder = p.externalOrderId?.toLowerCase().includes(q);
      const matchId = p.id.toLowerCase().includes(q);
      if (!matchOrder && !matchId) return false;
    }
    return true;
  });

  const getStatusBadgeVariant = (s: PaymentStatus) => {
    if (s === 'succeeded') return 'succeeded';
    if (s === 'waiting_for_capture') return 'waiting';
    if (s === 'canceled') return 'canceled';
    return 'neutral';
  };

  const getStatusTitle = (s: PaymentStatus) => {
    if (s === 'succeeded') return 'Оплачено';
    if (s === 'waiting_for_capture') return 'В холде (Ожидает)';
    if (s === 'canceled') return 'Отменено';
    return 'Ожидает оплаты';
  };

  const getMethodShortName = (m: PaymentMethodType) => {
    if (m === 'sbp') return 'СБП';
    if (m === 'bank_card') return 'Карта';
    if (m === 'crypto') return 'Крипта';
    return m;
  };

  return (
    <div className="section-card">
      <div className="section-head">
        <h2 className="section-title">Реестр платежей</h2>
        <Button variant="primary" onClick={onOpenCreate}>
          + Создать платёж / Ссылку
        </Button>
      </div>

      <div className="toolbar-row">
        <div className="toolbar-filters">
          <select
            className="filter-select"
            value={statusFilter}
            onChange={(e) => setStatusFilter(e.target.value)}
          >
            <option value="all">Все статусы</option>
            <option value="succeeded">Успешно (Succeeded)</option>
            <option value="waiting_for_capture">В холде (Waiting for Capture)</option>
            <option value="pending">Ожидает (Pending)</option>
            <option value="canceled">Отменено (Canceled)</option>
          </select>

          <select
            className="filter-select"
            value={methodFilter}
            onChange={(e) => setMethodFilter(e.target.value)}
          >
            <option value="all">Все способы оплаты</option>
            <option value="sbp">СБП (QR-код)</option>
            <option value="bank_card">Банковские карты</option>
            <option value="crypto">Криптовалюта</option>
          </select>
        </div>

        <div className="search-input-box">
          <svg className="search-icon" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
            <circle cx="11" cy="11" r="8" />
            <line x1="21" y1="21" x2="16.65" y2="16.65" />
          </svg>
          <input
            type="text"
            className="form-input"
            placeholder="Поиск по номеру заказа или ID..."
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
          />
        </div>
      </div>

      <div className="table-responsive">
        <table className="data-table">
          <thead>
            <tr>
              <th>Заказ / ID</th>
              <th>Дата и время</th>
              <th>Сумма</th>
              <th>Способ</th>
              <th>Статус</th>
              <th>Действия</th>
            </tr>
          </thead>
          <tbody>
            {filtered.length === 0 ? (
              <tr>
                <td colSpan={6} style={{ textAlign: 'center', color: 'rgba(9, 49, 141, 0.5)', padding: '30px' }}>
                  Платежи не найдены
                </td>
              </tr>
            ) : (
              filtered.map((p) => (
                <tr key={p.id}>
                  <td>
                    <div style={{ fontWeight: 600, color: 'var(--blue)' }}>{p.externalOrderId || '—'}</div>
                    <div className="mono" style={{ fontSize: '11px', color: 'rgba(9, 49, 141, 0.5)' }}>
                      {p.id.substring(0, 8)}...
                    </div>
                  </td>
                  <td style={{ fontSize: '13px', color: 'rgba(9, 49, 141, 0.7)' }}>{formatDateTime(p.createdAt)}</td>
                  <td>
                    <div style={{ fontWeight: 700, fontSize: '15px', color: 'var(--blue)' }}>
                      {formatMoney(p.amount)}
                    </div>
                    {p.refundedAmount ? (
                      <div style={{ fontSize: '11px', color: 'var(--red)' }}>
                        Возврат: {formatMoney(p.refundedAmount)}
                      </div>
                    ) : null}
                  </td>
                  <td>
                    <Badge variant="neutral" style={{ fontSize: '11px' }}>
                      {getMethodShortName(p.paymentMethodType)}
                    </Badge>
                  </td>
                  <td>
                    <Badge variant={getStatusBadgeVariant(p.status)}>
                      {getStatusTitle(p.status)}
                    </Badge>
                  </td>
                  <td className="td-actions">
                    <Button variant="ghost" size="sm" onClick={() => onOpenDetails(p)}>
                      Детали
                    </Button>
                    {p.status === 'waiting_for_capture' && (
                      <Button variant="primary" size="sm" onClick={() => onOpenCapture(p)}>
                        Списать
                      </Button>
                    )}
                    {p.status === 'succeeded' && (
                      <Button variant="outline" size="sm" onClick={() => onOpenRefund(p)}>
                        Возврат
                      </Button>
                    )}
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
};
