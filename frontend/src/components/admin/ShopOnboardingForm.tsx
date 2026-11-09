'use client';

import React, { FormEvent, useEffect, useState } from 'react';

const storageKey = 'dp_shop_onboarding';

export interface ShopOnboardingData {
  name: string;
  legalName: string;
  businessType: string;
  contactName: string;
  phone: string;
  inn: string;
  ogrn: string;
  website: string;
  email: string;
  address: string;
  bik: string;
  account: string;
}

const emptyData: ShopOnboardingData = {
  name: '', legalName: '', businessType: '', contactName: '', phone: '', inn: '', ogrn: '', website: '', email: '', address: '', bik: '', account: ''
};

const fields: Array<{ key: keyof ShopOnboardingData; label: string; type?: string; pattern?: string; hint?: string }> = [
  { key: 'name', label: 'Название магазина' },
  { key: 'businessType', label: 'Форма бизнеса', hint: 'Например: ИП, ООО или самозанятый' },
  { key: 'legalName', label: 'Полное наименование ИП или организации' },
  { key: 'contactName', label: 'Контактное лицо' },
  { key: 'phone', label: 'Телефон для связи', type: 'tel', pattern: '[+0-9() -]{10,20}' },
  { key: 'inn', label: 'ИНН', pattern: '\\d{10}|\\d{12}', hint: '10 или 12 цифр' },
  { key: 'ogrn', label: 'ОГРН или ОГРНИП', pattern: '\\d{13}|\\d{15}', hint: '13 или 15 цифр' },
  { key: 'website', label: 'Сайт магазина', type: 'url' },
  { key: 'email', label: 'Рабочая почта', type: 'email' },
  { key: 'address', label: 'Юридический адрес' },
  { key: 'bik', label: 'БИК банка', pattern: '\\d{9}', hint: '9 цифр' },
  { key: 'account', label: 'Расчётный счёт', pattern: '\\d{20}', hint: '20 цифр' }
];

export function ShopOnboardingForm({ onSubmit }: { onSubmit: (data: ShopOnboardingData) => Promise<void> }) {
  const [data, setData] = useState<ShopOnboardingData>(emptyData);
  const [submitted, setSubmitted] = useState(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const saved = localStorage.getItem(storageKey);
    if (saved) {
      try {
        setData({ ...emptyData, ...JSON.parse(saved) as Partial<ShopOnboardingData> });
        setSubmitted(true);
      } catch {
        localStorage.removeItem(storageKey);
      }
    }
  }, []);

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setBusy(true);
    setError(null);
    try {
      await onSubmit(data);
      localStorage.setItem(storageKey, JSON.stringify(data));
      setSubmitted(true);
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : 'Не удалось сохранить анкету');
    } finally {
      setBusy(false);
    }
  };

  return (
    <section className="max-w-3xl rounded-card border border-dp-line bg-white p-5 shadow-sm sm:p-7">
      <h2 className="text-lg font-bold text-dp-blue">Данные магазина</h2>
      <p className="mt-2 text-sm leading-6 text-dp-blue/65">
        Заполните анкету для подключения. Пока данные анкеты сохраняются в этом браузере.
      </p>
      {submitted && <p role="status" className="mt-4 rounded-card bg-emerald-50 p-3 text-sm text-emerald-800">Анкета сохранена. Статус: на рассмотрении.</p>}
      <form onSubmit={handleSubmit} className="mt-5 grid gap-4 sm:grid-cols-2">
        {fields.map((field) => (
          <label key={field.key} className="space-y-1 text-sm font-medium text-dp-blue">
            <span>{field.label}{field.hint && <span className="ml-1 text-xs font-normal text-dp-blue/50">{field.hint}</span>}</span>
            <input
              required
              type={field.type ?? 'text'}
              pattern={field.pattern}
              value={data[field.key]}
              onChange={(event) => {
                setSubmitted(false);
                setData((current) => ({ ...current, [field.key]: field.key === 'inn' || field.key === 'ogrn' || field.key === 'bik' || field.key === 'account' ? event.target.value.replace(/\D/g, '') : event.target.value }));
              }}
              className="w-full rounded-card border border-dp-line px-3 py-2.5 text-sm focus:border-dp-blue focus:outline-none"
            />
          </label>
        ))}
        <div className="sm:col-span-2">
          {error && <p role="alert" className="mb-3 text-sm text-red-700">{error}</p>}
          <button disabled={busy} type="submit" className="rounded-pill bg-dp-blue px-5 py-2.5 text-sm font-semibold text-white disabled:opacity-50">
            {busy ? 'Сохраняем…' : 'Сохранить анкету'}
          </button>
        </div>
      </form>
    </section>
  );
}
