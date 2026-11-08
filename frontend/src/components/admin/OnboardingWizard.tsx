import React, { useState } from 'react';
import { OnboardingProgressStepper } from './OnboardingProgressStepper';
import { ShopBasicInfoStep } from './ShopBasicInfoStep';
import { CompanyLegalStep } from './CompanyLegalStep';
import { VerificationChecklist } from './VerificationChecklist';
import { ApplicationStatusStep } from './ApplicationStatusStep';

export interface OnboardingWizardProps {
  onComplete: () => void;
}

export const OnboardingWizard: React.FC<OnboardingWizardProps> = ({ onComplete }) => {
  const [step, setStep] = useState<number>(1);

  return (
    <div className="bg-white border border-dp-line rounded-card p-6 shadow-sm space-y-6 max-w-xl mx-auto">
      <div className="text-center pb-2 border-b border-dp-line">
        <h2 className="text-base font-bold text-dp-blue">
          Подключение онлайн-эквайринга Добропей
        </h2>
        <p className="text-xs text-dp-blue/60 mt-1">
          3 простых шага для активации СБП, банковских карт и криптовалютных платежей
        </p>
      </div>

      <OnboardingProgressStepper currentStep={step} />

      {step === 1 && (
        <ShopBasicInfoStep onNext={() => setStep(2)} />
      )}

      {step === 2 && (
        <div className="space-y-6">
          <VerificationChecklist />
          <CompanyLegalStep onNext={() => setStep(3)} onBack={() => setStep(1)} />
        </div>
      )}

      {step === 3 && (
        <ApplicationStatusStep onReset={onComplete} />
      )}
    </div>
  );
};
