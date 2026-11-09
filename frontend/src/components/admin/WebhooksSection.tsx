import React from 'react';
import { WebhookDelivery } from '../../types/WebhookDelivery';
import { WebhookConfigForm } from './WebhookConfigForm';
import { WebhookDeliveryRow } from './WebhookDeliveryRow';

export interface WebhooksSectionProps {
  deliveries: WebhookDelivery[];
  onSaveConfig: (url: string, secret: string, events: string[]) => void;
  onInspectDelivery: (delivery: WebhookDelivery) => void;
  onRetryDelivery: (deliveryId: string) => void;
}

export const WebhooksSection: React.FC<WebhooksSectionProps> = ({
  deliveries,
  onSaveConfig,
  onInspectDelivery,
  onRetryDelivery
}) => {
  return (
    <div className="space-y-6">
      <div className="bg-white border border-dp-line rounded-card p-5 shadow-sm space-y-4">
        <div className="pb-3 border-b border-dp-line">
          <h3 className="text-sm font-bold text-dp-blue">Настройка Webhook-уведомлений</h3>
          <p className="text-xs text-dp-blue/60">
            Мгновенная доставка статусов платежей на ваш сервер с проверкой криптографической подписи
          </p>
        </div>
        <WebhookConfigForm onSave={onSaveConfig} />
      </div>

      <div className="bg-white border border-dp-line rounded-card p-5 shadow-sm space-y-4">
        <div className="pb-3 border-b border-dp-line">
          <h3 className="text-sm font-bold text-dp-blue">Журнал доставок Webhook</h3>
          <p className="text-xs text-dp-blue/60">
            История HTTP-отправок, HTTP-коды ответов и повторы при ошибках
          </p>
        </div>

        <div className="admin-table-scroll overflow-x-auto">
          <table className="admin-data-table w-full text-left text-xs border-collapse">
            <thead>
              <tr className="border-b border-dp-line text-dp-blue/60 font-semibold uppercase tracking-wider text-[10px]">
                <th className="py-2.5 px-3">Статус</th>
                <th className="py-2.5 px-3">Событие</th>
                <th className="py-2.5 px-3">URL</th>
                <th className="py-2.5 px-3 text-center">Попытки</th>
                <th className="py-2.5 px-3">Время отправки</th>
                <th className="py-2.5 px-3 text-right">Действие</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-dp-line/50">
              {deliveries.map((d) => (
                <WebhookDeliveryRow
                  key={d.id}
                  delivery={d}
                  onInspect={onInspectDelivery}
                  onRetry={onRetryDelivery}
                />
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
};
