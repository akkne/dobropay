import React from 'react';
import { Button } from '../ui/Button';

export interface ApplicationStatusStepProps {
  onReset: () => void;
}

export const ApplicationStatusStep: React.FC<ApplicationStatusStepProps> = ({ onReset }) => {
  return (
    <div className="max-w-md mx-auto text-center space-y-4 py-4">
      {/* Clock icon 36x36 from DESIGN.md */}
      <div className="w-16 h-16 mx-auto rounded-full bg-dp-amber-blush text-dp-amber border-2 border-amber-500/40 flex items-center justify-center">
        <svg className="w-9 h-9" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.2">
          <circle cx="12" cy="12" r="10" />
          <polyline points="12 6 12 12 16 14" />
        </svg>
      </div>

      <div className="space-y-1">
        <h3 className="text-base font-bold text-dp-blue">Заявка на проверке комплаенс</h3>
        <p className="text-xs text-dp-blue/70 leading-relaxed">
          Мы проверяем данные организации и сайт магазина. Обычно рассмотрение занимает от 15 минут до 2 часов.
        </p>
      </div>

      <div className="p-3.5 bg-dp-tint rounded-card border border-dp-line text-xs text-dp-blue/80 space-y-1.5 text-left">
        <div className="flex justify-between">
          <span className="text-dp-blue/60">Статус:</span>
          <span className="font-bold text-dp-amber">В обработке</span>
        </div>
        <div className="flex justify-between">
          <span className="text-dp-blue/60">Тестовый режим:</span>
          <span className="font-semibold text-dp-green">Доступен (Live & Test ключи активны)</span>
        </div>
      </div>

      <div className="pt-2">
        <Button variant="outline" size="md" full onClick={onReset}>
          Вернуться в панель управления
        </Button>
      </div>
    </div>
  );
};
