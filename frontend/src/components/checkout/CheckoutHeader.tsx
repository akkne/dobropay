import React from 'react';
import { CheckoutBrand } from './CheckoutBrand';
import { CheckoutTimerBadge } from './CheckoutTimerBadge';

export interface CheckoutHeaderProps {
  remainingSeconds: number;
}

export const CheckoutHeader: React.FC<CheckoutHeaderProps> = ({ remainingSeconds }) => {
  return (
    <header className="flex items-center justify-between px-1 py-1">
      <CheckoutBrand />
      <CheckoutTimerBadge remainingSeconds={remainingSeconds} />
    </header>
  );
};
