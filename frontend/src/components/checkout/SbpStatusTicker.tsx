import React from 'react';

export const SbpStatusTicker: React.FC = () => {
  return (
    <div className="flex items-center justify-center gap-2 text-xs font-semibold text-dp-blue/80 py-1">
      <span className="w-2.5 h-2.5 rounded-full bg-dp-green animate-ping inline-block" />
      <span>Ожидаем подтверждения от вашего банка...</span>
    </div>
  );
};
