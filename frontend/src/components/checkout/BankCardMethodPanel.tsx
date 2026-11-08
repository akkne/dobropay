import React, { useState } from 'react';
import { CardNumberInput } from './CardNumberInput';
import { CardExpiryCvvFields } from './CardExpiryCvvFields';
import { CardSaveCheckbox } from './CardSaveCheckbox';
import { Button } from '../ui/Button';
import { formatMoney } from '../../utils/formatters';

export interface BankCardMethodPanelProps {
  amount: number;
  onSubmit: (cardData: { number: string; expiry: string; cvv: string; saveCard: boolean }) => void;
}

export const BankCardMethodPanel: React.FC<BankCardMethodPanelProps> = ({ amount, onSubmit }) => {
  const [cardNumber, setCardNumber] = useState('');
  const [expiry, setExpiry] = useState('');
  const [cvv, setCvv] = useState('');
  const [saveCard, setSaveCard] = useState(false);

  const isFormValid =
    cardNumber.replace(/\s/g, '').length >= 16 &&
    expiry.length === 5 &&
    cvv.length >= 3;

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!isFormValid) return;
    onSubmit({ number: cardNumber, expiry, cvv, saveCard });
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-4 py-2">
      <CardNumberInput value={cardNumber} onChange={setCardNumber} />
      <CardExpiryCvvFields
        expiry={expiry}
        cvv={cvv}
        onExpiryChange={setExpiry}
        onCvvChange={setCvv}
      />
      <CardSaveCheckbox checked={saveCard} onChange={setSaveCard} />

      <div className="pt-2">
        <Button
          type="submit"
          variant="primary-blue"
          size="lg"
          full
          disabled={!isFormValid}
        >
          <span>Оплатить {formatMoney(amount)}</span>
        </Button>
      </div>

      <div className="flex items-center justify-center gap-1.5 text-[11px] text-dp-blue/60 text-center">
        <svg className="w-3.5 h-3.5 text-dp-green flex-shrink-0" viewBox="0 0 24 24" fill="currentColor">
          <path d="M12 1L3 5v6c0 5.55 3.84 10.74 9 12 5.16-1.26 9-6.45 9-12V5l-9-4zm-2 16l-4-4 1.41-1.41L10 14.17l6.59-6.59L18 9l-8 8z" />
        </svg>
        <span>Платежные данные надежно защищены стандартом PCI DSS Level 1</span>
      </div>
    </form>
  );
};
