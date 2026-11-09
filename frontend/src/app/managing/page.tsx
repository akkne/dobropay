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
import { ShopOnboardingForm, ShopOnboardingData } from '../../components/admin/ShopOnboardingForm';
import { CreatePaymentModal, CreatePaymentInput } from '../../components/admin/CreatePaymentModal';
import { AuthModal } from '../../components/admin/AuthModal';
import { Modal } from '../../components/ui/Modal';
import { Receipt } from '../../components/ui/Receipt';
import { apiClient } from '../../services/api';
import { Payment } from '../../types/Payment';
import { WebhookDelivery } from '../../types/WebhookDelivery';
import { FiscalReceipt } from '../../types/FiscalReceipt';
import { ApiKey } from '../../types/ApiKey';
import { Shop } from '../../types/Shop';
import { DomainConstants } from '../../constants/domain';

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
  const [error, setError] = useState<string | null>(null);
  const [selectedPayment, setSelectedPayment] = useState<Payment | null>(null);
  const [isDrawerOpen, setIsDrawerOpen] = useState(false);
  const [isCaptureOpen, setIsCaptureOpen] = useState(false);
  const [isRefundOpen, setIsRefundOpen] = useState(false);
  const [previewReceipt, setPreviewReceipt] = useState<FiscalReceipt | null>(null);
  const [toast, setToast] = useState<string | null>(null);
  const [newKey, setNewKey] = useState<string | null>(null);
  const [isCreatePaymentOpen, setIsCreatePaymentOpen] = useState(false);
  const [createdPaymentUrl, setCreatedPaymentUrl] = useState<string | null>(null);

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

  const createShop = async (name: string) => {
    const created = await apiClient.createShop(name);
    if (created.defaultKey) {
      sessionStorage.setItem(DomainConstants.StorageKeys.PaymentApiKey, created.defaultKey);
      setNewKey(created.defaultKey);
    }
    await loadData();
  };

  return (
    <div className="min-h-screen bg-slate-50/70 flex flex-col text-dp-blue">
      <AdminTopBar
        shopName={shop?.name || 'Dobropay'}
        isAuthenticated={isAuthenticated}
        onOpenLogin={() => { setError(null); setIsAuthOpen(true); }}
        onLogout={() => {
          sessionStorage.removeItem(DomainConstants.StorageKeys.PaymentApiKey);
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
        {isAuthenticated && !shop && <ShopOnboardingForm onSubmit={async (data: ShopOnboardingData) => {
          await createShop(data.name);
          showToast('Магазин создан. Анкета сохранена в этом браузере');
        }} onSkip={async () => {
          await createShop(DomainConstants.DefaultShopName);
          showToast('Магазин создан. Анкету можно заполнить позже');
        }} />}
        {isAuthenticated && shop && activeTab === 'payments' && (
          <div className="space-y-6">
            <KpiSummaryGrid payments={payments} />
            <div className="flex justify-end">
              <button type="button" onClick={() => setIsCreatePaymentOpen(true)} className="rounded-pill bg-dp-blue px-5 py-2.5 text-sm font-semibold text-white hover:bg-blue-800">
                + Создать платёж
              </button>
            </div>
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
            <ApiKeysSection apiKeys={apiKeys} canCreateLiveKey={shop.status === 'approved'} onCreateKey={async (name, mode) => {
              const created = await apiClient.createApiKey(shop.id, name, mode);
              setNewKey(created.apiKey ?? null);
              if (created.apiKey) sessionStorage.setItem(DomainConstants.StorageKeys.PaymentApiKey, created.apiKey);
              await loadData();
            }} onRevokeKey={id => void runAction(() => apiClient.revokeApiKey(shop.id, id), 'Ключ отозван')} />
            <WebhooksSection deliveries={deliveries} />
          </div>
        )}
        {isAuthenticated && shop && activeTab === 'onboarding' && <ShopOnboardingForm onSubmit={async () => {
          showToast('Анкета сохранена в этом браузере');
        }} onSkip={async () => {
          setActiveTab('payments');
        }} />}
      </main>
      <PaymentDrawer isOpen={isDrawerOpen} payment={selectedPayment} onClose={() => setIsDrawerOpen(false)}
        onCapture={() => { setIsDrawerOpen(false); setIsCaptureOpen(true); }}
        onCancel={payment => { setIsDrawerOpen(false); if (shop) void runAction(() => apiClient.cancelPayment(shop.id, payment.id), 'Платёж отменён'); }}
        onRefund={() => { setIsDrawerOpen(false); setIsRefundOpen(true); }} />
      <CreatePaymentModal isOpen={isCreatePaymentOpen} onClose={() => setIsCreatePaymentOpen(false)} onCreate={async (input: CreatePaymentInput) => {
        const apiKey = sessionStorage.getItem(DomainConstants.StorageKeys.PaymentApiKey);
        if (!apiKey) throw new Error('Сначала создайте рабочий API-ключ во вкладке «Разработчикам». Ключ сохранится в этой сессии.');
        const created = await apiClient.createPayment(apiKey, input);
        setIsCreatePaymentOpen(false);
        setCreatedPaymentUrl(created.checkoutUrl);
        await loadData();
      }} />
      <Modal isOpen={!!createdPaymentUrl} onClose={() => setCreatedPaymentUrl(null)}>
        <h2 className="text-lg font-bold text-dp-blue">Ссылка на оплату готова</h2>
        <p className="mt-2 text-sm text-dp-blue/65">Отправьте эту ссылку покупателю.</p>
        <input readOnly value={createdPaymentUrl ?? ''} className="mt-4 w-full select-all rounded-card border border-dp-line bg-slate-50 px-3 py-2.5 font-mono text-xs" />
        <div className="mt-4 flex gap-3">
          <button type="button" onClick={() => { if (createdPaymentUrl) void navigator.clipboard.writeText(new URL(createdPaymentUrl, window.location.origin).toString()); }} className="flex-1 rounded-pill bg-dp-blue px-4 py-2.5 text-sm font-semibold text-white">Скопировать</button>
          {createdPaymentUrl && <a href={createdPaymentUrl} target="_blank" rel="noreferrer" className="flex-1 rounded-pill border border-dp-line px-4 py-2.5 text-center text-sm font-semibold text-dp-blue">Открыть</a>}
        </div>
      </Modal>
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
