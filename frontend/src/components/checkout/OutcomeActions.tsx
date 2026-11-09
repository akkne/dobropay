import React from 'react';
import { PaymentStatus } from '../../types/PaymentStatus';
import { Button } from '../ui/Button';

export interface OutcomeActionsProps {
  status: PaymentStatus;
  onViewReceipt: () => void;
  onRetry: () => void;
  onReturnToShop: () => void;
}

export const OutcomeActions: React.FC<OutcomeActionsProps> = ({
  status,
  onViewReceipt,
  onRetry,
  onReturnToShop
}) => {
  return (
    <div className="w-full space-y-2.5 pt-2">
      {status === 'succeeded' && (
        <Button
          type="button"
          variant="primary-blue"
          size="lg"
          full
          onClick={onViewReceipt}
        >
          <svg className="w-4 h-4 mr-1" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
            <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z" />
            <polyline points="14 2 14 8 20 8" />
            <line x1="16" y1="13" x2="8" y2="13" />
            <line x1="16" y1="17" x2="8" y2="17" />
            <polyline points="10 9 9 9 8 9" />
          </svg>
          <span>Электронный чек</span>
        </Button>
      )}

      {status === 'canceled' && (
        <Button
          type="button"
          variant="primary-blue"
          size="lg"
          full
          onClick={onRetry}
        >
          <span>Попробовать снова</span>
        </Button>
      )}

      <Button
        type="button"
        variant="outline"
        size="md"
        full
        onClick={onReturnToShop}
      >
        <span>Вернуться в магазин</span>
      </Button>
    </div>
  );
};
