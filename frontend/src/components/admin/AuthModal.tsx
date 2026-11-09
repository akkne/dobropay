import React, { useState } from 'react';
import { Brand } from '../ui/Brand';
import { Modal } from '../ui/Modal';
import { Button } from '../ui/Button';

export interface AuthModalProps {
  isOpen: boolean;
  error: string | null;
  onClose: () => void;
  onLogin: (email: string, password: string) => void;
  onRegister: (email: string, password: string) => void;
}

export const AuthModal: React.FC<AuthModalProps> = ({ isOpen, error, onClose, onLogin, onRegister }) => {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [register, setRegister] = useState(false);

  const handleSubmit = (event: React.FormEvent) => {
    event.preventDefault();
    if (register) onRegister(email, password);
    else onLogin(email, password);
  };

  return (
    <Modal isOpen={isOpen} onClose={onClose} maxWidth="420px">
      <div className="flex justify-center mb-3"><Brand size={26} /></div>
      <h3 className="text-lg font-bold text-dp-blue text-center mb-5">{register ? 'Создать аккаунт' : 'Вход в личный кабинет'}</h3>
      <form onSubmit={handleSubmit} className="space-y-4">
        {error && <p role="alert" className="text-xs text-dp-red">{error}</p>}
        <label className="block text-xs font-semibold text-dp-blue" htmlFor="auth-email">Email сотрудника</label>
        <input type="email" id="auth-email" autoComplete="email" required value={email}
          onChange={event => setEmail(event.target.value)} className="w-full px-3.5 py-2.5 text-xs rounded-card border border-dp-line" />
        <label className="block text-xs font-semibold text-dp-blue" htmlFor="auth-password">Пароль</label>
        <input type="password" id="auth-password" autoComplete={register ? 'new-password' : 'current-password'} minLength={register ? 8 : undefined} required
          value={password} onChange={event => setPassword(event.target.value)} className="w-full px-3.5 py-2.5 text-xs rounded-card border border-dp-line" />
        <Button type="submit" variant="primary-blue" size="md" full>{register ? 'Зарегистрироваться' : 'Войти'}</Button>
        <button type="button" onClick={() => setRegister(!register)} className="w-full text-xs underline text-dp-blue">
          {register ? 'Уже есть аккаунт? Войти' : 'Нет аккаунта? Зарегистрироваться'}
        </button>
      </form>
    </Modal>
  );
};
