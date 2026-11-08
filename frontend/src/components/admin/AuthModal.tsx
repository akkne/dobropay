import React, { useState } from 'react';
import { Brand } from '../ui/Brand';
import { Modal } from '../ui/Modal';
import { Button } from '../ui/Button';

interface AuthModalProps {
  isOpen: boolean;
  onClose: () => void;
  onLogin: (email: string, pass: string) => void;
}

export const AuthModal: React.FC<AuthModalProps> = ({ isOpen, onClose, onLogin }) => {
  const [email, setEmail] = useState('demo@dobropay.local');
  const [password, setPassword] = useState('local-demo-password');

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    onLogin(email, password);
  };

  const handleFillDemo = () => {
    setEmail('demo@dobropay.local');
    setPassword('local-demo-password');
  };

  return (
    <Modal isOpen={isOpen} onClose={onClose} maxWidth="440px">
      <div style={{ display: 'flex', justifyContent: 'center', marginBottom: '12px' }}>
        <Brand size={26} />
      </div>

      <h3 style={{ fontSize: '20px', fontWeight: 700, color: 'var(--blue)', textAlign: 'center', marginBottom: '6px' }}>
        Вход в личный кабинет
      </h3>
      <p style={{ fontSize: '13px', color: 'rgba(9, 49, 141, 0.65)', textAlign: 'center', marginBottom: '20px' }}>
        Управляйте магазинами, платежами и интеграциями
      </p>

      <form onSubmit={handleSubmit}>
        <div className="form-group">
          <label className="form-label" htmlFor="auth-email">Email сотрудника</label>
          <input
            type="email"
            id="auth-email"
            className="form-input"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            required
          />
        </div>

        <div className="form-group">
          <label className="form-label" htmlFor="auth-pass">Пароль</label>
          <input
            type="password"
            id="auth-pass"
            className="form-input"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            required
          />
        </div>

        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '18px' }}>
          <button
            type="button"
            className="button ghost sm"
            style={{ paddingLeft: 0, fontSize: '12px' }}
            onClick={handleFillDemo}
          >
            ⚡ Заполнить демо-данные
          </button>
          <span style={{ fontSize: '12px', color: 'rgba(9, 49, 141, 0.7)' }}>Забыли пароль?</span>
        </div>

        <Button type="submit" variant="primary" size="lg" full>
          Войти в кабинет
        </Button>
      </form>
    </Modal>
  );
};
