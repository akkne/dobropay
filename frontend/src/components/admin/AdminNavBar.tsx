import React from 'react';

export type AdminTab = 'payments' | 'receipts' | 'developer' | 'onboarding';

interface AdminNavBarProps {
  activeTab: AdminTab;
  onChange: (tab: AdminTab) => void;
}

export const AdminNavBar: React.FC<AdminNavBarProps> = ({ activeTab, onChange }) => {
  return (
    <nav className="admin-nav-bar" aria-label="Разделы кабинета">
      <ul className="admin-nav-tabs">
        <li
          className={`admin-nav-item ${activeTab === 'payments' ? 'active' : ''}`}
          onClick={() => onChange('payments')}
        >
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
            <line x1="12" y1="1" x2="12" y2="23" />
            <path d="M17 5H9.5a3.5 3.5 0 0 0 0 7h5a3.5 3.5 0 0 1 0 7H6" />
          </svg>
          <span>Платежи и возвраты</span>
        </li>

        <li
          className={`admin-nav-item ${activeTab === 'receipts' ? 'active' : ''}`}
          onClick={() => onChange('receipts')}
        >
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
            <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z" />
            <polyline points="14 2 14 8 20 8" />
            <line x1="16" y1="13" x2="8" y2="13" />
            <line x1="16" y1="17" x2="8" y2="17" />
            <polyline points="10 9 9 9 8 9" />
          </svg>
          <span>Чеки 54-ФЗ</span>
        </li>

        <li
          className={`admin-nav-item ${activeTab === 'developer' ? 'active' : ''}`}
          onClick={() => onChange('developer')}
        >
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
            <polyline points="16 18 22 12 16 6" />
            <polyline points="8 6 2 12 8 18" />
          </svg>
          <span>Разработчикам: API и Webhook</span>
        </li>

        <li
          className={`admin-nav-item ${activeTab === 'onboarding' ? 'active' : ''}`}
          onClick={() => onChange('onboarding')}
        >
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
            <polygon points="12 2 2 7 12 12 22 7 12 2" />
            <polyline points="2 17 12 22 22 17" />
            <polyline points="2 12 12 17 22 12" />
          </svg>
          <span>Онбординг магазина</span>
        </li>
      </ul>
    </nav>
  );
};
