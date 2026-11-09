import React from 'react';
import { WebhookDelivery } from '../../types/WebhookDelivery';
import { WebhookDeliveryRow } from './WebhookDeliveryRow';

export interface WebhooksSectionProps {
  deliveries: WebhookDelivery[];
}

export const WebhooksSection: React.FC<WebhooksSectionProps> = ({
  deliveries
}) => {
  return (
    <div className="space-y-6">
      <div className="bg-white border border-dp-line rounded-card p-5 shadow-sm space-y-4">
        <div className="pb-3 border-b border-dp-line">
          <h3 className="text-sm font-bold text-dp-blue">Журнал доставок Webhook</h3>
          <p className="text-xs text-dp-blue/60">
            История HTTP-отправок, HTTP-коды ответов и повторы при ошибках
          </p>
        </div>

        {deliveries.length === 0 && <p className="text-xs text-dp-blue/60">Доставок пока нет.</p>}
        <div className="overflow-x-auto">
          <table className="w-full text-left text-xs border-collapse">
            <thead>
              <tr className="border-b border-dp-line text-dp-blue/60 font-semibold uppercase tracking-wider text-[10px]">
                <th className="py-2.5 px-3">Событие</th>
                <th className="py-2.5 px-3">Статус</th>
                <th className="py-2.5 px-3 text-center">Попытки</th>
                <th className="py-2.5 px-3">Последний ответ</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-dp-line/50">
              {deliveries.map((d) => (
                <WebhookDeliveryRow
                  key={d.id}
                  delivery={d}
                />
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
};
