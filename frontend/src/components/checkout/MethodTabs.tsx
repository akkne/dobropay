import React from 'react';
import { PaymentMethodType } from '../../types/PaymentMethodType';
import { MethodTabButton } from './MethodTabButton';

export interface MethodTabsProps {
  activeMethod: PaymentMethodType;
  onChange: (method: PaymentMethodType) => void;
}

export const MethodTabs: React.FC<MethodTabsProps> = ({ activeMethod, onChange }) => {
  const tabs: Array<{ type: PaymentMethodType; label: string; badge?: string }> = [
    { type: 'sbp', label: 'СБП', badge: '0% комиссия' },
    { type: 'bank_card', label: 'Банковская карта' },
  ];

  return (
    <div className="bg-dp-tint p-1.5 rounded-pill flex gap-1 border border-dp-line">
      {tabs.map((tab) => (
        <MethodTabButton
          key={tab.type}
          type={tab.type}
          label={tab.label}
          badge={tab.badge}
          isActive={activeMethod === tab.type}
          onClick={onChange}
        />
      ))}
    </div>
  );
};
