'use client';

import React, { useState, useEffect } from 'react';
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

  const [webhookConfig, setWebhookConfig] = useState({
    url: 'https://demo-merchant.local/api/dobropay-webhook',
    signingSecret: 'dp_sec_99a8b7c6d5e4f3a2b10987654321fedc',
    events: ['payment.succeeded', 'payment.waiting_for_capture', 'payment.canceled', 'refund.succeeded']
  });

  const showToast = (msg: string) => {
    setToastMessage(msg);
    setTimeout(() => setToastMessage(null), 3000);
  };

  const loadData = async () => {
    const s = await apiClient.getShop('default');
    setShop(s);
    const p = await apiClient.getPayments(s.id);
    setPayments(p);
    const d = await apiClient.getDeliveries(s.id);
    setDeliveries(d);
    const r = await apiClient.getReceipts(s.id);
    setReceipts(r);
    const k = await apiClient.getApiKeys(s.id);
    setApiKeys(k);
    const wc = await apiClient.getWebhookConfig(s.id);
    setWebhookConfig(wc);
    setIsAuthenticated(!!apiClient.getToken());
  };

  useEffect(() => {
    loadData();
  }, []);

  useEffect(() => {
    if (queryTab) {
      setActiveTab(queryTab);
    }
  }, [queryTab]);

  // Handlers
  const handleCaptureConfirm = async (kopecks: number) => {
    if (!selectedPayment) return;
    await apiClient.capturePayment('shop', selectedPayment.id, kopecks);
    setIsCaptureOpen(false);
    setIsDrawerOpen(false);
    await loadData();
    showToast('Списание успешно подтверждено!');
  };

  const handleRefundConfirm = async (kopecks: number, reason: string) => {
    if (!selectedPayment) return;
    await apiClient.refundPayment('shop', selectedPayment.id, kopecks, reason);
    setIsRefundOpen(false);
    setIsDrawerOpen(false);
    await loadData();
    showToast('Возврат успешно проведён!');
  };

  const handleCancelPayment = async (p: Payment) => {
    if (confirm('Вы уверены, что хотите отменить холд и разблокировать средства клиенту?')) {
      await apiClient.cancelPayment('shop', p.id);
      setIsDrawerOpen(false);
      await loadData();
      showToast('Холд успешно отменён');
    }
  };

  const handleCreatePaymentSubmit = (data: {
    amountKopecks: number;
    orderId: string;
    method: any;
    capture: boolean;
  }) => {
    const newPayment: Payment = {
      id: `pay-${Date.now()}`,
      externalOrderId: data.orderId,
      amount: data.amountKopecks,
      currency: 'RUB',
      status: 'pending',
      paymentMethodType: data.method,
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
      capturedAmount: 0,
      refundedAmount: 0,
      refunds: []
    };
    apiClient.addPayment(newPayment);
    setIsCreatePaymentOpen(false);
    loadData();

    const checkoutUrl = `/checkout/${newPayment.id}`;
    setCreatedCheckoutUrl(checkoutUrl);
  };

  return (
    <div className="admin-layout">
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

      <main className="admin-container">
        {/* VIEW 1: PAYMENTS */}
        {activeTab === 'payments' && (
          <div style={{ display: 'flex', flexDirection: 'column', gap: '24px' }}>
            <KpiSummaryGrid payments={payments} />

            <PaymentsTable
              payments={payments}
              onOpenCreate={() => setIsCreatePaymentOpen(true)}
              onOpenDetails={(p) => {
                setSelectedPayment(p);
                setIsDrawerOpen(true);
              }}
              onOpenCapture={(p) => {
                setSelectedPayment(p);
                setIsCaptureOpen(true);
              }}
              onOpenRefund={(p) => {
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
          <div style={{ display: 'flex', flexDirection: 'column', gap: '24px' }}>
            <ApiKeysSection
              apiKeys={apiKeys}
              onCreateKey={async (name) => {
                const k = await apiClient.createApiKey('shop', name);
                await loadData();
                return k;
              }}
              onRevokeKey={async (id) => {
                await apiClient.revokeApiKey(id);
                await loadData();
                showToast('API-ключ отозван');
              }}
            />

            <WebhooksSection
              deliveries={deliveries}
              initialUrl={webhookConfig.url}
              initialSecret={webhookConfig.signingSecret}
              initialEvents={webhookConfig.events}
              onSaveConfig={async (url, signingSecret, events) => {
                await apiClient.updateWebhookConfig('shop', { url, signingSecret, events });
                showToast('Настройки Webhook сохранены!');
              }}
              onInspectDelivery={(d) => setInspectDelivery(d)}
              onRetryDelivery={() => {
                showToast('Повторная отправка Webhook запущена...');
                setTimeout(() => showToast('Webhook успешно доставлен (HTTP 200)'), 1200);
              }}
            />
          </div>
        )}

        {/* VIEW 4: ONBOARDING */}
        {activeTab === 'onboarding' && (
          <OnboardingWizard
            onSuccess={() => showToast('Анкета магазина принята на проверку!')}
            onGoToPayments={() => setActiveTab('payments')}
          />
        )}
      </main>

      {/* MODALS & DRAWERS */}
      {isDrawerOpen && (
        <PaymentDrawer
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
      )}

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
        <div style={{ textAlign: 'center' }}>
          <div style={{ fontSize: '40px', marginBottom: '10px' }}>🎉</div>
          <h3 style={{ fontSize: '20px', fontWeight: 700, color: 'var(--blue)', marginBottom: '6px' }}>
            Платёжная ссылка готова!
          </h3>
          <p style={{ fontSize: '14px', color: 'rgba(9, 49, 141, 0.7)', marginBottom: '18px' }}>
            Отправьте эту ссылку клиенту или перейдите для проверки формы оплаты
          </p>

          <div className="form-group">
            <input
              type="text"
              className="form-input mono"
              readOnly
              value={createdCheckoutUrl ? `${typeof window !== 'undefined' ? window.location.origin : ''}${createdCheckoutUrl}` : ''}
            />
          </div>

          <div style={{ display: 'flex', gap: '10px', marginTop: '14px' }}>
            <Button
              variant="primary"
              style={{ flex: 1 }}
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
              <a href={createdCheckoutUrl} target="_blank" className="button outline" style={{ flex: 1 }}>
                Открыть Hosted Checkout
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
      <Modal isOpen={!!previewReceipt} onClose={() => setPreviewReceipt(null)} maxWidth="440px">
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
        <div className="toast-container">
          <div className="toast info">{toastMessage}</div>
        </div>
      )}
    </div>
  );
}

export default function AdminPage() {
  return (
    <React.Suspense fallback={<div className="p-8 text-center text-slate-500">Загрузка панели управления...</div>}>
      <AdminContent />
    </React.Suspense>
  );
}
