import React, { useState } from 'react';
import { Button } from '../ui/Button';

export interface ThreeDsSmsChallengeProps {
  onSuccess: () => void;
  onCancel: () => void;
}

export const ThreeDsSmsChallenge: React.FC<ThreeDsSmsChallengeProps> = ({ onSuccess, onCancel }) => {
  const [code, setCode] = useState('');
  const [error, setError] = useState('');

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (code.trim() === '1234' || code.trim().length >= 4) {
      onSuccess();
    } else {
      setError('Введите корректный код подтверждения');
    }
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      <div className="flex items-center gap-3 pb-3 border-b border-dp-line">
        <div className="w-9 h-9 rounded-full bg-dp-blue text-white flex items-center justify-center font-bold text-xs">
          3DS
        </div>
        <div>
          <h3 className="text-sm font-bold text-dp-blue">Подтверждение операции</h3>
          <p className="text-xs text-dp-blue/60">3-D Secure 2.0 · Банк-эмитент</p>
        </div>
      </div>

      <div className="text-xs text-dp-blue/80 space-y-1">
        <p>Мы отправили SMS с одноразовым паролем на номер <strong className="font-mono">+7 (999) •••-••-42</strong>.</p>
      </div>

      <div className="space-y-1">
        <label className="text-xs font-semibold text-dp-blue">Код из SMS</label>
        <input
          type="text"
          inputMode="numeric"
          maxLength={6}
          placeholder="1234"
          value={code}
          onChange={(e) => {
            setCode(e.target.value);
            setError('');
          }}
          className="w-full px-4 py-3 rounded-card border border-dp-line text-center font-mono text-xl tracking-widest bg-white focus:outline-none focus:border-dp-blue focus:ring-1 focus:ring-dp-blue/30 transition-colors"
        />
        {error && <p className="text-xs text-dp-red font-medium">{error}</p>}
      </div>

      <div className="flex items-center gap-2 pt-2">
        <Button type="submit" variant="primary-blue" size="md" full>
          Подтвердить
        </Button>
        <Button type="button" variant="outline" size="md" onClick={onCancel}>
          Отмена
        </Button>
      </div>
    </form>
  );
};
