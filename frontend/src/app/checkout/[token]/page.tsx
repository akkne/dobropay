'use client';

import React, { useState, useEffect, useCallback, Suspense } from 'react';
import { useParams, useRouter, useSearchParams } from 'next/navigation';
import { CheckoutHeader } from '../../../components/checkout/CheckoutHeader';
import { OrderSummary } from '../../../components/checkout/OrderSummary';
import { MethodTabs } from '../../../components/checkout/MethodTabs';
import { SbpMethodPanel } from '../../../components/checkout/SbpMethodPanel';
import { BankCardMethodPanel } from '../../../components/checkout/BankCardMethodPanel';
import { OutcomeScreen } from '../../../components/checkout/OutcomeScreen';
import { CheckoutLoadingSkeleton } from '../../../components/checkout/CheckoutLoadingSkeleton';
import { CheckoutErrorState } from '../../../components/checkout/CheckoutErrorState';
import { Modal } from '../../../components/ui/Modal';
import { Receipt } from '../../../components/ui/Receipt';
import { apiClient } from '../../../services/api';
import { CheckoutSession } from '../../../types/CheckoutSession';
import { PaymentMethodType } from '../../../types/PaymentMethodType';
import { PaymentStatus } from '../../../types/PaymentStatus';

function CheckoutContent() {
  const params = useParams();
  const searchParams = useSearchParams();
  const router = useRouter();

  const tokenParam = (params?.token as string) || searchParams.get('token') || 'demo-checkout-token-2026';
  const queryTab = searchParams.get('tab') as PaymentMethodType | null;
  const queryStatus = searchParams.get('status') as PaymentStatus | null;

  const [session, setSession] = useState<CheckoutSession | null>(null);
  const [isLoading, setIsLoading] = useState<boolean>(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const [activeMethod, setActiveMethod] = useState<PaymentMethodType>(queryTab || 'sbp');
  const [status, setStatus] = useState<PaymentStatus>(queryStatus || 'pending');
  const [remainingSeconds, setRemainingSeconds] = useState<number>(30 * 60);
  const [isReceiptOpen, setIsReceiptOpen] = useState(false);
  const [toastMessage, setToastMessage] = useState<string | null>(null);

  const loadSession = useCallback(async () => {
    setIsLoading(true);
    setErrorMessage(null);
    try {
      const data = await apiClient.getCheckoutSession(tokenParam);
      setSession(data);
      if (queryStatus) {
        setStatus(queryStatus);
      } else {
        setStatus(data.status);
      }
      if (queryTab) {
        setActiveMethod(queryTab);
      } else if (data.paymentMethodType) {
        setActiveMethod(data.paymentMethodType);
      }

      const diff = Math.max(0, Math.floor((new Date(data.expiresAt).getTime() - Date.now()) / 1000));
      setRemainingSeconds(diff > 0 ? diff : 30 * 60);
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Не удалось загрузить данные сессии оплаты.';
      setErrorMessage(msg);
    } finally {
      setIsLoading(false);
    }
  }, [tokenParam, queryTab, queryStatus]);

  useEffect(() => {
    loadSession();
  }, [loadSession]);

  // Countdown timer
  useEffect(() => {
    if (status !== 'pending' || remainingSeconds <= 0) return;
    const interval = setInterval(() => {
      setRemainingSeconds((prev) => {
        if (prev <= 1) {
          clearInterval(interval);
          setStatus('canceled');
          return 0;
        }
        return prev - 1;
      });
    }, 1000);
    return () => clearInterval(interval);
  }, [status, remainingSeconds]);

  const showToast = (msg: string) => {
    setToastMessage(msg);
    setTimeout(() => setToastMessage(null), 3000);
  };

  const handleCardSubmit = async (cardData: { number: string; expiry: string; cvv: string; saveCard: boolean }) => {
    try {
      const res = await apiClient.startCheckoutSession(tokenParam, 'bank_card', cardData);
      const targetUrl = res.confirmationUrl || `/checkout/${tokenParam}/simulator?method=bank_card`;
      router.push(targetUrl);
    } catch {
      router.push(`/checkout/${tokenParam}/simulator?method=bank_card`);
    }
  };

  const handleSbpPay = async () => {
    try {
      const res = await apiClient.startCheckoutSession(tokenParam, 'sbp');
      const targetUrl = res.confirmationUrl || `/checkout/${tokenParam}/simulator?method=sbp`;
      router.push(targetUrl);
    } catch {
      router.push(`/checkout/${tokenParam}/simulator?method=sbp`);
    }
  };

  if (isLoading) {
    return (
      <div className="min-h-screen bg-gradient-to-b from-dp-tint to-white flex items-center justify-center p-4">
        <CheckoutLoadingSkeleton />
      </div>
    );
  }

  if (errorMessage || !session) {
    return (
      <div className="min-h-screen bg-gradient-to-b from-dp-tint to-white flex items-center justify-center p-4">
        <CheckoutErrorState
          message={errorMessage || 'Сессия оплаты не найдена.'}
          onRetry={loadSession}
        />
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gradient-to-b from-dp-tint to-white flex flex-col items-center justify-start px-4 py-8 md:py-12 pb-24">
      <div className="w-full max-w-[500px] flex flex-col gap-4">
        <CheckoutHeader remainingSeconds={remainingSeconds} />

        <OrderSummary
          shopName={session.shopName}
          orderId={session.orderId}
          amount={session.amount}
        />

        <main className="bg-white border border-dp-line rounded-card p-6 shadow-card transition-all">
          {status === 'pending' ? (
            <div className="space-y-5">
              <MethodTabs activeMethod={activeMethod} onChange={setActiveMethod} />

              {activeMethod === 'sbp' && <SbpMethodPanel onPay={handleSbpPay} />}
              {activeMethod === 'bank_card' && (
                <BankCardMethodPanel amount={session.amount} onSubmit={handleCardSubmit} />
              )}
            </div>
          ) : (
            <OutcomeScreen
              status={status}
              amount={session.amount}
              onViewReceipt={() => setIsReceiptOpen(true)}
              onRetry={() => {
                setStatus('pending');
                setRemainingSeconds(30 * 60);
              }}
              onReturnToShop={() => {
                showToast('Перенаправление в магазин...');
              }}
            />
          )}
        </main>
      </div>

      <Modal
        isOpen={isReceiptOpen}
        onClose={() => setIsReceiptOpen(false)}
        title="Электронный фискальный чек"
      >
        <Receipt
          orderNumber={session.orderId}
          amount={session.amount}
          legalEntity={session.shopName}
          createdAt={session.createdAt}
          paymentMethodTitle={session.paymentMethodType === 'sbp' ? 'СБП' : 'Банковская карта'}
        />
      </Modal>

      {toastMessage && (
        <div className="fixed bottom-20 left-1/2 -translate-x-1/2 bg-dp-blue text-white px-4 py-2 rounded-pill text-xs shadow-lg font-medium animate-fadeIn z-50">
          {toastMessage}
        </div>
      )}
    </div>
  );
}

export default function CheckoutPage() {
  return (
    <Suspense
      fallback={
        <div className="min-h-screen bg-gradient-to-b from-dp-tint to-white flex items-center justify-center p-4">
          <CheckoutLoadingSkeleton />
        </div>
      }
    >
      <CheckoutContent />
    </Suspense>
  );
}
