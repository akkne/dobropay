'use client';

import React, { useState, useEffect } from 'react';
import { useParams, useRouter, useSearchParams } from 'next/navigation';
import { apiClient } from '../../../../services/api';
import { CheckoutSession } from '../../../../types/CheckoutSession';
import { PaymentMethodType } from '../../../../types/PaymentMethodType';
import { formatMoney } from '../../../../utils/formatters';
import { Button } from '../../../../components/ui/Button';

export default function MockPaymentSimulatorPage() {
  const params = useParams();
  const searchParams = useSearchParams();
  const router = useRouter();

  const tokenParam = (params?.token as string) || 'demo-checkout-token-2026';
  const methodParam = (searchParams.get('method') as PaymentMethodType) || 'bank_card';

  const [session, setSession] = useState<CheckoutSession | null>(null);
  const [isLoading, setIsLoading] = useState<boolean>(true);
  const [isProcessing, setIsProcessing] = useState<boolean>(false);
  const [processingStatus, setProcessingStatus] = useState<string>('');
  const [selectedBank, setSelectedBank] = useState<string>('Добропей Банк');
  const [smsCode, setSmsCode] = useState<string>('');

  useEffect(() => {
    async function fetchSession() {
      try {
        const data = await apiClient.getCheckoutSession(tokenParam);
        setSession(data);
      } catch {
        // Fallback when no payment session is available.
        setSession({
          token: tokenParam,
          paymentId: 'demo-payment-id',
          shopId: 'demo-shop-id',
          shopName: 'Магазин',
          orderId: 'ORD-98721',
          amount: 1500,
          currency: 'RUB',
          status: 'pending',
          paymentMethodType: methodParam,
          capture: true,
          expiresAt: new Date(Date.now() + 30 * 60 * 1000).toISOString(),
          createdAt: new Date().toISOString(),
          description: 'Оплата заказа'
        });
      } finally {
        setIsLoading(false);
      }
    }
    fetchSession();
  }, [tokenParam, methodParam]);

  const handleSimulateSuccess = async () => {
    if (!session) return;
    setIsProcessing(true);
    setProcessingStatus('Связь с банком-эмитентом и подтверждение транзакции...');

    try {
      await apiClient.simulateCheckoutOutcome(tokenParam, 'succeeded');
      setTimeout(() => {
        setProcessingStatus('Транзакция успешно авторизована! Возврат в магазин...');
        setTimeout(() => {
          router.push(`/checkout/${tokenParam}`);
        }, 800);
      }, 1000);
    } catch {
      router.push(`/checkout/${tokenParam}`);
    }
  };

  const handleSimulateDecline = async () => {
    if (!session) return;
    setIsProcessing(true);
    setProcessingStatus('Отклонение платежа по запросу пользователя...');

    try {
      await apiClient.simulateCheckoutOutcome(tokenParam, 'canceled');
      setTimeout(() => {
        router.push(`/checkout/${tokenParam}`);
      }, 800);
    } catch {
      router.push(`/checkout/${tokenParam}`);
    }
  };

  if (isLoading || !session) {
    return (
      <div className="min-h-screen bg-slate-50 flex items-center justify-center p-4">
        <div className="bg-white p-6 rounded-2xl shadow-sm border border-slate-200 text-center max-w-sm w-full">
          <div className="w-8 h-8 border-2 border-dp-blue border-t-transparent rounded-full animate-spin mx-auto mb-3" />
          <p className="text-sm font-medium text-slate-600">Загрузка платежного шлюза...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gradient-to-b from-slate-100 to-slate-200 flex flex-col items-center justify-center p-4">
      <div className="w-full max-w-md bg-white rounded-2xl shadow-xl border border-slate-200 overflow-hidden">
        {/* Header */}
        <div className="bg-slate-900 text-white px-6 py-5 flex items-center justify-between">
          <div className="flex items-center gap-2.5">
            <div className="w-7 h-7 rounded-lg bg-dp-pink flex items-center justify-center font-bold text-white text-xs">
              DP
            </div>
            <div>
              <div className="text-sm font-bold tracking-tight">Dobropay</div>
              <div className="text-[11px] text-slate-400">Подтверждение оплаты</div>
            </div>
          </div>
          <span className="px-2 py-0.5 rounded text-[10px] font-semibold bg-emerald-500/20 text-emerald-300 border border-emerald-500/30">Оплата</span>
        </div>

        {/* Order Details Header */}
        <div className="px-6 py-4 bg-slate-50 border-b border-slate-100 flex items-center justify-between">
          <div>
            <div className="text-xs text-slate-500 font-medium">{session.shopName}</div>
            <div className="text-xs text-slate-400">Заказ #{session.orderId}</div>
          </div>
          <div className="text-right">
            <div className="text-lg font-bold text-slate-900">{formatMoney(session.amount)}</div>
            <div className="text-[10px] text-slate-400">
              {session.capture ? 'Прямое списание' : 'Холдирование средств'}
            </div>
          </div>
        </div>

        {/* Main interactive area */}
        <div className="p-6 space-y-6">
          {isProcessing ? (
            <div className="py-12 text-center space-y-4">
              <div className="w-12 h-12 border-3 border-dp-blue border-t-transparent rounded-full animate-spin mx-auto" />
              <div className="text-sm font-semibold text-slate-800">{processingStatus}</div>
              <div className="text-xs text-slate-400">Пожалуйста, не закрывайте страницу</div>
            </div>
          ) : (
            <>
              {methodParam === 'bank_card' ? (
                <div className="space-y-4">
                  <div className="bg-gradient-to-tr from-slate-800 to-slate-900 rounded-xl p-5 text-white shadow-md relative overflow-hidden">
                    <div className="absolute right-3 top-3 text-[11px] font-mono text-slate-400">
                      3-D Secure 2.0
                    </div>
                    <div className="text-xs text-slate-400 uppercase tracking-wider mb-3">Банковская карта</div>
                    <div className="font-mono text-base tracking-widest mb-4">•••• •••• •••• 4242</div>
                    <div className="flex items-center justify-between text-xs text-slate-300">
                      <span>DOBROPAY CLIENT</span>
                      <span>12/28</span>
                    </div>
                  </div>

                  <div className="bg-slate-50 border border-slate-200 rounded-xl p-4 space-y-2">
                    <label className="text-xs font-semibold text-slate-700 block">
                      Код подтверждения из SMS
                    </label>
                    <div className="flex gap-2">
                      <input
                        type="text"
                        value={smsCode}
                        onChange={(e) => setSmsCode(e.target.value)}
                        placeholder="Код из SMS"
                        className="w-full px-3 py-2 text-sm font-mono tracking-widest text-center rounded-lg border border-slate-300 bg-white text-slate-900 focus:outline-none focus:ring-2 focus:ring-dp-blue"
                      />
                    </div>
                    <p className="text-[11px] text-slate-500">
                      Введите код, который отправил банк.
                    </p>
                  </div>
                </div>
              ) : (
                <div className="space-y-4">
                  <div className="border border-slate-200 rounded-xl p-4 bg-slate-50 space-y-3">
                    <div className="flex items-center gap-2 text-slate-800 font-semibold text-xs">
                      <span className="w-2.5 h-2.5 rounded-full bg-dp-pink" />
                      Система быстрых платежей (СБП)
                    </div>
                    <p className="text-xs text-slate-600">
                      Выберите банк для подтверждения перевода через мобильное приложение:
                    </p>
                    <select
                      value={selectedBank}
                      onChange={(e) => setSelectedBank(e.target.value)}
                      className="w-full px-3 py-2 text-xs rounded-lg border border-slate-300 bg-white text-slate-800 font-medium focus:outline-none focus:ring-2 focus:ring-dp-blue"
                    >
                      <option value="Добропей Банк">Добропей Банк (0% комиссии)</option>
                      <option value="Сбербанк">Сбербанк Онлайн</option>
                      <option value="Т-Банк">Т-Банк</option>
                      <option value="ВТБ">ВТБ Онлайн</option>
                      <option value="Альфа-Банк">Альфа-Банк</option>
                    </select>
                  </div>
                </div>
              )}

              {/* Action buttons */}
              <div className="space-y-2.5 pt-2">
                <Button
                  variant="primary-blue"
                  size="lg"
                  full
                  onClick={handleSimulateSuccess}
                >
                  <svg className="w-4 h-4 mr-1.5" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5">
                    <polyline points="20 6 9 17 4 12" />
                  </svg>
                  <span>Подтвердить оплату {formatMoney(session.amount)}</span>
                </Button>

                <button
                  type="button"
                  onClick={handleSimulateDecline}
                  className="w-full py-2.5 px-4 rounded-pill text-xs font-semibold text-rose-700 bg-rose-50 hover:bg-rose-100 border border-rose-200 transition-colors flex items-center justify-center gap-1.5"
                >
                  <svg className="w-4 h-4 text-rose-600" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5">
                    <line x1="18" y1="6" x2="6" y2="18" />
                    <line x1="6" y1="6" x2="18" y2="18" />
                  </svg>
                  <span>Отклонить платеж (Отказ банка / Отмена)</span>
                </button>

                <div className="text-center pt-2">
                  <button
                    type="button"
                    onClick={() => router.push(`/checkout/${tokenParam}`)}
                    className="text-xs text-slate-500 hover:text-slate-800 transition-colors underline"
                  >
                    ← Вернуться в форму оплаты
                  </button>
                </div>
              </div>
            </>
          )}
        </div>

        {/* Footer */}
        <div className="px-6 py-3 bg-slate-50 border-t border-slate-100 flex items-center justify-between text-[11px] text-slate-400">
          <span>Безопасное соединение</span>
          <span>Dobropay</span>
        </div>
      </div>
    </div>
  );
}
