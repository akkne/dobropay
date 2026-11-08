import React from 'react';
import Link from 'next/link';
import { Brand } from '../ui/Brand';
import { Button } from '../ui/Button';

interface AdminTopBarProps {
  shopName: string;
  isAuthenticated: boolean;
  userEmail?: string;
  onOpenLogin: () => void;
  onLogout: () => void;
}

export const AdminTopBar: React.FC<AdminTopBarProps> = ({
  shopName,
  isAuthenticated,
  userEmail = 'demo@dobropay.local',
  onOpenLogin,
  onLogout
}) => {
  return (
    <header className="admin-topbar">
      <div className="topbar-left">
        <Brand size={24} />
        <div className="shop-badge-dropdown">
          <span className="status-dot-active" />
          <span>{shopName}</span>
          <span className="mode-badge">Тестовый режим</span>
        </div>
      </div>

      <div className="topbar-right">
        <Link href="/checkout/demo-checkout-token-2026" target="_blank" className="button outline sm">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
            <path d="M18 13v6a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h6" />
            <polyline points="15 3 21 3 21 9" />
            <line x1="10" y1="14" x2="21" y2="3" />
          </svg>
          <span>Hosted Checkout</span>
        </Link>

        {!isAuthenticated ? (
          <Button variant="primary" size="sm" onClick={onOpenLogin}>
            Войти
          </Button>
        ) : (
          <div className="shop-badge-dropdown">
            <span style={{ fontSize: '13px' }}>{userEmail}</span>
            <Button variant="ghost" size="sm" onClick={onLogout} style={{ padding: '2px 6px' }}>
              Выйти
            </Button>
          </div>
        )}
      </div>
    </header>
  );
};
