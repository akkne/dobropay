import React from 'react';
import { WebhookDelivery } from '../../types/WebhookDelivery';
import { formatDateTime } from '../../utils/formatters';

export interface WebhookDeliveryRowProps {
  delivery: WebhookDelivery;
  onInspect: (delivery: WebhookDelivery) => void;
  onRetry: (deliveryId: string) => void;
}

export const WebhookDeliveryRow: React.FC<WebhookDeliveryRowProps> = ({
  delivery,
  onInspect,
  onRetry
}) => {
  const is2xx = delivery.statusCode >= 200 && delivery.statusCode < 300;

  return (
    <tr className="hover:bg-dp-tint/60 transition-colors">
      <td className="py-2.5 px-3">
        <span
          className={`inline-block px-2 py-0.5 rounded text-[11px] font-bold font-mono ${
            is2xx
              ? 'bg-dp-green-blush text-dp-green border border-dp-green/20'
              : 'bg-dp-red-blush text-dp-red border border-red-500/20'
          }`}
        >
          {delivery.statusCode}
        </span>
      </td>
      <td className="py-2.5 px-3 font-semibold text-dp-blue">
        {delivery.eventType}
      </td>
      <td className="py-2.5 px-3 font-mono text-[11px] text-dp-blue/60 truncate max-w-[180px]">
        {delivery.url}
      </td>
      <td className="py-2.5 px-3 font-mono text-center">
        {delivery.attempts}
      </td>
      <td className="py-2.5 px-3 font-mono text-dp-blue/70">
        {formatDateTime(delivery.createdAt)}
      </td>
      <td className="py-2.5 px-3 text-right">
        <div className="flex items-center justify-end gap-1.5">
          <button
            type="button"
            onClick={() => onInspect(delivery)}
            className="px-2.5 py-1 text-[11px] font-semibold bg-dp-tint text-dp-blue rounded-pill hover:bg-dp-blush transition-colors"
          >
            Инспектор
          </button>
          {!is2xx && (
            <button
              type="button"
              onClick={() => onRetry(delivery.id)}
              className="px-2.5 py-1 text-[11px] font-semibold bg-dp-blue text-white rounded-pill hover:bg-blue-800 transition-colors"
            >
              Повторить
            </button>
          )}
        </div>
      </td>
    </tr>
  );
};
