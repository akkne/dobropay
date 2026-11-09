import React from 'react';
import { Brand } from './Brand';
import { formatMoney, formatDateTime } from '../../utils/formatters';

export interface ReceiptProps {
  orderNumber: string;
  amount: number;
  paymentMethodTitle?: string;
  fnNumber?: string;
  fdNumber?: string;
  fpdNumber?: string;
  createdAt?: string;
  legalEntity?: string;
  inn?: string;
}

export const Receipt: React.FC<ReceiptProps> = ({
  orderNumber,
  amount,
  paymentMethodTitle = 'СБП',
  fnNumber = '9999078900012345',
  fdNumber = '4021',
  fpdNumber = '3948572019',
  createdAt = new Date().toISOString(),
  legalEntity = 'ООО «Добропей Диджитал»',
  inn = '7707083893'
}) => {
  return (
    <div className="w-full max-w-[370px] mx-auto bg-white border border-dp-line border-t-[5px] border-t-dp-pink rounded-card p-6 shadow-receipt text-dp-blue">
      <div className="flex items-center justify-between pb-4 border-b border-dp-line">
        <Brand size={18} />
        <span className="inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded-pill text-xs font-semibold bg-dp-green-blush text-dp-green border border-dp-green/20">
          Оплачено
        </span>
      </div>

      <div className="py-3 text-center text-sm font-bold tracking-tight border-b border-dashed border-dp-line">
        Электронный чек · Заказ №{orderNumber}
      </div>

      <dl className="py-3 space-y-2 text-xs">
        <div className="flex items-center justify-between border-b border-dashed border-dp-line/60 pb-1.5">
          <dt className="text-dp-blue/70">Организация</dt>
          <dd className="font-medium text-right">{legalEntity}</dd>
        </div>
        <div className="flex items-center justify-between border-b border-dashed border-dp-line/60 pb-1.5">
          <dt className="text-dp-blue/70">ИНН</dt>
          <dd className="font-mono text-right font-medium">{inn}</dd>
        </div>
        <div className="flex items-center justify-between border-b border-dashed border-dp-line/60 pb-1.5">
          <dt className="text-dp-blue/70">Способ оплаты</dt>
          <dd className="font-medium text-right">{paymentMethodTitle}</dd>
        </div>
        <div className="flex items-center justify-between border-b border-dashed border-dp-line/60 pb-1.5">
          <dt className="text-dp-blue/70">Дата и время</dt>
          <dd className="font-mono text-right font-medium">{formatDateTime(createdAt)}</dd>
        </div>
        <div className="flex items-center justify-between border-b border-dashed border-dp-line/60 pb-1.5">
          <dt className="text-dp-blue/70">ФН</dt>
          <dd className="font-mono text-right font-medium">{fnNumber}</dd>
        </div>
        <div className="flex items-center justify-between border-b border-dashed border-dp-line/60 pb-1.5">
          <dt className="text-dp-blue/70">ФД / ФПД</dt>
          <dd className="font-mono text-right font-medium">{fdNumber} / {fpdNumber}</dd>
        </div>
      </dl>

      <div className="flex items-center justify-between pt-3 border-t-2 border-dp-blue text-base">
        <span className="font-semibold text-dp-blue/80">Итого к оплате</span>
        <strong className="text-xl font-bold font-mono tracking-tight">{formatMoney(amount)}</strong>
      </div>

      <div className="mt-4 pt-3 border-t border-dashed border-dp-line text-[11px] text-center text-dp-blue/60">
        Чек сформирован и доступен в электронном виде.
      </div>
    </div>
  );
};
