import React from 'react';

export const VerificationChecklist: React.FC = () => {
  const items = [
    'Автоматическая проверка ИНН по базе ЕГРЮЛ / ЕГРИП',
    'Проверка банковского БИК и контрольного ключа расчетного счета',
    'Комплаенс-проверка сайта на соответствие 115-ФЗ и правилам МИР / СБП'
  ];

  return (
    <div className="bg-dp-blush border border-dp-line rounded-card p-4 space-y-2.5 max-w-md mx-auto">
      <div className="text-xs font-bold text-dp-blue">
        Параметры автоматической верификации:
      </div>
      <div className="space-y-2 text-xs text-dp-blue/80">
        {items.map((text, i) => (
          <div key={i} className="flex items-start gap-2.5">
            <div className="w-[23px] h-[23px] rounded-full bg-dp-blue text-white flex items-center justify-center flex-shrink-0 mt-0.5">
              <svg className="w-3.5 h-3.5" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="3">
                <polyline points="20 6 9 17 4 12" />
              </svg>
            </div>
            <span className="leading-tight">{text}</span>
          </div>
        ))}
      </div>
    </div>
  );
};
