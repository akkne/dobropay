import React from 'react';
import { FiscalReceipt } from '../../types/FiscalReceipt';
import { FiscalReceiptRow } from './FiscalReceiptRow';

export interface ReceiptsTableProps {
  receipts: FiscalReceipt[];
  onPreviewReceipt: (receipt: FiscalReceipt) => void;
}

export const ReceiptsTable: React.FC<ReceiptsTableProps> = ({ receipts, onPreviewReceipt }) => {
  return (
    <div className="bg-white border border-dp-line rounded-card p-5 shadow-sm space-y-4">
      <div className="pb-3 border-b border-dp-line">
        <h3 className="text-sm font-bold text-dp-blue">Реестр фискальных чеков</h3>
        <p className="text-xs text-dp-blue/60">
          Все чеки автоматически фискализируются через облачную кассу Добропей и регистрируются в ОФД / ФНС
        </p>
      </div>

      <div className="overflow-x-auto">
        <table className="w-full text-left text-xs border-collapse">
          <thead>
            <tr className="border-b border-dp-line text-dp-blue/60 font-semibold uppercase tracking-wider text-[10px]">
              <th className="py-2.5 px-3">Номер чека / Заказ</th>
              <th className="py-2.5 px-3">Сумма</th>
              <th className="py-2.5 px-3">ФН</th>
              <th className="py-2.5 px-3">ФД / ФПД</th>
              <th className="py-2.5 px-3">Статус</th>
              <th className="py-2.5 px-3">Дата регистрации</th>
              <th className="py-2.5 px-3 text-right">Действие</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-dp-line/50">
            {receipts.map((r) => (
              <FiscalReceiptRow
                key={r.id}
                receipt={r}
                onPreview={onPreviewReceipt}
              />
            ))}
          </tbody>
        </table>

        {receipts.length === 0 && (
          <p className="text-xs text-dp-blue/50 text-center py-8">
            Нет зарегистрированных чеков.
          </p>
        )}
      </div>
    </div>
  );
};
