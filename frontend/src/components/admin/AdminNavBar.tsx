import React from 'react';

export type AdminTab = 'payments' | 'receipts' | 'developer' | 'onboarding';

export interface AdminNavBarProps {
  activeTab: AdminTab;
  onChange: (tab: AdminTab) => void;
}

export const AdminNavBar: React.FC<AdminNavBarProps> = ({ activeTab, onChange }) => {
  const tabs: Array<{ id: AdminTab; label: string; icon: React.ReactNode }> = [
    {
      id: 'payments',
      label: 'Платежи и возвраты',
      icon: (
        <svg className="w-4 h-4" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
          <line x1="12" y1="1" x2="12" y2="23" />
          <path d="M17 5H9.5a3.5 3.5 0 0 0 0 7h5a3.5 3.5 0 0 1 0 7H6" />
        </svg>
      )
    },
    {
      id: 'receipts',
      label: 'Чеки',
      icon: (
        <svg className="w-4 h-4" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
          <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z" />
          <polyline points="14 2 14 8 20 8" />
          <line x1="16" y1="13" x2="8" y2="13" />
          <line x1="16" y1="17" x2="8" y2="17" />
        </svg>
      )
    },
    {
      id: 'developer',
      label: 'Разработчикам (API & Webhook)',
      icon: (
        <svg className="w-4 h-4" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
          <polyline points="16 18 22 12 16 6" />
          <polyline points="8 6 2 12 8 18" />
        </svg>
      )
    },
    {
      id: 'onboarding',
      label: 'Подключение магазина (3 шага)',
      icon: (
        <svg className="w-4 h-4" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
          <path d="M16 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2" />
          <circle cx="8.5" cy="7" r="4" />
          <line x1="20" y1="8" x2="20" y2="14" />
          <line x1="23" y1="11" x2="17" y2="11" />
        </svg>
      )
    }
  ];

  return (
    <nav className="admin-nav border-b border-dp-line bg-white px-4 md:px-8 overflow-x-auto" aria-label="Разделы кабинета">
      <ul className="flex items-center gap-1 md:gap-3 py-1">
        {tabs.map((tab) => {
          const isActive = activeTab === tab.id;
          return (
            <li key={tab.id}>
              <button
                type="button"
                onClick={() => onChange(tab.id)}
                className={`flex items-center gap-2 py-3 px-3.5 text-xs font-semibold border-b-2 transition-all whitespace-nowrap cursor-pointer ${
                  isActive
                    ? 'border-dp-blue text-dp-blue'
                    : 'border-transparent text-dp-blue/60 hover:text-dp-blue hover:border-dp-line'
                }`}
              >
                {tab.icon}
                <span>{tab.label}</span>
              </button>
            </li>
          );
        })}
      </ul>
    </nav>
  );
};
