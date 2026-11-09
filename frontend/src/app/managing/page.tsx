'use client';

import React, { useState, useEffect, useCallback } from 'react';
import { useSearchParams } from 'next/navigation';
import { AdminTopBar } from '../../components/admin/AdminTopBar';
import { AdminNavBar, AdminTab } from '../../components/admin/AdminNavBar';
import { KpiSummaryGrid } from '../../components/admin/KpiSummaryGrid';
import { PaymentsTable } from '../../components/admin/PaymentsTable';
import { PaymentDrawer } from '../../components/admin/PaymentDrawer';
import { CaptureModal } from '../../components/admin/CaptureModal';
import { RefundModal } from '../../components/admin/RefundModal';
import { ApiKeysSection } from '../../components/admin/ApiKeysSection';
import { WebhooksSection } from '../../components/admin/WebhooksSection';
import { ReceiptsTable } from '../../components/admin/ReceiptsTable';
import { AuthModal } from '../../components/admin/AuthModal';
import { Modal } from '../../components/ui/Modal';
import { Receipt } from '../../components/ui/Receipt';
import { apiClient } from '../../services/api';
import { Payment } from '../../types/Payment';
import { WebhookDelivery } from '../../types/WebhookDelivery';
import { FiscalReceipt } from '../../types/FiscalReceipt';
import { ApiKey } from '../../types/ApiKey';
import { Shop } from '../../types/Shop';

