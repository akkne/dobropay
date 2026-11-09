import React from 'react';
import { WebhookDelivery } from '../../types/WebhookDelivery';

export interface WebhookDeliveryRowProps {
  delivery: WebhookDelivery;
}

export const WebhookDeliveryRow: React.FC<WebhookDeliveryRowProps> = ({ delivery }) => (
  <tr className="border-b border-dp-line/50 text-xs">
    <td className="py-2.5 px-3 font-mono">{delivery.id}</td>
    <td className="py-2.5 px-3">{delivery.state}</td>
    <td className="py-2.5 px-3 text-center">{delivery.attempts}</td>
    <td className="py-2.5 px-3">{delivery.lastResult || '—'}</td>
  </tr>
);
