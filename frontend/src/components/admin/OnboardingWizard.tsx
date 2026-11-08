import React, { useState } from 'react';
import { validateInn, validateBik, validateAccount } from '../../utils/validation';
import { Button } from '../ui/Button';

interface OnboardingWizardProps {
  onSuccess: (data: { inn: string; ogrn: string; legalName: string; account: string; bik: string }) => void;
  onGoToPayments: () => void;
}

export const OnboardingWizard: React.FC<OnboardingWizardProps> = ({ onSuccess, onGoToPayments }) => {
  const [step, setStep] = useState<number>(1);
  const [shopName, setShopName] = useState('Магазин мерча Dobropay');
  const [siteUrl, setSiteUrl] = useState('https://dobropay-merch.ru');
  const [email, setEmail] = useState('merchant@dobropay-merch.ru');

  const [inn, setInn] = useState('7707083893');
  const [ogrn, setOgrn] = useState('1027700132195');
  const [legalName, setLegalName] = useState('ООО «Добропей Диджитал»');
  const [account, setAccount] = useState('40702810938000012345');
  const [bik, setBik] = useState('044525225');
  const [error, setError] = useState<string | null>(null);

  const handleStep2Submit = (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    if (!validateInn(inn)) {
      setError('ИНН должен содержать 10 или 12 цифр с корректным контрольным разрядом');
      return;
    }
    if (!validateBik(bik)) {
      setError('БИК банка должен состоять из 9 цифр и начинаться на 04');
      return;
    }
    if (!validateAccount(account)) {
      setError('Расчётный счёт должен состоять ровно из 20 цифр');
      return;
    }

    onSuccess({ inn, ogrn, legalName, account, bik });
    setStep(3);
  };

  return (
    <div className="section-card" style={{ maxWidth: '820px', margin: '0 auto', width: '100%' }}>
      <div className="wizard-steps">
        <div className={`wizard-step-node ${step === 1 ? 'active' : step > 1 ? 'completed' : ''}`}>
          <div className="wizard-step-circle">01</div>
          <div className="wizard-step-label">Регистрация</div>
        </div>
        <div className={`wizard-step-node ${step === 2 ? 'active' : step > 2 ? 'completed' : ''}`}>
          <div className="wizard-step-circle">02</div>
          <div className="wizard-step-label">Анкета юрлица</div>
        </div>
        <div className={`wizard-step-node ${step === 3 ? 'active' : ''}`}>
          <div className="wizard-step-circle">03</div>
          <div className="wizard-step-label">Проверка заявки</div>
        </div>
      </div>

      {/* Step 1 */}
      {step === 1 && (
        <div className="wizard-card-step">
          <h2 style={{ fontSize: '22px', fontWeight: 700, color: 'var(--blue)', marginBottom: '8px' }}>
            Шаг 01. Информация о магазине
          </h2>
          <p style={{ fontSize: '14px', color: 'rgba(9, 49, 141, 0.7)', marginBottom: '20px' }}>
            Укажите публичные данные вашего интернет-магазина или сервиса
          </p>

          <div className="form-group">
            <label className="form-label" htmlFor="ob-shop-name">Название магазина</label>
            <input
              type="text"
              id="ob-shop-name"
              className="form-input"
              value={shopName}
              onChange={(e) => setShopName(e.target.value)}
              required
            />
          </div>

          <div className="form-group">
            <label className="form-label" htmlFor="ob-site-url">Сайт или мобильное приложение</label>
            <input
              type="url"
              id="ob-site-url"
              className="form-input"
              value={siteUrl}
              onChange={(e) => setSiteUrl(e.target.value)}
              required
            />
          </div>

          <div className="form-group">
            <label className="form-label" htmlFor="ob-email">Email для финансовых отчётов</label>
            <input
              type="email"
              id="ob-email"
              className="form-input"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              required
            />
          </div>

          <Button
            type="button"
            variant="primary"
            size="lg"
            full
            style={{ marginTop: '14px' }}
            onClick={() => setStep(2)}
          >
            Продолжить к анкете организации →
          </Button>
        </div>
      )}

      {/* Step 2 */}
      {step === 2 && (
        <div className="wizard-card-step">
          <h2 style={{ fontSize: '22px', fontWeight: 700, color: 'var(--blue)', marginBottom: '8px' }}>
            Шаг 02. Анкета организации
          </h2>
          <p style={{ fontSize: '14px', color: 'rgba(9, 49, 141, 0.7)', marginBottom: '20px' }}>
            Заполните реквизиты юрлица или ИП для заключения договора эквайринга
          </p>

          {error && (
            <div style={{ background: 'var(--red-blush)', color: 'var(--red)', padding: '10px 14px', borderRadius: '8px', fontSize: '13px', marginBottom: '16px' }}>
              {error}
            </div>
          )}

          <form onSubmit={handleStep2Submit}>
            <div className="form-group">
              <label className="form-label" htmlFor="ob-inn">
                ИНН организации (10 цифр) или ИП (12 цифр)
              </label>
              <input
                type="text"
                id="ob-inn"
                className="form-input mono"
                maxLength={12}
                value={inn}
                onChange={(e) => setInn(e.target.value)}
                required
              />
              <span className="form-hint">Автоматическая валидация контрольного разряда</span>
            </div>

            <div className="form-group">
              <label className="form-label" htmlFor="ob-ogrn">ОГРН / ОГРНИП</label>
              <input
                type="text"
                id="ob-ogrn"
                className="form-input mono"
                maxLength={15}
                value={ogrn}
                onChange={(e) => setOgrn(e.target.value)}
                required
              />
            </div>

            <div className="form-group">
              <label className="form-label" htmlFor="ob-legal-name">Полное наименование юрлица</label>
              <input
                type="text"
                id="ob-legal-name"
                className="form-input"
                value={legalName}
                onChange={(e) => setLegalName(e.target.value)}
                required
              />
            </div>

            <div className="form-group">
              <label className="form-label" htmlFor="ob-account">Расчётный счёт (20 цифр)</label>
              <input
                type="text"
                id="ob-account"
                className="form-input mono"
                maxLength={20}
                value={account}
                onChange={(e) => setAccount(e.target.value)}
                required
              />
            </div>

            <div className="form-group">
              <label className="form-label" htmlFor="ob-bik">БИК банка (9 цифр, начинается на 04)</label>
              <input
                type="text"
                id="ob-bik"
                className="form-input mono"
                maxLength={9}
                value={bik}
                onChange={(e) => setBik(e.target.value)}
                required
              />
            </div>

            <div style={{ background: 'var(--tint)', borderRadius: 'var(--radius-sm)', padding: '14px', marginBottom: '20px' }}>
              <div className="verification-item">
                <span className="check-circle">✓</span>
                <span>Проверка отсутствия блокировок по 115-ФЗ</span>
              </div>
              <div className="verification-item">
                <span className="check-circle">✓</span>
                <span>Электронное подписание договора через КЭП</span>
              </div>
              <div className="verification-item">
                <span className="check-circle">✓</span>
                <span>Подключение прямого шлюза СБП НСПК</span>
              </div>
            </div>

            <div style={{ display: 'flex', gap: '12px' }}>
              <Button type="button" variant="outline" size="lg" onClick={() => setStep(1)}>
                ← Назад
              </Button>
              <Button type="submit" variant="primary" size="lg" full>
                Отправить анкету на проверку →
              </Button>
            </div>
          </form>
        </div>
      )}

      {/* Step 3 */}
      {step === 3 && (
        <div className="wizard-card-step" style={{ textAlign: 'center', padding: '20px 0' }}>
          <div style={{ display: 'flex', justifyContent: 'center', marginBottom: '16px' }}>
            <svg width="48" height="48" viewBox="0 0 36 36">
              <circle cx="18" cy="18" r="18" fill="var(--pink)" />
              <circle cx="18" cy="18" r="14" fill="#FFFFFF" />
              <polyline points="18 10 18 18 24 18" stroke="var(--blue)" strokeWidth="2.5" fill="none" strokeLinecap="round" />
            </svg>
          </div>

          <h2 style={{ fontSize: '24px', fontWeight: 700, color: 'var(--blue)', marginBottom: '8px' }}>
            Анкета отправлена на проверку!
          </h2>
          <p style={{ fontSize: '15px', color: 'rgba(9, 49, 141, 0.7)', maxWidth: '42ch', margin: '0 auto 24px', lineHeight: 1.5 }}>
            Комплаенс-служба проверяет предоставленные данные. Обычно это занимает от 15 минут до 2 часов.
          </p>

          <div style={{ display: 'inline-flex', alignItems: 'center', gap: '8px', padding: '8px 16px', background: 'var(--amber-blush)', borderRadius: 'var(--radius-pill)', color: 'var(--amber)', fontWeight: 600, marginBottom: '24px' }}>
            <span>● Статус: Проверка юридических документов</span>
          </div>

          <div>
            <Button variant="primary" size="lg" onClick={onGoToPayments}>
              Перейти к реестру платежей
            </Button>
          </div>
        </div>
      )}
    </div>
  );
};
