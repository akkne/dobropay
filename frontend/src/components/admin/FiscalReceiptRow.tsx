import React from 'react';
import { FiscalReceipt } from '../../types/FiscalReceipt';
import { formatMoney, formatDateTime } from '../../utils/formatters';

export interface FiscalReceiptRowProps {
  receipt: FiscalReceipt;
  onPreview: (receipt: FiscalReceipt) => void;
}

export const FiscalReceiptRow: React.FC<FiscalReceiptRowProps> = ({ receipt, onPreview }) => {
  return (
    <tr className="hover:bg-dp-tint/60 transition-colors">
      <td className="py-2.5 px-3">
        <div className="font-bold font-mono text-dp-blue">Чек #{receipt.fdNumber || receipt.id}</div>
        <div className="text-[10px] text-dp-blue/50">Заказ #{receipt.orderNumber}</div>
      </td>
      <td className="py-2.5 px-3 font-mono font-bold text-dp-blue">
        {formatMoney(receipt.amount)}
      </td>
      <td className="py-2.5 px-3 font-mono text-[11px] text-dp-blue/70">
        {receipt.fnNumber}
      </td>
      <td className="py-2.5 px-3 font-mono text-[11px] text-dp-blue/70">
        {receipt.fdNumber} / {receipt.fpdNumber}
      </td>
      <td className="py-2.5 px-3">
        <span className={`inline-block px-2 py-0.5 rounded text-[10px] font-bold ${receipt.fiscalStatus === 'succeeded' ? 'bg-dp-green-blush text-dp-green' : 'bg-dp-amber-blush text-dp-amber'}`}>
          {receipt.fiscalStatus === 'succeeded' ? 'Фискализирован' : receipt.fiscalStatus}
        </span>
      </td>
      <td className="py-2.5 px-3 font-mono text-dp-blue/70">
        {formatDateTime(receipt.createdAt)}
      </td>
      <td className="py-2.5 px-3 text-right">
        {receipt.fiscalStatus === 'succeeded' && <button
          type="button"
          onClick={() => onPreview(receipt)}
          className="px-2.5 py-1 text-[11px] font-semibold bg-dp-tint text-dp-blue rounded-pill hover:bg-dp-blush transition-colors"
        >
          Печатная форма
        </button>}
      </td>
    </tr>
  );
};
