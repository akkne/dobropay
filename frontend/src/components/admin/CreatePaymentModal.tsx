'use client';

import React, { FormEvent, useState } from 'react';
import { Modal } from '../ui/Modal';
import { PaymentMethodType } from '../../types/PaymentMethodType';

export interface CreatePaymentInput {
  amount: number;
  externalOrderId: string;
  paymentMethodType: PaymentMethodType;
  capture: boolean;
}

export function CreatePaymentModal({
  isOpen,
  onClose,
  onCreate
}: {
  isOpen: boolean;
  onClose: () => void;
  onCreate: (input: CreatePaymentInput) => Promise<void>;
}) {
  const [rubles, setRubles] = useState('');
  const [orderId, setOrderId] = useState('');
  const [paymentMethodType, setPaymentMethodType] = useState<PaymentMethodType>('sbp');
  const [capture, setCapture] = useState(true);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const submit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const amount = Math.round(Number(rubles.replace(',', '.')) * 100);
    if (!Number.isSafeInteger(amount) || amount <= 0) {
      setError('Введите сумму больше нуля.');
      return;
    }
    setBusy(true);
    setError(null);
    try {
      await onCreate({
        amount,
        externalOrderId: orderId.trim() || `order-${crypto.randomUUID()}`,
        paymentMethodType,
        capture
      });
      setRubles('');
      setOrderId('');
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : 'Не удалось создать платёж');
    } finally {
      setBusy(false);
    }
  };

  return (
    <Modal isOpen={isOpen} onClose={onClose} maxWidth="460px">
      <h2 className="text-lg font-bold text-dp-blue">Создать платёж</h2>
      <p className="mt-1 text-sm text-dp-blue/65">После создания отправьте покупателю ссылку на оплату.</p>
      <form className="mt-5 space-y-4" onSubmit={submit}>
        <label className="block space-y-1 text-sm font-medium text-dp-blue">
          <span>Сумма, ₽</span>
          <input required min="1" step="0.01" type="number" value={rubles} onChange={event => setRubles(event.target.value)} className="w-full rounded-card border border-dp-line px-3 py-2.5" />
        </label>
        <label className="block space-y-1 text-sm font-medium text-dp-blue">
          <span>Номер заказа</span>
          <input value={orderId} onChange={event => setOrderId(event.target.value)} className="w-full rounded-card border border-dp-line px-3 py-2.5" placeholder="Можно оставить пустым" />
        </label>
        <label className="block space-y-1 text-sm font-medium text-dp-blue">
          <span>Способ оплаты</span>
          <select value={paymentMethodType} onChange={event => setPaymentMethodType(event.target.value as PaymentMethodType)} className="w-full rounded-card border border-dp-line px-3 py-2.5">
            <option value="sbp">СБП</option>
            <option value="bank_card">Банковская карта</option>
          </select>
        </label>
        <label className="flex items-start gap-2 text-sm text-dp-blue">
          <input type="checkbox" checked={capture} onChange={event => setCapture(event.target.checked)} className="mt-1" />
          <span>Списать средства сразу</span>
        </label>
        {error && <p role="alert" className="text-sm text-red-700">{error}</p>}
        <div className="flex gap-3 pt-1">
          <button type="button" onClick={onClose} className="flex-1 rounded-pill border border-dp-line px-4 py-2.5 text-sm">Отмена</button>
          <button disabled={busy} type="submit" className="flex-1 rounded-pill bg-dp-blue px-4 py-2.5 text-sm font-semibold text-white disabled:opacity-50">{busy ? 'Создаём…' : 'Создать ссылку'}</button>
        </div>
      </form>
    </Modal>
  );
}
