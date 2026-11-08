import React, { useState } from 'react';
import { WebhookDelivery } from '../../types/WebhookDelivery';
import { formatDateTime } from '../../utils/formatters';
import { Badge } from '../ui/Badge';
import { Button } from '../ui/Button';

interface WebhooksSectionProps {
  deliveries: WebhookDelivery[];
  initialUrl: string;
  initialSecret: string;
  initialEvents: string[];
  onSaveConfig: (url: string, signingSecret: string, events: string[]) => void;
  onInspectDelivery: (delivery: WebhookDelivery) => void;
  onRetryDelivery: (deliveryId: string) => void;
}

export const WebhooksSection: React.FC<WebhooksSectionProps> = ({
  deliveries,
  initialUrl,
  initialSecret,
  initialEvents,
  onSaveConfig,
  onInspectDelivery,
  onRetryDelivery
}) => {
  const [url, setUrl] = useState(initialUrl);
  const [signingSecret, setSigningSecret] = useState(initialSecret);
  const [events, setEvents] = useState<string[]>(initialEvents);

  const toggleEvent = (ev: string) => {
    setEvents((prev) =>
      prev.includes(ev) ? prev.filter((e) => e !== ev) : [...prev, ev]
    );
  };

  const handleSave = (e: React.FormEvent) => {
    e.preventDefault();
    onSaveConfig(url, signingSecret, events);
  };

  return (
    <div className="section-card">
      <div className="section-head">
        <div>
          <h2 className="section-title">Исходящие Webhook уведомления</h2>
          <p style={{ fontSize: '13px', color: 'rgba(9, 49, 141, 0.65)', marginTop: '4px' }}>
            Уведомления о смене статусов платежей и возвратов с криптографической подписью HMAC-SHA256
          </p>
        </div>
      </div>

      <form onSubmit={handleSave}>
        <div className="form-group">
          <label className="form-label" htmlFor="webhook-url">
            URL обработчика (Endpoint на вашем сервере)
          </label>
          <input
            type="url"
            id="webhook-url"
            className="form-input mono"
            placeholder="https://example.com/api/dobropay-webhook"
            value={url}
            onChange={(e) => setUrl(e.target.value)}
            required
          />
        </div>

        <div className="form-group">
          <label className="form-label" htmlFor="webhook-secret">
            Секретный ключ подписи (Signing Secret)
          </label>
          <input
            type="text"
            id="webhook-secret"
            className="form-input mono"
            placeholder="dp_sec_..."
            value={signingSecret}
            onChange={(e) => setSigningSecret(e.target.value)}
            required
          />
          <span className="form-hint">
            Используется для проверки заголовка <code>X-Dobropay-Signature: sha256=...</code>
          </span>
        </div>

        <div className="form-group">
          <label className="form-label">События для отправки:</label>
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(2, 1fr)', gap: '10px', marginTop: '6px' }}>
            {[
              { id: 'payment.succeeded', label: 'payment.succeeded — успешная оплата' },
              { id: 'payment.waiting_for_capture', label: 'payment.waiting_for_capture — холд средств' },
              { id: 'payment.canceled', label: 'payment.canceled — отмена платежа' },
              { id: 'refund.succeeded', label: 'refund.succeeded — успешный возврат' }
            ].map((ev) => (
              <label key={ev.id} className="toggle-row">
                <input
                  type="checkbox"
                  className="toggle-checkbox"
                  checked={events.includes(ev.id)}
                  onChange={() => toggleEvent(ev.id)}
                />
                <span><code>{ev.label}</code></span>
              </label>
            ))}
          </div>
        </div>

        <Button type="submit" variant="primary-blue" style={{ marginTop: '10px' }}>
          Сохранить настройки Webhook
        </Button>
      </form>

      <h3 style={{ fontSize: '16px', fontWeight: 700, color: 'var(--blue)', marginTop: '18px' }}>
        Журнал доставок Webhook
      </h3>
      <div className="table-responsive">
        <table className="data-table">
          <thead>
            <tr>
              <th>Тип события</th>
              <th>URL назначения</th>
              <th>Статус ответа</th>
              <th>Попыток</th>
              <th>Дата отправки</th>
              <th>Действия</th>
            </tr>
          </thead>
          <tbody>
            {deliveries.map((d) => (
              <tr key={d.id}>
                <td>
                  <span className="mono" style={{ fontWeight: 600, color: 'var(--blue)' }}>
                    {d.eventType}
                  </span>
                </td>
                <td className="mono" style={{ fontSize: '12px', color: 'rgba(9, 49, 141, 0.7)', maxWidth: '220px', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                  {d.url}
                </td>
                <td>
                  <Badge variant={d.statusCode === 200 ? 'succeeded' : 'waiting'}>
                    HTTP {d.statusCode}
                  </Badge>
                </td>
                <td style={{ textAlign: 'center', fontWeight: 600 }}>{d.attempts}</td>
                <td style={{ fontSize: '12px', color: 'rgba(9, 49, 141, 0.65)' }}>
                  {formatDateTime(d.createdAt)}
                </td>
                <td className="td-actions">
                  <Button variant="ghost" size="sm" onClick={() => onInspectDelivery(d)}>
                    Инспектор
                  </Button>
                  <Button variant="outline" size="sm" onClick={() => onRetryDelivery(d.id)}>
                    Повторить
                  </Button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
};
