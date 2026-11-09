'use client';

import React, { useState, useEffect, useCallback, Suspense } from 'react';
import { useParams } from 'next/navigation';
import { CheckoutHeader } from '../../../components/checkout/CheckoutHeader';
import { OrderSummary } from '../../../components/checkout/OrderSummary';
import { CheckoutLoadingSkeleton } from '../../../components/checkout/CheckoutLoadingSkeleton';
import { CheckoutErrorState } from '../../../components/checkout/CheckoutErrorState';
import { Modal } from '../../../components/ui/Modal';
import { Button } from '../../../components/ui/Button';
import { apiClient } from '../../../services/api';
import { CheckoutSession } from '../../../types/CheckoutSession';

function CheckoutContent() {
  const params = useParams();
  const token = params?.token as string;
  const [session, setSession] = useState<CheckoutSession | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [sbpOpen, setSbpOpen] = useState(false);
  const [sbpBusy, setSbpBusy] = useState(false);
  const [sbpWaiting, setSbpWaiting] = useState(false);
  const [sbpError, setSbpError] = useState<string | null>(null);

  const loadSession = useCallback(async () => {
    setLoading(true);
    try {
      setSession(await apiClient.getCheckoutSession(token));
      setError(null);
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : 'Не удалось загрузить платёж');
    } finally {
      setLoading(false);
    }
  }, [token]);

  useEffect(() => { void loadSession(); }, [loadSession]);

  useEffect(() => {
    if (!sbpWaiting) return;
    const timer = window.setInterval(async () => {
      try {
        const updated = await apiClient.getCheckoutSession(token);
        setSession(updated);
        if (updated.status !== 'pending') {
          setSbpWaiting(false);
          setSbpOpen(false);
        }
      } catch {
        setSbpError('Не удалось обновить статус платежа. Попробуйте ещё раз.');
      }
    }, 2000);
    return () => window.clearInterval(timer);
  }, [sbpWaiting, token]);

  const openSbp = async () => {
    setSbpOpen(true);
    setSbpBusy(true);
    setSbpError(null);
    try {
      await apiClient.startSbpCheckout(token);
    } catch (cause) {
      setSbpError(cause instanceof Error ? cause.message : 'Не удалось начать оплату');
    } finally {
      setSbpBusy(false);
    }
  };

  const completeSbp = async (outcome: 'succeeded' | 'canceled') => {
    setSbpBusy(true);
    setSbpError(null);
    try {
      await apiClient.simulateSbpCheckout(token, outcome);
      setSbpWaiting(true);
    } catch (cause) {
      setSbpError(cause instanceof Error ? cause.message : 'Не удалось подтвердить операцию');
    } finally {
      setSbpBusy(false);
    }
  };

  if (loading) return <div className="min-h-screen flex items-center justify-center"><CheckoutLoadingSkeleton /></div>;
  if (error || !session) return <div className="min-h-screen flex items-center justify-center"><CheckoutErrorState message={error || 'Платёж не найден'} onRetry={loadSession} /></div>;

  const remainingSeconds = Math.max(0, Math.floor((new Date(session.expiresAt).getTime() - Date.now()) / 1000));
  return (
    <div className="min-h-screen bg-gradient-to-b from-dp-tint to-white flex justify-center px-4 py-8">
      <div className="w-full max-w-[500px] space-y-4">
        <CheckoutHeader remainingSeconds={remainingSeconds} />
        <OrderSummary shopName={session.shopName} orderId={session.orderId} amount={session.amount} />
        <div className="bg-white border border-dp-line rounded-card p-6 text-center">
          <h1 className="text-lg font-bold">Статус платежа</h1>
          <p className="mt-3">{session.status}</p>
          {session.status === 'pending' && (
            <div className="mt-4">
              <Button type="button" onClick={() => void openSbp()}>Оплатить через СБП</Button>
            </div>
          )}
          <button type="button" onClick={() => void loadSession()} className="mt-5 text-sm underline">Обновить статус</button>
        </div>
      </div>
      <Modal isOpen={sbpOpen} onClose={() => setSbpOpen(false)} title="Оплата через СБП">
        <p className="mb-4 text-sm text-dp-blue">Демонстрация оплаты. Деньги не списываются.</p>
        <p className="mb-5 text-lg font-bold text-dp-blue">{(session.amount / 100).toLocaleString('ru-RU', { minimumFractionDigits: 2 })} ₽</p>
        {sbpError && <div className="mb-4"><p role="alert" className="text-sm text-dp-red">{sbpError}</p><button type="button" className="mt-2 text-sm underline" onClick={() => void openSbp()}>Попробовать ещё раз</button></div>}
        {sbpWaiting ? <p role="status" className="text-sm">Подтверждаем платёж…</p> : (
          <div className="flex flex-wrap gap-3">
            <Button type="button" disabled={sbpBusy || Boolean(sbpError)} onClick={() => void completeSbp('succeeded')}>Подтвердить оплату</Button>
            <Button type="button" variant="outline" disabled={sbpBusy || Boolean(sbpError)} onClick={() => void completeSbp('canceled')}>Отменить платёж</Button>
          </div>
        )}
      </Modal>
    </div>
  );
}

export default function CheckoutPage() {
  return <Suspense fallback={<div className="min-h-screen flex items-center justify-center"><CheckoutLoadingSkeleton /></div>}><CheckoutContent /></Suspense>;
}
