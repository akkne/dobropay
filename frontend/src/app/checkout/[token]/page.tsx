'use client';

import React, { useState, useEffect } from 'react';
import { useParams, useSearchParams } from 'next/navigation';
import { CheckoutHeader } from '../../../components/checkout/CheckoutHeader';
import { OrderSummary } from '../../../components/checkout/OrderSummary';
import { MethodTabs } from '../../../components/checkout/MethodTabs';
import { SbpMethodPanel } from '../../../components/checkout/SbpMethodPanel';
import { BankCardMethodPanel } from '../../../components/checkout/BankCardMethodPanel';
import { CryptoMethodPanel } from '../../../components/checkout/CryptoMethodPanel';
import { ThreeDsModal } from '../../../components/checkout/ThreeDsModal';
import { OutcomeScreen } from '../../../components/checkout/OutcomeScreen';
import { SandboxSimulatorBar } from '../../../components/checkout/SandboxSimulatorBar';
import { Modal } from '../../../components/ui/Modal';
import { Receipt } from '../../../components/ui/Receipt';
import { PaymentMethodType } from '../../../types/PaymentMethodType';
import { PaymentStatus } from '../../../types/PaymentStatus';

function CheckoutContent() {
  const params = useParams();
  const searchParams = useSearchParams();

  const tokenParam = (params?.token as string) || searchParams.get('token') || 'demo-checkout-token-2026';
  const initialTab = (searchParams.get('tab') as PaymentMethodType) || 'sbp';
  const initialStatus = (searchParams.get('status') as PaymentStatus) || 'pending';

  const [activeMethod, setActiveMethod] = useState<PaymentMethodType>(initialTab);
  const [status, setStatus] = useState<PaymentStatus>(initialStatus);
  const [remainingSeconds, setRemainingSeconds] = useState<number>(30 * 60);
  const [is3DsOpen, setIs3DsOpen] = useState(false);
  const [isReceiptOpen, setIsReceiptOpen] = useState(false);
  const [toastMessage, setToastMessage] = useState<string | null>(null);

  const amount = 129900;
  const shopName = 'Магазин мерча Dobropay';
  const orderId = '1042';

  // Timer
  useEffect(() => {
    if (status !== 'pending') return;
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
  }, [status]);

  const showToast = (msg: string) => {
    setToastMessage(msg);
    setTimeout(() => setToastMessage(null), 3000);
  };

  const handleCardSubmit = () => {
    setIs3DsOpen(true);
  };

  const handle3DsSuccess = () => {
    setIs3DsOpen(false);
    showToast('3-D Secure проверка пройдена');
    setTimeout(() => setStatus('succeeded'), 600);
  };

  const handle3DsCancel = () => {
    setIs3DsOpen(false);
    setStatus('canceled');
  };

  const handleSimulate = (outcome: 'succeeded' | 'waiting_for_capture' | 'canceled') => {
    showToast(`Симуляция: ${outcome}`);
    setStatus(outcome);
  };

  const handleReset = () => {
    setStatus('pending');
    setRemainingSeconds(30 * 60);
    showToast('Форма оплаты сброшена');
  };

  const getMethodName = (m: PaymentMethodType) => {
    if (m === 'sbp') return 'СБП (Система быстрых платежей)';
    if (m === 'bank_card') return 'Банковская карта (МИР/Visa/MC)';
    if (m === 'crypto') return 'Криптовалюта (USDT TRC-20)';
    return m;
  };

  return (
    <div className="checkout-page">
      <div className="checkout-wrapper">
        <CheckoutHeader remainingSeconds={remainingSeconds} />

        <main className="checkout-card">
          <OrderSummary shopName={shopName} orderId={orderId} amount={amount} />

          {status === 'pending' ? (
            <div>
              <MethodTabs activeMethod={activeMethod} onChange={setActiveMethod} />

              {activeMethod === 'sbp' && (
                <SbpMethodPanel
                  onSimulateSuccess={() => {
                    showToast('Открытие приложения банка через СБП...');
                    setTimeout(() => setStatus('succeeded'), 2500);
                  }}
                />
              )}

              {activeMethod === 'bank_card' && (
                <BankCardMethodPanel
                  amount={amount}
                  onSubmit={handleCardSubmit}
                />
              )}

              {activeMethod === 'crypto' && (
                <CryptoMethodPanel
                  rubAmount={amount}
                  onCopyText={(text, msg) => {
                    navigator.clipboard?.writeText(text);
                    showToast(msg);
                  }}
                />
              )}
            </div>
          ) : (
            <OutcomeScreen
              status={status}
              token={tokenParam}
              amount={amount}
              methodName={getMethodName(activeMethod)}
              onOpenReceipt={() => setIsReceiptOpen(true)}
              onRetry={handleReset}
            />
          )}
        </main>
      </div>

      <ThreeDsModal
        isOpen={is3DsOpen}
        onSuccess={handle3DsSuccess}
        onCancel={handle3DsCancel}
      />

      <Modal isOpen={isReceiptOpen} onClose={() => setIsReceiptOpen(false)} maxWidth="440px">
        <Receipt
          orderNumber={orderId}
          amount={amount}
          paymentMethodTitle={getMethodName(activeMethod)}
        />
      </Modal>

      <SandboxSimulatorBar onSimulate={handleSimulate} onReset={handleReset} />

      {toastMessage && (
        <div className="toast-container">
          <div className="toast info">{toastMessage}</div>
        </div>
      )}
    </div>
  );
}

export default function CheckoutPage() {
  return (
    <React.Suspense fallback={<div className="p-8 text-center text-slate-500">Загрузка формы оплаты...</div>}>
      <CheckoutContent />
    </React.Suspense>
  );
}
