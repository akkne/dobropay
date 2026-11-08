import React from 'react';
import { WebhookDelivery } from '../../types/WebhookDelivery';
import { Modal } from '../ui/Modal';
import { Button } from '../ui/Button';

interface WebhookInspectorModalProps {
  delivery: WebhookDelivery | null;
  isOpen: boolean;
  onClose: () => void;
}

export const WebhookInspectorModal: React.FC<WebhookInspectorModalProps> = ({
  delivery,
  isOpen,
  onClose
}) => {
  if (!delivery) return null;

  return (
    <Modal isOpen={isOpen} onClose={onClose} maxWidth="600px">
      <h3 style={{ fontSize: '20px', fontWeight: 700, color: 'var(--blue)', marginBottom: '6px' }}>
        Инспектор Webhook доставки
      </h3>
      <div style={{ fontSize: '13px', color: 'rgba(9, 49, 141, 0.7)', marginBottom: '16px' }}>
        Событие: <strong className="mono">{delivery.eventType}</strong>
      </div>

      <div className="form-group">
        <label className="form-label">URL отправки:</label>
        <div className="mono" style={{ fontSize: '12px', wordBreak: 'break-all' }}>
          {delivery.url}
        </div>
      </div>

      <div className="form-group">
        <label className="form-label">Заголовок подписи <code>X-Dobropay-Signature</code>:</label>
        <div
          className="mono"
          style={{ fontSize: '12px', wordBreak: 'break-all', color: 'var(--blue)', background: 'var(--tint)', padding: '8px', borderRadius: '6px' }}
        >
          {delivery.signature}
        </div>
      </div>

      <div className="form-group">
        <label className="form-label">Тело запроса (JSON Payload):</label>
        <pre
          className="mono"
          style={{ background: '#09318D', color: '#FFFFFF', padding: '14px', borderRadius: '8px', fontSize: '12px', overflowX: 'auto', maxHeight: '220px' }}
        >
          {delivery.payload}
        </pre>
      </div>

      <Button variant="outline" full onClick={onClose}>
        Закрыть
      </Button>
    </Modal>
  );
};
