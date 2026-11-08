import React, { useState } from 'react';
import { Modal } from '../ui/Modal';
import { Button } from '../ui/Button';

interface ThreeDsModalProps {
  isOpen: boolean;
  onSuccess: () => void;
  onCancel: () => void;
}

export const ThreeDsModal: React.FC<ThreeDsModalProps> = ({ isOpen, onSuccess, onCancel }) => {
  const [code, setCode] = useState('1234');

  return (
    <Modal isOpen={isOpen} onClose={onCancel} maxWidth="420px">
      <div style={{ textAlign: 'center' }}>
        <div style={{ display: 'flex', justifyContent: 'center', marginBottom: '14px' }}>
          <span className="badge neutral" style={{ fontSize: '13px', fontWeight: 700 }}>
            3-D SECURE 2.0
          </span>
        </div>

        <h3 style={{ fontSize: '20px', fontWeight: 700, color: 'var(--blue)', marginBottom: '8px' }}>
          Подтверждение платежа
        </h3>

        <p style={{ fontSize: '14px', color: 'rgba(9, 49, 141, 0.7)', marginBottom: '20px' }}>
          Банк-эмитент отправил проверочный SMS-код на ваш привязанный номер телефона +7 (•••) •••-42-10
        </p>

        <div className="form-group" style={{ textAlign: 'left' }}>
          <label className="form-label" htmlFor="sms-code-input">
            Код из SMS (для теста: любой или 1234)
          </label>
          <input
            type="text"
            id="sms-code-input"
            className="form-input mono"
            placeholder="1234"
            maxLength={6}
            value={code}
            onChange={(e) => setCode(e.target.value)}
          />
        </div>

        <div style={{ display: 'flex', gap: '10px', marginTop: '10px' }}>
          <Button variant="primary" size="lg" full onClick={onSuccess}>
            Подтвердить
          </Button>
        </div>
      </div>
    </Modal>
  );
};
