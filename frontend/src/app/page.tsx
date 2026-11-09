'use client';

import React, { useState, useEffect, useCallback } from 'react';
import { useSearchParams } from 'next/navigation';
import { AdminTopBar } from '../components/admin/AdminTopBar';
import { AdminNavBar, AdminTab } from '../components/admin/AdminNavBar';
import { KpiSummaryGrid } from '../components/admin/KpiSummaryGrid';
import { PaymentsTable } from '../components/admin/PaymentsTable';
import { PaymentDrawer } from '../components/admin/PaymentDrawer';
import { CaptureModal } from '../components/admin/CaptureModal';
import { RefundModal } from '../components/admin/RefundModal';
import { CreatePaymentModal } from '../components/admin/CreatePaymentModal';
import { ApiKeysSection } from '../components/admin/ApiKeysSection';
import { WebhooksSection } from '../components/admin/WebhooksSection';
import { WebhookInspectorModal } from '../components/admin/WebhookInspectorModal';
import { ReceiptsTable } from '../components/admin/ReceiptsTable';
import { OnboardingWizard } from '../components/admin/OnboardingWizard';
import { AuthModal } from '../components/admin/AuthModal';
import { Modal } from '../components/ui/Modal';
import { Receipt } from '../components/ui/Receipt';
import { Button } from '../components/ui/Button';

import { apiClient } from '../services/api';
import { Payment } from '../types/Payment';
import { WebhookDelivery } from '../types/WebhookDelivery';
import { FiscalReceipt } from '../types/FiscalReceipt';
import { ApiKey } from '../types/ApiKey';
import { Shop } from '../types/Shop';
import { PaymentMethodType } from '../types/PaymentMethodType';

