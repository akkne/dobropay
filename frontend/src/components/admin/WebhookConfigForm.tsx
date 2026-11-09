import React, { useState } from 'react';
import { Button } from '../ui/Button';

export interface WebhookConfigFormProps {
  initialUrl?: string;
  initialSecret?: string;
  onSave: (url: string, secret: string, events: string[]) => void;
}

export const WebhookConfigForm: React.FC<WebhookConfigFormProps> = ({
  initialUrl = 'https://dobroplay.online/webhooks/payment',
  initialSecret = 'dp_sec_99a8b7c6d5e4f3a2b10987654321fedc',
  onSave
}) => {
  const [url, setUrl] = useState(initialUrl);
  const [secret, setSecret] = useState(initialSecret);
  const [events, setEvents] = useState<string[]>([
    'payment.succeeded',
    'payment.waiting_for_capture',
    'payment.canceled',
    'refund.succeeded'
  ]);

  const toggleEvent = (ev: string) => {
    setEvents((prev) => (prev.includes(ev) ? prev.filter((e) => e !== ev) : [...prev, ev]));
  };

  const handleGenerateSecret = () => {
    const random = Array.from({ length: 32 }, () => Math.floor(Math.random() * 16).toString(16)).join('');
    setSecret(`dp_sec_${random}`);
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    onSave(url, secret, events);
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      <div className="space-y-1">
        <label className="text-xs font-semibold text-dp-blue">Webhook URL (HTTPS endpoint)</label>
        <input
          type="url"
          required
          placeholder="https://your-domain.com/webhooks/dobropay"
          value={url}
          onChange={(e) => setUrl(e.target.value)}
          className="w-full px-3.5 py-2.5 text-xs rounded-card border border-dp-line bg-white font-mono focus:outline-none focus:border-dp-blue"
        />
      </div>

      <div className="space-y-1">
        <div className="flex items-center justify-between">
          <label className="text-xs font-semibold text-dp-blue">HMAC-SHA256 Signing Secret</label>
          <button
            type="button"
            onClick={handleGenerateSecret}
            className="text-[11px] text-blue-600 hover:underline cursor-pointer"
          >
            Сгенерировать новый
          </button>
        </div>
        <input
          type="text"
          readOnly
          value={secret}
          className="w-full px-3.5 py-2.5 text-xs rounded-card border border-dp-line bg-dp-tint font-mono select-all"
        />
        <p className="text-[11px] text-dp-blue/60">
          Используется для проверки подписи в заголовке <code>X-Dobropay-Signature: sha256=...</code>
        </p>
      </div>

      <div className="space-y-2">
        <label className="text-xs font-semibold text-dp-blue">Подписка на события:</label>
        <div className="grid grid-cols-2 gap-2 text-xs">
          {[
            { id: 'payment.succeeded', label: 'Оплата успешна' },
            { id: 'payment.waiting_for_capture', label: 'Авторизация холда' },
            { id: 'payment.canceled', label: 'Отмена платежа' },
            { id: 'refund.succeeded', label: 'Успешный возврат' }
          ].map((item) => (
            <label key={item.id} className="flex items-center gap-2 cursor-pointer text-dp-blue">
              <input
                type="checkbox"
                checked={events.includes(item.id)}
                onChange={() => toggleEvent(item.id)}
                className="w-4 h-4 rounded text-dp-blue border-dp-line focus:ring-dp-pink"
              />
              <span>{item.label}</span>
            </label>
          ))}
        </div>
      </div>

      <div className="pt-2">
        <Button type="submit" variant="primary-blue" size="sm">
          Сохранить конфигурацию Webhook
        </Button>
      </div>
    </form>
  );
};
