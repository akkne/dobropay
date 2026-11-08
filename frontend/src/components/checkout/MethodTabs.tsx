import React from 'react';
import { PaymentMethodType } from '../../types/PaymentMethodType';

interface MethodTabsProps {
  activeMethod: PaymentMethodType;
  onChange: (method: PaymentMethodType) => void;
}

export const MethodTabs: React.FC<MethodTabsProps> = ({ activeMethod, onChange }) => {
  return (
    <nav className="method-tabs" aria-label="Способы оплаты">
      <button
        type="button"
        className={`method-tab ${activeMethod === 'sbp' ? 'active' : ''}`}
        onClick={() => onChange('sbp')}
      >
        <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
          <rect x="3" y="3" width="7" height="7" />
          <rect x="14" y="3" width="7" height="7" />
          <rect x="3" y="14" width="7" height="7" />
          <rect x="14" y="14" width="7" height="7" />
        </svg>
        <span>СБП (QR)</span>
      </button>

      <button
        type="button"
        className={`method-tab ${activeMethod === 'bank_card' ? 'active' : ''}`}
        onClick={() => onChange('bank_card')}
      >
        <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
          <rect x="2" y="5" width="20" height="14" rx="2" />
          <line x1="2" y1="10" x2="22" y2="10" />
        </svg>
        <span>Карта</span>
      </button>

      <button
        type="button"
        className={`method-tab ${activeMethod === 'crypto' ? 'active' : ''}`}
        onClick={() => onChange('crypto')}
      >
        <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
          <circle cx="12" cy="12" r="10" />
          <path d="M9.5 9h5a2.5 2.5 0 0 1 0 5H9.5" />
          <path d="M9.5 14h5.5a2.5 2.5 0 0 1 0 5H9.5" />
          <line x1="12" y1="6" x2="12" y2="9" />
          <line x1="12" y1="19" x2="12" y2="22" />
        </svg>
        <span>Крипта</span>
      </button>
    </nav>
  );
};
