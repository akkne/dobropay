import React from 'react';
import { PaymentStatus } from '../../types/PaymentStatus';
import { OutcomeStatusHero } from './OutcomeStatusHero';
import { OutcomeActions } from './OutcomeActions';

export interface OutcomeScreenProps {
  status: PaymentStatus;
  amount: number;
  onViewReceipt: () => void;
  onRetry: () => void;
  onReturnToShop: () => void;
}

export const OutcomeScreen: React.FC<OutcomeScreenProps> = ({
  status,
  amount,
  onViewReceipt,
  onRetry,
  onReturnToShop
}) => {
  return (
    <div className="flex flex-col items-center space-y-6 py-4">
      <OutcomeStatusHero status={status} amount={amount} />
      <OutcomeActions
        status={status}
        onViewReceipt={onViewReceipt}
        onRetry={onRetry}
        onReturnToShop={onReturnToShop}
      />
    </div>
  );
};
