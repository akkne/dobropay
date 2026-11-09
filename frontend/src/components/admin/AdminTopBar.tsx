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
  userEmail = 'manager@dobroplay.online',
  onOpenLogin,
  onLogout
}) => {
  return (
    <header className="bg-white border-b border-dp-line px-4 md:px-8 py-3 flex items-center justify-between sticky top-0 z-30 shadow-xs">
      <div className="flex items-center gap-4">
        <Brand size={24} />
        <div className="hidden sm:flex items-center gap-2 px-3 py-1 bg-dp-tint rounded-pill border border-dp-line text-xs font-semibold text-dp-blue">
          <span className="w-2 h-2 rounded-full bg-dp-green" />
          <span>{shopName}</span>
        </div>
      </div>

      <div className="flex items-center gap-2.5">
        <Link
          href="/developers"
          className="inline-flex items-center px-3 py-1.5 rounded-pill text-xs font-semibold text-dp-blue hover:bg-dp-tint transition-colors"
        >
          Документация API
        </Link>
        <Link
          href="/checkout/demo-checkout-token-2026"
          target="_blank"
          className="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-pill text-xs font-medium border border-dp-line bg-transparent text-dp-blue hover:bg-dp-tint transition-colors"
        >
          <svg className="w-3.5 h-3.5" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
            <path d="M18 13v6a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h6" />
            <polyline points="15 3 21 3 21 9" />
            <line x1="10" y1="14" x2="21" y2="3" />
          </svg>
          <span className="hidden sm:inline">Открыть</span>
          <span>Checkout</span>
        </Link>

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
