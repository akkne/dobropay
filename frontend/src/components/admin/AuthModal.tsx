import React, { useState } from 'react';
import { Brand } from '../ui/Brand';
import { Modal } from '../ui/Modal';
import { Button } from '../ui/Button';

export interface AuthModalProps {
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
    <Modal isOpen={isOpen} onClose={onClose} maxWidth="420px">
      <div className="flex justify-center mb-3">
        <Brand size={26} />
      </div>

      <h3 className="text-lg font-bold text-dp-blue text-center mb-1">
        Вход в личный кабинет
      </h3>
      <p className="text-xs text-dp-blue/60 text-center mb-5">
        Управляйте магазинами, платежами и интеграциями
      </p>

      <form onSubmit={handleSubmit} className="space-y-4">
        <div className="space-y-1">
          <label className="text-xs font-semibold text-dp-blue" htmlFor="auth-email">
            Email сотрудника
          </label>
          <input
            type="email"
            id="auth-email"
            className="w-full px-3.5 py-2.5 text-xs rounded-card border border-dp-line bg-white focus:outline-none focus:border-dp-blue"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            required
          />
        </div>

        <div className="space-y-1">
          <label className="text-xs font-semibold text-dp-blue" htmlFor="auth-password">
            Пароль
          </label>
          <input
            type="password"
            id="auth-password"
            className="w-full px-3.5 py-2.5 text-xs rounded-card border border-dp-line bg-white focus:outline-none focus:border-dp-blue"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            required
          />
        </div>

        <div className="pt-2 space-y-2">
          <Button type="submit" variant="primary-blue" size="md" full>
            Войти в кабинет
          </Button>

          <Button
            type="button"
            variant="outline"
            size="sm"
            full
            onClick={handleFillDemo}
          >
            Подставить демо-данные
          </Button>
        </div>
      </form>
    </Modal>
  );
};
