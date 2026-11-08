import React from 'react';
import { Brand } from './Brand';
import { formatMoney, formatDateTime } from '../../utils/formatters';

interface ReceiptProps {
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
    <div className="receipt-preview">
      <div className="receipt-head">
        <Brand size={20} />
        <span className="receipt-status-badge">Оплачено</span>
      </div>

      <div className="receipt-title">Чек · Заказ №{orderNumber}</div>

      <dl className="receipt-dl">
        <div className="receipt-dl-row">
          <dt>Организация</dt>
          <dd>{legalEntity}</dd>
        </div>
        <div className="receipt-dl-row">
          <dt>ИНН</dt>
          <dd className="mono">{inn}</dd>
        </div>
        <div className="receipt-dl-row">
          <dt>Способ</dt>
          <dd>{paymentMethodTitle}</dd>
        </div>
        <div className="receipt-dl-row">
          <dt>Дата</dt>
          <dd className="mono">{formatDateTime(createdAt)}</dd>
        </div>
        <div className="receipt-dl-row">
          <dt>Фискальный накопитель (ФН)</dt>
          <dd className="mono">{fnNumber}</dd>
        </div>
        <div className="receipt-dl-row">
          <dt>ФД / ФПД</dt>
          <dd className="mono">{fdNumber} / {fpdNumber}</dd>
        </div>
      </dl>

      <div className="receipt-total-row">
        <span style={{ fontSize: '15px', fontWeight: 550, color: 'rgba(9, 49, 141, 0.7)' }}>
          Итого
        </span>
        <strong>{formatMoney(amount)}</strong>
      </div>

      <div className="receipt-demo-note">Электронный кассовый чек по 54-ФЗ</div>
    </div>
  );
};