function AdminContent() {
  const searchParams = useSearchParams();
  const queryTab = (searchParams.get('tab') as AdminTab) || 'payments';

  const [activeTab, setActiveTab] = useState<AdminTab>(queryTab);
  const [shop, setShop] = useState<Shop | null>(null);
  const [payments, setPayments] = useState<Payment[]>([]);
  const [deliveries, setDeliveries] = useState<WebhookDelivery[]>([]);
  const [receipts, setReceipts] = useState<FiscalReceipt[]>([]);
  const [apiKeys, setApiKeys] = useState<ApiKey[]>([]);
  const [isAuthenticated, setIsAuthenticated] = useState<boolean>(false);

  // Modals state
  const [selectedPayment, setSelectedPayment] = useState<Payment | null>(null);
  const [isDrawerOpen, setIsDrawerOpen] = useState(false);
  const [isCaptureOpen, setIsCaptureOpen] = useState(false);
  const [isRefundOpen, setIsRefundOpen] = useState(false);
  const [isCreatePaymentOpen, setIsCreatePaymentOpen] = useState(false);
  const [createdCheckoutUrl, setCreatedCheckoutUrl] = useState<string | null>(null);
  const [inspectDelivery, setInspectDelivery] = useState<WebhookDelivery | null>(null);
  const [previewReceipt, setPreviewReceipt] = useState<FiscalReceipt | null>(null);
  const [isAuthOpen, setIsAuthOpen] = useState(false);
  const [toastMessage, setToastMessage] = useState<string | null>(null);

  const showToast = (msg: string) => {
    setToastMessage(msg);
    setTimeout(() => setToastMessage(null), 3000);
  };

  const loadData = useCallback(async () => {
    try {
      const shopData = await apiClient.getShop('shop-1');
      setShop(shopData);

      const paymentsData = await apiClient.getPayments('shop-1');
      setPayments(paymentsData);

      const deliveriesData = await apiClient.getDeliveries('shop-1');
      setDeliveries(deliveriesData);

      const receiptsData = await apiClient.getReceipts('shop-1');
      setReceipts(receiptsData);

      const keysData = await apiClient.getApiKeys('shop-1');
      setApiKeys(keysData);
    } catch {
      showToast('Ошибка загрузки данных');
    }
  }, []);

  useEffect(() => {
    loadData();
    setIsAuthenticated(!!apiClient.getToken());
  }, [loadData]);

  const handleCaptureConfirm = async (kopecks: number) => {
    if (!selectedPayment) return;
    try {
      await apiClient.capturePayment('shop-1', selectedPayment.id, kopecks);
      setIsCaptureOpen(false);
      await loadData();
      showToast(`Холд успешно списан на сумму ${(kopecks / 100).toFixed(2)} ₽!`);
    } catch {
      showToast('Ошибка при подтверждении списания');
    }
  };

  const handleCancelPayment = async (payment: Payment) => {
    try {
      await apiClient.cancelPayment('shop-1', payment.id, 'merchant_canceled');
      setIsDrawerOpen(false);
      await loadData();
      showToast('Платёж успешно отменён');
    } catch {
      showToast('Ошибка при отмене платежа');
    }
  };

  const handleRefundConfirm = async (kopecks: number, reason: string) => {
    if (!selectedPayment) return;
    try {
      await apiClient.refundPayment('shop-1', selectedPayment.id, kopecks, reason);
      setIsRefundOpen(false);
      await loadData();
      showToast(`Возврат на сумму ${(kopecks / 100).toFixed(2)} ₽ успешно оформлен!`);
    } catch {
      showToast('Ошибка при оформлении возврата');
    }
  };

  const handleCreatePaymentSubmit = (data: {
    amountKopecks: number;
    orderId: string;
    method: PaymentMethodType;
    capture: boolean;
  }) => {
    const newId = `pay-${Date.now()}`;
    const token = `tok-${Date.now()}`;
    const checkoutUrl = `/checkout/${token}`;

    const newPayment: Payment = {
      id: newId,
      shopId: 'shop-1',
      orderId: data.orderId,
      externalOrderId: data.orderId,
      amount: data.amountKopecks,
      currency: 'RUB',
      status: 'pending',
      paymentMethodType: data.method,
      capture: data.capture,
      checkoutUrl,
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
      refunds: []
    };

    apiClient.addPayment(newPayment);
    setIsCreatePaymentOpen(false);
    loadData();
    setCreatedCheckoutUrl(checkoutUrl);
  };

  return (
    <div className="min-h-screen bg-slate-50/70 flex flex-col text-dp-blue">
      <AdminTopBar
        shopName={shop?.name || 'Магазин мерча Dobropay'}
        isAuthenticated={isAuthenticated}
        onOpenLogin={() => setIsAuthOpen(true)}
        onLogout={() => {
          apiClient.setToken(null);
          setIsAuthenticated(false);
          showToast('Вы вышли из личного кабинета');
        }}
      />

      <AdminNavBar activeTab={activeTab} onChange={setActiveTab} />

      <main className="flex-1 w-full max-w-6xl mx-auto px-4 sm:px-6 lg:px-8 py-6 space-y-6">
        {/* VIEW 1: PAYMENTS */}
        {activeTab === 'payments' && (
          <div className="space-y-6">
            <KpiSummaryGrid payments={payments} />

            <PaymentsTable
              payments={payments}
              onCreatePayment={() => setIsCreatePaymentOpen(true)}
              onSelectPayment={(p) => {
                setSelectedPayment(p);
                setIsDrawerOpen(true);
              }}
              onCapture={(p) => {
                setSelectedPayment(p);
                setIsCaptureOpen(true);
              }}
              onCancel={handleCancelPayment}
              onRefund={(p) => {
                setSelectedPayment(p);
                setIsRefundOpen(true);
              }}
            />
          </div>
        )}

        {/* VIEW 2: RECEIPTS 54-FZ */}
        {activeTab === 'receipts' && (
          <ReceiptsTable
            receipts={receipts}
            onPreviewReceipt={(r) => setPreviewReceipt(r)}
          />
        )}

        {/* VIEW 3: DEVELOPER PORTAL */}
        {activeTab === 'developer' && (
          <div className="space-y-6">
            <ApiKeysSection
              apiKeys={apiKeys}
              onCreateKey={async (name) => {
                await apiClient.createApiKey('shop-1', name);
                await loadData();
                showToast(`Ключ "${name}" успешно создан!`);
              }}
              onRevokeKey={async (id) => {
                await apiClient.revokeApiKey('shop-1', id);
                await loadData();
                showToast('API-ключ отозван');
              }}
            />

            <WebhooksSection
              deliveries={deliveries}
              onSaveConfig={async (url, signingSecret, events) => {
                await apiClient.updateWebhookConfig('shop-1', { url, signingSecret, events });
                showToast('Настройки Webhook сохранены!');
              }}
              onInspectDelivery={(d) => setInspectDelivery(d)}
              onRetryDelivery={() => {
                showToast('Повторная отправка Webhook запущена...');
                setTimeout(() => showToast('Webhook успешно доставлен (HTTP 200)'), 1000);
              }}
            />
          </div>
        )}

        {/* VIEW 4: ONBOARDING */}
        {activeTab === 'onboarding' && (
          <OnboardingWizard
            onComplete={() => {
              showToast('Заявка принята в работу!');
              setActiveTab('payments');
            }}
          />
        )}
      </main>

      {/* DRAWERS & MODALS */}
      <PaymentDrawer
        isOpen={isDrawerOpen}
        payment={selectedPayment}
        onClose={() => setIsDrawerOpen(false)}
        onCapture={(p) => {
          setIsDrawerOpen(false);
          setIsCaptureOpen(true);
        }}
        onCancel={handleCancelPayment}
        onRefund={(p) => {
          setIsDrawerOpen(false);
          setIsRefundOpen(true);
        }}
      />

      <CaptureModal
        payment={selectedPayment}
        isOpen={isCaptureOpen}
        onClose={() => setIsCaptureOpen(false)}
        onConfirm={handleCaptureConfirm}
      />

      <RefundModal
        payment={selectedPayment}
        isOpen={isRefundOpen}
        onClose={() => setIsRefundOpen(false)}
        onConfirm={handleRefundConfirm}
      />

      <CreatePaymentModal
        isOpen={isCreatePaymentOpen}
        onClose={() => setIsCreatePaymentOpen(false)}
        onCreate={handleCreatePaymentSubmit}
      />

      {/* Modal: Payment Created */}
      <Modal isOpen={!!createdCheckoutUrl} onClose={() => setCreatedCheckoutUrl(null)}>
        <div className="text-center space-y-4">
          <div className="text-4xl">🎉</div>
          <h3 className="text-base font-bold text-dp-blue">
            Платёжная ссылка готова!
          </h3>
          <p className="text-xs text-dp-blue/70">
            Отправьте эту ссылку клиенту или перейдите для проверки Hosted Checkout
          </p>

          <input
            type="text"
            className="w-full px-3 py-2 text-xs font-mono rounded-card border border-dp-line bg-dp-tint select-all"
            readOnly
            value={createdCheckoutUrl ? `${typeof window !== 'undefined' ? window.location.origin : ''}${createdCheckoutUrl}` : ''}
          />

          <div className="flex gap-2 pt-2">
            <Button
              variant="primary-blue"
              size="sm"
              full
              onClick={() => {
                if (createdCheckoutUrl) {
                  navigator.clipboard?.writeText(`${window.location.origin}${createdCheckoutUrl}`);
                  showToast('Ссылка скопирована!');
                }
              }}
            >
              Скопировать ссылку
            </Button>
            {createdCheckoutUrl && (
              <a
                href={createdCheckoutUrl}
                target="_blank"
                rel="noopener noreferrer"
                className="flex-1 inline-flex items-center justify-center px-4 py-2 rounded-pill text-xs font-semibold border border-dp-line text-dp-blue hover:bg-dp-tint transition-colors"
              >
                Открыть Checkout
              </a>
            )}
          </div>
        </div>
      </Modal>

      <WebhookInspectorModal
        delivery={inspectDelivery}
        isOpen={!!inspectDelivery}
        onClose={() => setInspectDelivery(null)}
      />

      {/* Modal: Fiscal Receipt Preview */}
      <Modal isOpen={!!previewReceipt} onClose={() => setPreviewReceipt(null)} maxWidth="420px">
        {previewReceipt && (
          <Receipt
            orderNumber={previewReceipt.orderNumber}
            amount={previewReceipt.amount}
            fnNumber={previewReceipt.fnNumber}
            fdNumber={previewReceipt.fdNumber}
            fpdNumber={previewReceipt.fpdNumber}
            createdAt={previewReceipt.createdAt}
          />
        )}
      </Modal>

      <AuthModal
        isOpen={isAuthOpen}
        onClose={() => setIsAuthOpen(false)}
        onLogin={(email) => {
          apiClient.setToken('mock-bearer-token');
          setIsAuthenticated(true);
          setIsAuthOpen(false);
          showToast(`Добро пожаловать, ${email}!`);
        }}
      />

      {toastMessage && (
        <div className="fixed bottom-6 right-6 z-50 bg-dp-blue text-white px-4 py-2.5 rounded-pill shadow-popover text-xs font-semibold animate-fade-in">
          {toastMessage}
        </div>
      )}
    </div>
  );
}

export default function AdminPage() {
  return (
    <React.Suspense fallback={<div className="p-8 text-center text-dp-blue/60">Загрузка панели управления...</div>}>
      <AdminContent />
    </React.Suspense>
  );
}
