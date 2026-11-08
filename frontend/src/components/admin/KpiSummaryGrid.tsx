import React from 'react';
import { Payment } from '../../types/Payment';
import { formatMoney } from '../../utils/formatters';

interface KpiSummaryGridProps {
  payments: Payment[];
}

export const KpiSummaryGrid: React.FC<KpiSummaryGridProps> = ({ payments }) => {
  let todayVolume = 0;
  let succeededCount = 0;
  let holdAmount = 0;
  let refundAmount = 0;

  for (const p of payments) {
    if (p.status === 'succeeded') {
      todayVolume += p.capturedAmount || p.amount;
      succeededCount++;
    } else if (p.status === 'waiting_for_capture') {
      holdAmount += p.amount;
    }
    if (p.refundedAmount) {
      refundAmount += p.refundedAmount;
    }
  }

  return (
    <div className="kpi-grid">
      <div className="kpi-card">
        <div className="kpi-title">Оборот за сегодня</div>
        <div className="kpi-value">{formatMoney(todayVolume)}</div>
        <div className="kpi-meta">↑ +14.2% к прошлому дню</div>
      </div>

      <div className="kpi-card">
        <div className="kpi-title">Успешных оплат</div>
        <div className="kpi-value">{succeededCount}</div>
        <div className="kpi-meta">Конверсия 98.4%</div>
      </div>

      <div className="kpi-card">
        <div className="kpi-title">В холде (Ожидает)</div>
        <div className="kpi-value">{formatMoney(holdAmount)}</div>
        <div className="kpi-meta" style={{ color: 'var(--amber)' }}>
          Двухстадийные платежи
        </div>
      </div>

      <div className="kpi-card">
        <div className="kpi-title">Сумма возвратов</div>
        <div className="kpi-value">{formatMoney(refundAmount)}</div>
        <div className="kpi-meta" style={{ color: 'rgba(9, 49, 141, 0.65)' }}>
          Частичные и полные
        </div>
      </div>
    </div>
  );
};
