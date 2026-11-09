'use client';

import React, { useState, useEffect, useCallback, Suspense } from 'react';
import { useParams } from 'next/navigation';
import { CheckoutHeader } from '../../../components/checkout/CheckoutHeader';
import { OrderSummary } from '../../../components/checkout/OrderSummary';
import { CheckoutLoadingSkeleton } from '../../../components/checkout/CheckoutLoadingSkeleton';
import { CheckoutErrorState } from '../../../components/checkout/CheckoutErrorState';
import { apiClient } from '../../../services/api';
import { CheckoutSession } from '../../../types/CheckoutSession';

function CheckoutContent() {
  const params = useParams();
  const token = params?.token as string;
  const [session, setSession] = useState<CheckoutSession | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

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
          {session.status === 'pending' && <p className="mt-3 text-sm text-dp-red">Приём платежей временно недоступен.</p>}
          <button type="button" onClick={() => void loadSession()} className="mt-5 text-sm underline">Обновить статус</button>
        </div>
      </div>
    </div>
  );
}

export default function CheckoutPage() {
  return <Suspense fallback={<div className="min-h-screen flex items-center justify-center"><CheckoutLoadingSkeleton /></div>}><CheckoutContent /></Suspense>;
}
