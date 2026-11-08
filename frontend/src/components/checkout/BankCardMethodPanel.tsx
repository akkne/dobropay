import React, { useState } from 'react';
import { formatCardNumber, formatExpiryDate, formatMoney } from '../../utils/formatters';
import { detectCardBrand } from '../../utils/validation';
import { Button } from '../ui/Button';

interface BankCardMethodPanelProps {
  amount: number;
  onSubmit: (data: { cardNumber: string; expiry: string; cvv: string; saveCard: boolean }) => void;
}

export const BankCardMethodPanel: React.FC<BankCardMethodPanelProps> = ({ amount, onSubmit }) => {
  const [cardNumber, setCardNumber] = useState('');
  const [expiry, setExpiry] = useState('');
  const [cvv, setCvv] = useState('');
  const [saveCard, setSaveCard] = useState(true);

  const brandInfo = detectCardBrand(cardNumber);

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    onSubmit({ cardNumber, expiry, cvv, saveCard });
  };

  return (
    <section className="method-panel active" aria-labelledby="tab-card">
      <form onSubmit={handleSubmit}>
        <div className="form-group">
          <label className="form-label" htmlFor="card-number">Номер карты</label>
          <div className="card-input-wrapper">
            <input
              type="text"
              id="card-number"
              className="form-input mono"
              placeholder="2200 0000 0000 0000"
              maxLength={23}
              value={cardNumber}
              onChange={(e) => setCardNumber(formatCardNumber(e.target.value))}
              required
            />
            <div className="card-brand-icon">
              {brandInfo.brand === 'mir' && (
                <span className="badge neutral" style={{ fontWeight: 700, color: '#009949' }}>МИР</span>
              )}
              {brandInfo.brand === 'visa' && (
                <span className="badge neutral" style={{ fontWeight: 700, color: '#1A1F71' }}>VISA</span>
              )}
              {brandInfo.brand === 'mastercard' && (
                <span className="badge neutral" style={{ fontWeight: 700, color: '#EB001B' }}>MC</span>
              )}
              {brandInfo.brand === 'generic' && (
                <span className="badge neutral" style={{ fontSize: '11px' }}>КАРТА</span>
              )}
            </div>
          </div>
        </div>

        <div className="card-grid-row">
          <div className="form-group">
            <label className="form-label" htmlFor="card-expiry">Срок действия</label>
            <input
              type="text"
              id="card-expiry"
              className="form-input mono"
              placeholder="ММ/ГГ"
              maxLength={5}
              value={expiry}
              onChange={(e) => setExpiry(formatExpiryDate(e.target.value))}
              required
            />
          </div>

          <div className="form-group">
            <label className="form-label" htmlFor="card-cvv">Код CVC / CVV</label>
            <input
              type="password"
              id="card-cvv"
              className="form-input mono"
              placeholder="•••"
              maxLength={4}
              value={cvv}
              onChange={(e) => setCvv(e.target.value.replace(/\D/g, '').substring(0, 4))}
              required
            />
          </div>
        </div>

        <div className="form-group" style={{ marginTop: '4px', marginBottom: '22px' }}>
          <label className="toggle-row">
            <input
              type="checkbox"
              className="toggle-checkbox"
              checked={saveCard}
              onChange={(e) => setSaveCard(e.target.checked)}
            />
            <span>Сохранить карту для быстрых покупок в один клик</span>
          </label>
        </div>

        <Button type="submit" variant="primary" size="lg" full>
          Оплатить {formatMoney(amount)}
        </Button>

        <div className="card-secure-notice">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
            <rect x="3" y="11" width="18" height="11" rx="2" ry="2" />
            <path d="M7 11V7a5 5 0 0 1 10 0v4" />
          </svg>
          <span>Безопасная оплата с поддержкой 3-D Secure и шифрованием TLS</span>
        </div>
      </form>
    </section>
  );
};
