import React from 'react';
import { Button } from '../ui/Button';

export interface PaymentsTableToolbarProps {
  searchQuery: string;
  statusFilter: string;
  methodFilter: string;
  onSearchChange: (query: string) => void;
  onStatusChange: (status: string) => void;
  onMethodChange: (method: string) => void;
  onCreatePayment: () => void;
}

export const PaymentsTableToolbar: React.FC<PaymentsTableToolbarProps> = ({
  searchQuery,
  statusFilter,
  methodFilter,
  onSearchChange,
  onStatusChange,
  onMethodChange,
  onCreatePayment
}) => {
  return (
    <div className="flex flex-col md:flex-row items-stretch md:items-center justify-between gap-3 pb-2">
      <div className="flex flex-wrap items-center gap-2.5 flex-1">
        <div className="relative flex-1 min-w-[200px] max-w-sm">
          <input
            type="text"
            placeholder="Поиск по заказу или ID..."
            value={searchQuery}
            onChange={(e) => onSearchChange(e.target.value)}
            className="w-full pl-9 pr-3 py-2 text-xs rounded-pill border border-dp-line bg-white focus:outline-none focus:border-dp-blue transition-colors"
          />
          <svg className="w-4 h-4 text-dp-blue/40 absolute left-3 top-1/2 -translate-y-1/2" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
            <circle cx="11" cy="11" r="8" />
            <line x1="21" y1="21" x2="16.65" y2="16.65" />
          </svg>
        </div>

        <select
          value={statusFilter}
          onChange={(e) => onStatusChange(e.target.value)}
          className="px-3 py-2 text-xs rounded-pill border border-dp-line bg-white text-dp-blue font-medium focus:outline-none focus:border-dp-blue"
        >
          <option value="all">Все статусы</option>
          <option value="succeeded">Успешные</option>
          <option value="waiting_for_capture">В холде</option>
          <option value="pending">Ожидающие</option>
          <option value="canceled">Отмененные</option>
        </select>

        <select
          value={methodFilter}
          onChange={(e) => onMethodChange(e.target.value)}
          className="px-3 py-2 text-xs rounded-pill border border-dp-line bg-white text-dp-blue font-medium focus:outline-none focus:border-dp-blue"
        >
          <option value="all">Все методы</option>
          <option value="sbp">СБП</option>
          <option value="bank_card">Карта</option>
        </select>
      </div>

      <Button
        variant="primary-blue"
        size="sm"
        onClick={onCreatePayment}
      >
        <svg className="w-3.5 h-3.5 mr-1" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5">
          <line x1="12" y1="5" x2="12" y2="19" />
          <line x1="5" y1="12" x2="19" y2="12" />
        </svg>
        <span>Создать платёж</span>
      </Button>
    </div>
  );
};
