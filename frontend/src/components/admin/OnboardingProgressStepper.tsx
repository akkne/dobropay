import React from 'react';

export interface OnboardingProgressStepperProps {
  currentStep: number;
}

export const OnboardingProgressStepper: React.FC<OnboardingProgressStepperProps> = ({ currentStep }) => {
  const steps = [
    { num: 1, label: 'Магазин' },
    { num: 2, label: 'Анкета юрлица' },
    { num: 3, label: 'Проверка заявки' }
  ];

  return (
    <div className="flex items-center justify-between max-w-sm mx-auto mb-6">
      {steps.map((s, idx) => (
        <React.Fragment key={s.num}>
          <div className="flex flex-col items-center gap-1.5">
            <div
              className={`w-8 h-8 rounded-full flex items-center justify-center text-xs font-bold transition-colors ${
                currentStep >= s.num
                  ? 'bg-dp-blue text-white shadow-sm'
                  : 'bg-dp-tint text-dp-blue/40 border border-dp-line'
              }`}
            >
              {s.num}
            </div>
            <span
              className={`text-[11px] font-medium ${
                currentStep >= s.num ? 'text-dp-blue font-bold' : 'text-dp-blue/40'
              }`}
            >
              {s.label}
            </span>
          </div>

          {idx < steps.length - 1 && (
            <div
              className={`flex-1 h-0.5 mx-2 -mt-4 transition-colors ${
                currentStep > s.num ? 'bg-dp-blue' : 'bg-dp-line'
              }`}
            />
          )}
        </React.Fragment>
      ))}
    </div>
  );
};