function AdminContent() {
  const searchParams = useSearchParams();
  const [activeTab, setActiveTab] = useState<AdminTab>((searchParams.get('tab') as AdminTab) || 'payments');
  const [shop, setShop] = useState<Shop | null>(null);
  const [payments, setPayments] = useState<Payment[]>([]);
  const [deliveries, setDeliveries] = useState<WebhookDelivery[]>([]);
  const [receipts, setReceipts] = useState<FiscalReceipt[]>([]);
  const [apiKeys, setApiKeys] = useState<ApiKey[]>([]);
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [isAuthOpen, setIsAuthOpen] = useState(false);
  const [shopName, setShopName] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [selectedPayment, setSelectedPayment] = useState<Payment | null>(null);
  const [isDrawerOpen, setIsDrawerOpen] = useState(false);
  const [isCaptureOpen, setIsCaptureOpen] = useState(false);
  const [isRefundOpen, setIsRefundOpen] = useState(false);
  const [previewReceipt, setPreviewReceipt] = useState<FiscalReceipt | null>(null);
  const [toast, setToast] = useState<string | null>(null);
  const [newKey, setNewKey] = useState<string | null>(null);

  const showToast = (message: string) => {
    setToast(message);
    setTimeout(() => setToast(null), 4000);
  };

  const loadData = useCallback(async () => {
    if (!apiClient.getToken()) return;
    try {
      setError(null);
      const shops = await apiClient.getShops();
      const selected = shops[0] ?? null;
      setShop(selected);
      if (!selected) return;
      const [nextPayments, nextDeliveries, nextReceipts, nextKeys] = await Promise.all([
        apiClient.getPayments(selected.id),
        apiClient.getDeliveries(selected.id),
        apiClient.getReceipts(selected.id),
        apiClient.getApiKeys(selected.id)
      ]);
      setPayments(nextPayments);
      setDeliveries(nextDeliveries);
      setReceipts(nextReceipts);
      setApiKeys(nextKeys);
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : 'Не удалось загрузить данные');
    }
  }, []);

  useEffect(() => {
    const authenticated = !!apiClient.getToken();
    setIsAuthenticated(authenticated);
    if (authenticated) void loadData();
    else setIsAuthOpen(true);
  }, [loadData]);

  const runAction = async (action: () => Promise<unknown>, success: string) => {
    try {
      await action();
      await loadData();
      showToast(success);
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : 'Операция не выполнена');
    }
  };

  return (
    <div className="min-h-screen bg-slate-50/70 flex flex-col text-dp-blue">
      <AdminTopBar
        shopName={shop?.name || 'Dobropay'}
        isAuthenticated={isAuthenticated}
        onOpenLogin={() => { setError(null); setIsAuthOpen(true); }}
        onLogout={() => {
          void apiClient.logout().catch(() => {
            setError('Сервер не подтвердил выход. Локальная сессия закрыта.');
          }).finally(() => {
            setIsAuthenticated(false);
            setShop(null);
            setPayments([]);
            setDeliveries([]);
            setReceipts([]);
            setApiKeys([]);
          });
        }}
      />
      {isAuthenticated && shop && <AdminNavBar activeTab={activeTab} onChange={setActiveTab} />}
      <main className="flex-1 w-full max-w-6xl mx-auto px-4 sm:px-6 lg:px-8 py-6 space-y-6">
        {error && <div role="alert" className="rounded-card border border-dp-red p-4 text-dp-red">{error} <button type="button" onClick={() => void loadData()} className="underline">Повторить</button></div>}
        {!isAuthenticated && <p>Войдите в личный кабинет, чтобы увидеть данные магазина.</p>}
        {isAuthenticated && !shop && (
          <form onSubmit={async event => {
            event.preventDefault();
            await runAction(() => apiClient.createShop(shopName), 'Магазин создан');
          }} className="max-w-md bg-white p-6 rounded-card border border-dp-line space-y-4">
            <h2 className="font-bold">Создать магазин</h2>
            <input required value={shopName} onChange={event => setShopName(event.target.value)} placeholder="Название магазина" className="w-full border border-dp-line rounded-card p-3" />
            <button type="submit" className="bg-dp-blue text-white rounded-pill px-4 py-2">Создать</button>
          </form>
        )}
        {isAuthenticated && shop && activeTab === 'payments' && (
          <div className="space-y-6">
            <KpiSummaryGrid payments={payments} />
            <PaymentsTable payments={payments}
              onSelectPayment={payment => { setSelectedPayment(payment); setIsDrawerOpen(true); }}
              onCapture={payment => { setSelectedPayment(payment); setIsCaptureOpen(true); }}
              onCancel={payment => void runAction(() => apiClient.cancelPayment(shop.id, payment.id), 'Платёж отменён')}
              onRefund={payment => { setSelectedPayment(payment); setIsRefundOpen(true); }} />
          </div>
        )}
        {isAuthenticated && shop && activeTab === 'receipts' && <ReceiptsTable receipts={receipts} onPreviewReceipt={setPreviewReceipt} />}
        {isAuthenticated && shop && activeTab === 'developer' && (
          <div className="space-y-6">
            <ApiKeysSection apiKeys={apiKeys} onCreateKey={async name => {
              try {
                const created = await apiClient.createApiKey(shop.id, name);
                setNewKey(created.apiKey ?? null);
                await loadData();
              } catch (cause) {
                setError(cause instanceof Error ? cause.message : 'Не удалось создать ключ');
              }
            }} onRevokeKey={id => void runAction(() => apiClient.revokeApiKey(shop.id, id), 'Ключ отозван')} />
            <WebhooksSection deliveries={deliveries} />
          </div>
        )}
        {isAuthenticated && shop && activeTab === 'onboarding' && (
          <p>Статус проверки магазина: {shop.status}. Для обновления реквизитов используйте API подключения.</p>
        )}
      </main>
      <PaymentDrawer isOpen={isDrawerOpen} payment={selectedPayment} onClose={() => setIsDrawerOpen(false)}
        onCapture={() => { setIsDrawerOpen(false); setIsCaptureOpen(true); }}
        onCancel={payment => { setIsDrawerOpen(false); if (shop) void runAction(() => apiClient.cancelPayment(shop.id, payment.id), 'Платёж отменён'); }}
        onRefund={() => { setIsDrawerOpen(false); setIsRefundOpen(true); }} />
      <CaptureModal payment={selectedPayment} isOpen={isCaptureOpen} onClose={() => setIsCaptureOpen(false)}
        onConfirm={amount => {
          if (shop && selectedPayment) void runAction(() => apiClient.capturePayment(shop.id, selectedPayment.id, amount), 'Списание подтверждено');
          setIsCaptureOpen(false);
        }} />
      <RefundModal payment={selectedPayment} isOpen={isRefundOpen} onClose={() => setIsRefundOpen(false)}
        onConfirm={(amount, reason) => {
          if (shop && selectedPayment) void runAction(() => apiClient.refundPayment(shop.id, selectedPayment.id, amount, reason), 'Возврат создан');
          setIsRefundOpen(false);
        }} />
      <Modal isOpen={!!previewReceipt} onClose={() => setPreviewReceipt(null)} maxWidth="420px">
        {previewReceipt && <Receipt orderNumber={previewReceipt.orderNumber} amount={previewReceipt.amount}
          fnNumber={previewReceipt.fnNumber} fdNumber={previewReceipt.fdNumber} fpdNumber={previewReceipt.fpdNumber} createdAt={previewReceipt.createdAt} />}
      </Modal>
      <Modal isOpen={!!newKey} onClose={() => setNewKey(null)}>
        <h2 className="font-bold mb-3">Новый API-ключ</h2>
        <p className="text-xs mb-3">Сохраните ключ сейчас: повторно он не отображается.</p>
        <code className="break-all select-all">{newKey}</code>
      </Modal>
      <AuthModal isOpen={isAuthOpen} error={error} onClose={() => setIsAuthOpen(false)} onRegister={async (email, password) => {
        try {
          await apiClient.register(email, password);
          setIsAuthenticated(true);
          setIsAuthOpen(false);
          await loadData();
        } catch (cause) {
          setError(cause instanceof Error ? cause.message : 'Не удалось зарегистрироваться');
        }
      }} onLogin={async (email, password) => {
        try {
          await apiClient.login(email, password);
          setIsAuthenticated(true);
          setIsAuthOpen(false);
          await loadData();
        } catch (cause) {
          setError(cause instanceof Error ? cause.message : 'Не удалось войти');
        }
      }} />
      {toast && <div className="fixed bottom-6 right-6 z-50 bg-dp-blue text-white px-4 py-2 rounded-pill">{toast}</div>}
    </div>
  );
}

export default function AdminPage() {
  return <React.Suspense fallback={<div className="p-8">Загрузка...</div>}><AdminContent /></React.Suspense>;
}
