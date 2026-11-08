import React, { useState } from 'react';
import { Modal } from '../ui/Modal';
import { Button } from '../ui/Button';
import { WebhookDelivery } from '../../types/WebhookDelivery';

export interface WebhookInspectorModalProps {
  isOpen: boolean;
  delivery: WebhookDelivery | null;
  onClose: () => void;
}

export const WebhookInspectorModal: React.FC<WebhookInspectorModalProps> = ({
  isOpen,
  delivery,
  onClose
}) => {
  const [copied, setCopied] = useState(false);

  if (!delivery) return null;

  const handleCopyPayload = () => {
    navigator.clipboard.writeText(delivery.payload);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

  return (
    <Modal isOpen={isOpen} onClose={onClose} maxWidth="600px">
      <div className="space-y-4">
        <div className="pb-3 border-b border-dp-line">
          <h3 className="text-sm font-bold text-dp-blue">
            Инспектор HTTP Webhook: {delivery.eventType}
          </h3>
          <p className="text-xs text-dp-blue/60 font-mono truncate">{delivery.url}</p>
        </div>

        <div className="space-y-1.5">
          <label className="text-xs font-semibold text-dp-blue">Заголовки запроса</label>
          <div className="bg-dp-tint p-3 rounded-card font-mono text-[11px] text-dp-blue space-y-1 border border-dp-line">
            <div><strong className="text-dp-blue/70">Content-Type:</strong> application/json; charset=utf-8</div>
            <div className="break-all"><strong className="text-dp-blue/70">X-Dobropay-Signature:</strong> {delivery.signature}</div>
            <div><strong className="text-dp-blue/70">X-Dobropay-Delivery-Id:</strong> {delivery.id}</div>
            <div><strong className="text-dp-blue/70">X-Dobropay-Event:</strong> {delivery.eventType}</div>
          </div>
        </div>

        <div className="space-y-1.5">
          <div className="flex items-center justify-between">
            <label className="text-xs font-semibold text-dp-blue">Тело запроса (JSON Payload)</label>
            <button
              type="button"
              onClick={handleCopyPayload}
              className="text-[11px] text-blue-600 hover:underline cursor-pointer"
            >
              {copied ? 'Скопировано!' : 'Копировать JSON'}
            </button>
          </div>
          <pre className="bg-[#0b1329] text-emerald-400 p-3.5 rounded-card font-mono text-xs overflow-x-auto max-h-56 leading-relaxed">
            {delivery.payload}
          </pre>
        </div>

        <div className="flex justify-end pt-2">
          <Button variant="outline" size="sm" onClick={onClose}>
            Закрыть инспектор
          </Button>
        </div>
      </div>
    </Modal>
  );
};
