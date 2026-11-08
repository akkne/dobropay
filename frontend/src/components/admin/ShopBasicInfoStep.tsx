import React, { useState } from 'react';
import { Button } from '../ui/Button';

export interface ShopBasicInfoStepProps {
  onNext: (data: { name: string; url: string; email: string }) => void;
}

export const ShopBasicInfoStep: React.FC<ShopBasicInfoStepProps> = ({ onNext }) => {
  const [name, setName] = useState('Магазин мерча Dobropay');
  const [url, setUrl] = useState('https://store.dobropay.io');
  const [email, setEmail] = useState('finance@dobropay.io');

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    onNext({ name, url, email });
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-4 max-w-md mx-auto">
      <div className="space-y-1">
        <label className="text-xs font-semibold text-dp-blue">Название проекта или магазина</label>
        <input
          type="text"
          required
          value={name}
          onChange={(e) => setName(e.target.value)}
          className="w-full px-3.5 py-2.5 text-xs rounded-card border border-dp-line bg-white focus:outline-none focus:border-dp-blue"
        />
      </div>

      <div className="space-y-1">
        <label className="text-xs font-semibold text-dp-blue">Сайт или мобильное приложение</label>
        <input
          type="url"
          required
          value={url}
          onChange={(e) => setUrl(e.target.value)}
          className="w-full px-3.5 py-2.5 text-xs rounded-card border border-dp-line bg-white focus:outline-none focus:border-dp-blue font-mono"
        />
      </div>

      <div className="space-y-1">
        <label className="text-xs font-semibold text-dp-blue">Финансовый email для реестров</label>
        <input
          type="email"
          required
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          className="w-full px-3.5 py-2.5 text-xs rounded-card border border-dp-line bg-white focus:outline-none focus:border-dp-blue"
        />
      </div>

      <div className="pt-2">
        <Button type="submit" variant="primary-blue" size="md" full>
          Далее: Реквизиты юрлица
        </Button>
      </div>
    </form>
  );
};
