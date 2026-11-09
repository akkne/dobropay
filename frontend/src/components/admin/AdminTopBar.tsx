import React from 'react';
import Link from 'next/link';
import { Brand } from '../ui/Brand';
import { Button } from '../ui/Button';

export interface AdminTopBarProps {
  shopName: string;
  isAuthenticated: boolean;
  userEmail?: string;
  onOpenLogin: () => void;
  onLogout: () => void;
}

export const AdminTopBar: React.FC<AdminTopBarProps> = ({
  shopName,
  isAuthenticated,
  userEmail,
  onOpenLogin,
  onLogout
}) => {
  return (
    <header className="bg-white border-b border-dp-line px-4 md:px-8 py-3 flex items-center justify-between sticky top-0 z-30 shadow-xs">
      <div className="flex items-center gap-4">
        <Link href="/" aria-label="Dobropay — главная"><Brand size={24} /></Link>
        <div className="hidden sm:flex items-center gap-2 px-3 py-1 bg-dp-tint rounded-pill border border-dp-line text-xs font-semibold text-dp-blue">
          <span className="w-2 h-2 rounded-full bg-dp-green" />
          <span>{shopName}</span>
        </div>
      </div>

      <div className="flex items-center gap-2.5">
        <Link href="/developers" className="text-xs font-semibold text-dp-blue hover:underline">Разработчикам</Link>
        {!isAuthenticated ? (
          <Button variant="primary" size="sm" onClick={onOpenLogin}>
            Войти
          </Button>
        ) : (
          <div className="flex items-center gap-2 bg-dp-tint px-3 py-1 rounded-pill text-xs">
            <span className="font-mono text-dp-blue font-medium hidden sm:inline">{userEmail}</span>
            <button
              type="button"
              onClick={onLogout}
              className="text-xs text-dp-blue/60 hover:text-dp-red font-semibold cursor-pointer"
            >
              Выйти
            </button>
          </div>
        )}
      </div>
    </header>
  );
};
