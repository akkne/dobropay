import React from 'react';

export const PaymentsTableEmptyState: React.FC = () => {
  return (
    <div className="py-12 text-center space-y-2">
      <div className="w-12 h-12 mx-auto rounded-full bg-dp-tint text-dp-blue/40 flex items-center justify-center">
        <svg className="w-6 h-6" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
          <circle cx="12" cy="12" r="10" />
          <line x1="8" y1="12" x2="16" y2="12" />
        </svg>
      </div>
      <p className="text-sm font-semibold text-dp-blue">Платежи не найдены</p>
      <p className="text-xs text-dp-blue/50">Попробуйте изменить параметры поиска или фильтров</p>
    </div>
  );
};
