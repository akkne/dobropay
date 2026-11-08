import React, { useState } from 'react';
import { Button } from '../ui/Button';
import { validateInn, validateBik, validateAccount } from '../../utils/validation';

export interface CompanyLegalStepProps {
  onNext: (data: { inn: string; ogrn: string; companyName: string; bik: string; account: string }) => void;
  onBack: () => void;
}

export const CompanyLegalStep: React.FC<CompanyLegalStepProps> = ({ onNext, onBack }) => {
  const [inn, setInn] = useState('7707083893');
  const [ogrn, setOgrn] = useState('1027700132195');
  const [companyName, setCompanyName] = useState('ООО «Добропей Диджитал»');
  const [bik, setBik] = useState('044525974');
  const [account, setAccount] = useState('40702810938000012345');

  const isInnValid = validateInn(inn);
  const isBikValid = validateBik(bik);
  const isAccountValid = validateAccount(account);

  const isFormValid = isInnValid && isBikValid && isAccountValid && companyName.trim().length > 0;

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!isFormValid) return;
    onNext({ inn, ogrn, companyName, bik, account });
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-4 max-w-md mx-auto">
      <div className="space-y-1">
        <div className="flex items-center justify-between">
          <label className="text-xs font-semibold text-dp-blue">ИНН (10 или 12 цифр)</label>
          <span className={`text-[10px] font-bold ${isInnValid ? 'text-dp-green' : 'text-dp-red'}`}>
            {isInnValid ? 'Контрольный разряд верный' : '10 или 12 цифр'}
          </span>
        </div>
        <input
          type="text"
          maxLength={12}
          value={inn}
          onChange={(e) => setInn(e.target.value.replace(/\D/g, ''))}
          className="w-full px-3.5 py-2.5 text-xs rounded-card border border-dp-line bg-white font-mono focus:outline-none focus:border-dp-blue"
        />
      </div>

      <div className="space-y-1">
        <label className="text-xs font-semibold text-dp-blue">Наименование организации / ИП</label>
        <input
          type="text"
          required
          value={companyName}
          onChange={(e) => setCompanyName(e.target.value)}
          className="w-full px-3.5 py-2.5 text-xs rounded-card border border-dp-line bg-white focus:outline-none focus:border-dp-blue"
        />
      </div>

      <div className="grid grid-cols-2 gap-3">
        <div className="space-y-1">
          <div className="flex items-center justify-between">
            <label className="text-xs font-semibold text-dp-blue">БИК банка</label>
            <span className={`text-[10px] font-bold ${isBikValid ? 'text-dp-green' : 'text-dp-red'}`}>
              {isBikValid ? '04...' : '9 цифр'}
            </span>
          </div>
          <input
            type="text"
            maxLength={9}
            value={bik}
            onChange={(e) => setBik(e.target.value.replace(/\D/g, ''))}
            className="w-full px-3.5 py-2.5 text-xs rounded-card border border-dp-line bg-white font-mono focus:outline-none focus:border-dp-blue"
          />
        </div>

        <div className="space-y-1">
          <label className="text-xs font-semibold text-dp-blue">ОГРН / ОГРНИП</label>
          <input
            type="text"
            maxLength={15}
            value={ogrn}
            onChange={(e) => setOgrn(e.target.value.replace(/\D/g, ''))}
            className="w-full px-3.5 py-2.5 text-xs rounded-card border border-dp-line bg-white font-mono focus:outline-none focus:border-dp-blue"
          />
        </div>
      </div>

      <div className="space-y-1">
        <div className="flex items-center justify-between">
          <label className="text-xs font-semibold text-dp-blue">Расчётный счёт (20 цифр)</label>
          <span className={`text-[10px] font-bold ${isAccountValid ? 'text-dp-green' : 'text-dp-red'}`}>
            {isAccountValid ? 'Корректный' : '20 цифр (407...)'}
          </span>
        </div>
        <input
          type="text"
          maxLength={20}
          value={account}
          onChange={(e) => setAccount(e.target.value.replace(/\D/g, ''))}
          className="w-full px-3.5 py-2.5 text-xs rounded-card border border-dp-line bg-white font-mono focus:outline-none focus:border-dp-blue"
        />
      </div>

      <div className="flex gap-2 pt-2">
        <Button type="button" variant="outline" size="md" onClick={onBack}>
          Назад
        </Button>
        <Button type="submit" variant="primary-blue" size="md" full disabled={!isFormValid}>
          Отправить заявку
        </Button>
      </div>
    </form>
  );
};
