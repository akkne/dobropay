import React from 'react';
import { FiscalReceipt } from '../../types/FiscalReceipt';
import { formatMoney } from '../../utils/formatters';
import { Badge } from '../ui/Badge';
import { Button } from '../ui/Button';

interface ReceiptsTableProps {
  receipts: FiscalReceipt[];
  onPreviewReceipt: (receipt: FiscalReceipt) => void;
}

export const ReceiptsTable: React.FC<ReceiptsTableProps> = ({
  receipts,
  onPreviewReceipt
}) => {
  return (
    <div className="section-card">
      <div className="section-head">
        <div>
          <h2 className="section-title">Реестр фискальных чеков (54-ФЗ)</h2>
          <p style={{ fontSize: '13px', color: 'rgba(9, 49, 141, 0.65)', marginTop: '4px' }}>
            Автоматическая передача данных в ФНС через облачные кассы и отправка квитанций покупателям
          </p>
        </div>
      </div>

      <div className="table-responsive">
        <table className="data-table">
          <thead>
            <tr>
              <th>Номер заказа / ID</th>
              <th>Реквизиты чека</th>
              <th>Сумма</th>
              <th>Email покупателя</th>
              <th>Фискализация</th>
              <th>Email статус</th>
              <th>Действия</th>
            </tr>
          </thead>
          <tbody>
            {receipts.map((r) => (
              <tr key={r.id}>
                <td>
                  <div style={{ fontWeight: 600, color: 'var(--blue)' }}>Заказ №{r.orderNumber}</div>
                  <div className="mono" style={{ fontSize: '11px', color: 'rgba(9, 49, 141, 0.5)' }}>
                    {r.id}
                  </div>
                </td>
                <td className="mono" style={{ fontSize: '12px' }}>
                  ФД №{r.fdNumber} · ФПД {r.fpdNumber}
                </td>
                <td style={{ fontWeight: 700, color: 'var(--blue)' }}>{formatMoney(r.amount)}</td>
                <td style={{ fontSize: '13px' }}>{r.customerEmail}</td>
                <td>
                  <Badge variant="paid">Фискализирован</Badge>
                </td>
                <td>
                  <Badge variant="succeeded">Отправлен</Badge>
                </td>
                <td className="td-actions">
                  <Button variant="ghost" size="sm" onClick={() => onPreviewReceipt(r)}>
                    Печатная форма
                  </Button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
};
