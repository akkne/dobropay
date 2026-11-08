import React from 'react';
import { Button } from '../ui/Button';

export interface SbpBankDeeplinkButtonProps {
  deeplink?: string;
  onClick?: () => void;
}

export const SbpBankDeeplinkButton: React.FC<SbpBankDeeplinkButtonProps> = ({ deeplink, onClick }) => {
  return (
    <Button
      variant="primary-blue"
      size="lg"
      full
      onClick={() => {
        if (onClick) {
          onClick();
        } else if (deeplink) {
          window.location.href = deeplink;
        }
      }}
    >
      <svg className="w-5 h-5 mr-1" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
        <rect x="5" y="2" width="14" height="20" rx="2" ry="2" />
        <line x1="12" y1="18" x2="12.01" y2="18" />
      </svg>
      <span>Оплатить через СБП</span>
    </Button>
  );
};
