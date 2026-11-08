import React from 'react';
import { Payment } from '../../types/Payment';
import { formatMoney } from '../../utils/formatters';
import { KpiStatCard } from './KpiStatCard';

export interface KpiSummaryGridProps {
  payments: Payment[];
}

export const KpiSummaryGrid: React.FC<KpiSummaryGridProps> = ({ payments }) => {
  const succeededPayments = payments.filter((p) => p.status === 'succeeded');
  const holdPayments = payments.filter((p) => p.status === 'waiting_for_capture');

  const totalTurnover = succeededPayments.reduce((acc, p) => acc + (p.capturedAmount || p.amount), 0);
  const totalHold = holdPayments.reduce((acc, p) => acc + p.amount, 0);
  const totalRefunds = payments.reduce((acc, p) => acc + (p.refundedAmount || 0), 0);
  const successfulCount = succeededPayments.length;

  return (
    <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
      <KpiStatCard
        title="Оборот за сегодня"
        value={formatMoney(totalTurnover)}
        subtitle="Успешные списания"
        trend="+14.2%"
        variant="blue"
      />
      <KpiStatCard
        title="Успешных оплат"
        value={`${successfulCount} шт`}
        subtitle="Конверсия 96.4%"
        variant="green"
      />
      <KpiStatCard
        title="В холде (авторизовано)"
        value={formatMoney(totalHold)}
        subtitle="Ожидают списания"
        variant="amber"
      />
      <KpiStatCard
        title="Сумма возвратов"
        value={formatMoney(totalRefunds)}
        subtitle="По запросам мерчанта"
        variant="pink"
      />
    </div>
  );
};
