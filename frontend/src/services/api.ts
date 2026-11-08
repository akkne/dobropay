import { Shop } from '../types/Shop';
import { Payment } from '../types/Payment';
import { WebhookDelivery } from '../types/WebhookDelivery';
import { FiscalReceipt } from '../types/FiscalReceipt';
import { ApiKey } from '../types/ApiKey';
import { CheckoutSession } from '../types/CheckoutSession';
import { PaymentMethodType } from '../types/PaymentMethodType';
import { PaymentStatus } from '../types/PaymentStatus';
import { DomainConstants } from '../constants/domain';
import {
  initialMockShop,
  initialMockPayments,
  initialMockApiKeys,
  initialMockDeliveries,
  initialMockReceipts
} from './mockData';

export interface IApiClient {
  getShop(shopId: string): Promise<Shop>;
  getPayments(shopId: string): Promise<Payment[]>;
  capturePayment(shopId: string, paymentId: string, amount?: number): Promise<{ status: string; capturedAmount?: number }>;
  cancelPayment(shopId: string, paymentId: string, reason?: string): Promise<{ status: string }>;
  refundPayment(shopId: string, paymentId: string, amount: number, reason?: string): Promise<{ id: string; amount: number }>;
  getDeliveries(shopId: string): Promise<WebhookDelivery[]>;
  getReceipts(shopId: string): Promise<FiscalReceipt[]>;
  getApiKeys(shopId: string): Promise<ApiKey[]>;
  createApiKey(shopId: string, name: string, mode?: 'test' | 'live'): Promise<ApiKey>;
  revokeApiKey(shopId: string, keyId: string): Promise<void>;
  getWebhookConfig(shopId: string): Promise<{ url: string; signingSecret: string; events: string[] }>;
  updateWebhookConfig(shopId: string, config: { url: string; signingSecret: string; events: string[] }): Promise<void>;
  getCheckoutSession(token: string): Promise<CheckoutSession>;
  startCheckoutSession(token: string, method: PaymentMethodType, data?: Record<string, unknown>): Promise<{ status: PaymentStatus; confirmationUrl?: string }>;
  simulateCheckoutOutcome(token: string, outcome: PaymentStatus): Promise<{ status: PaymentStatus }>;
}

export class DobropayApiClient implements IApiClient {
  private baseUrl: string;
  private token: string | null = null;

  private shopState: Shop = { ...initialMockShop };
  private paymentsState: Payment[] = [...initialMockPayments];
  private apiKeysState: ApiKey[] = [...initialMockApiKeys];
  private deliveriesState: WebhookDelivery[] = [...initialMockDeliveries];
  private receiptsState: FiscalReceipt[] = [...initialMockReceipts];
  private webhookConfigState = {
    url: 'https://demo-merchant.local/api/dobropay-webhook',
    signingSecret: 'dp_sec_99a8b7c6d5e4f3a2b10987654321fedc',
    events: ['payment.succeeded', 'payment.waiting_for_capture', 'payment.canceled', 'refund.succeeded']
  };

  constructor() {
    this.baseUrl = typeof window !== 'undefined'
      ? localStorage.getItem(DomainConstants.StorageKeys.ApiBaseUrl) || DomainConstants.DefaultGatewayUrl
      : DomainConstants.DefaultGatewayUrl;

    this.token = typeof window !== 'undefined'
      ? localStorage.getItem(DomainConstants.StorageKeys.AuthToken)
      : null;
  }

  public setToken(token: string | null) {
    this.token = token;
    if (typeof window !== 'undefined') {
      if (token) {
        localStorage.setItem(DomainConstants.StorageKeys.AuthToken, token);
      } else {
        localStorage.removeItem(DomainConstants.StorageKeys.AuthToken);
      }
    }
  }

  public getToken(): string | null {
    return this.token;
  }

  private getAuthHeaders(includeContentType = true): Record<string, string> {
    const headers: Record<string, string> = {
      'Accept': 'application/json'
    };
    if (includeContentType) {
      headers['Content-Type'] = 'application/json';
    }
    if (this.token) {
      headers['Authorization'] = `Bearer ${this.token}`;
    }
    return headers;
  }

  public async getShop(shopId: string): Promise<Shop> {
    try {
      const response = await fetch(`${this.baseUrl}/v1/developer/shops`, {
        headers: this.getAuthHeaders(false)
      });
      if (response.ok) {
        const shops = await response.json();
        if (Array.isArray(shops) && shops.length > 0) {
          const s = shops.find((x: { id: string }) => x.id === shopId) || shops[0];
          this.shopState = {
            id: s.id,
            name: s.name || this.shopState.name,
            inn: s.inn || this.shopState.inn,
            status: s.status || this.shopState.status,
            createdAt: s.createdAt || this.shopState.createdAt
          };
          return this.shopState;
        }
      }
    } catch {
      // Standalone mode fallback
    }
    return this.shopState;
  }

  public async getPayments(shopId: string): Promise<Payment[]> {
    try {
      const response = await fetch(`${this.baseUrl}/v1/admin/shops/${shopId}/payments`, {
        headers: this.getAuthHeaders(false)
      });
      if (response.ok) {
        const rawPayments = await response.json();
        if (Array.isArray(rawPayments)) {
          const mapped: Payment[] = rawPayments.map((p: Record<string, unknown>) => ({
            id: String(p.id ?? ''),
            shopId: String(p.shopId ?? shopId),
            orderId: String(p.externalOrderId ?? p.id ?? ''),
            externalOrderId: String(p.externalOrderId ?? p.id ?? ''),
            amount: Number(p.amount ?? 0),
            currency: String(p.currency ?? 'RUB'),
            status: (p.status as PaymentStatus) ?? 'pending',
            paymentMethodType: ((p.paymentMethodType as string) === 'sbp' ? 'sbp' : 'bank_card') as PaymentMethodType,
            capture: Boolean(p.capture ?? true),
            checkoutUrl: `/checkout/${p.id}`,
            capturedAmount: p.capturedAmount ? Number(p.capturedAmount) : undefined,
            refundedAmount: Number(p.refundedAmount ?? 0),
            cancellationReason: p.cancellationReason ? String(p.cancellationReason) : undefined,
            createdAt: p.createdAt ? String(p.createdAt) : new Date().toISOString(),
            updatedAt: p.updatedAt ? String(p.updatedAt) : new Date().toISOString(),
            refunds: []
          }));
          this.paymentsState = mapped;
          return mapped;
        }
      }
    } catch {
      // Standalone mode fallback
    }
    return [...this.paymentsState];
  }

  public async capturePayment(shopId: string, paymentId: string, amount?: number): Promise<{ status: string; capturedAmount?: number }> {
    try {
      const response = await fetch(`${this.baseUrl}/v1/admin/shops/${shopId}/payments/${paymentId}/capture`, {
        method: 'POST',
        headers: this.getAuthHeaders(true),
        body: JSON.stringify({ amount })
      });
      if (response.ok) {
        const data = await response.json();
        return { status: data.status || 'succeeded', capturedAmount: data.capturedAmount ?? amount };
      }
    } catch {
      // Standalone fallback
    }

    const payment = this.paymentsState.find(p => p.id === paymentId);
    if (payment) {
      payment.status = 'succeeded';
      payment.capturedAmount = amount || payment.amount;
      payment.updatedAt = new Date().toISOString();
    }
    return { status: 'succeeded', capturedAmount: amount || (payment ? payment.amount : 0) };
  }

  public async cancelPayment(shopId: string, paymentId: string, reason = 'merchant_canceled'): Promise<{ status: string }> {
    try {
      const response = await fetch(`${this.baseUrl}/v1/admin/shops/${shopId}/payments/${paymentId}/cancel`, {
        method: 'POST',
        headers: this.getAuthHeaders(true),
        body: JSON.stringify({ reason })
      });
      if (response.ok) {
        const data = await response.json();
        return { status: data.status || 'canceled' };
      }
    } catch {
      // Standalone fallback
    }

    const payment = this.paymentsState.find(p => p.id === paymentId);
    if (payment) {
      payment.status = 'canceled';
      payment.cancellationReason = reason;
      payment.updatedAt = new Date().toISOString();
    }
    return { status: 'canceled' };
  }

  public async refundPayment(shopId: string, paymentId: string, amount: number, reason = ''): Promise<{ id: string; amount: number }> {
    try {
      const response = await fetch(`${this.baseUrl}/v1/admin/shops/${shopId}/payments/${paymentId}/refunds`, {
        method: 'POST',
        headers: this.getAuthHeaders(true),
        body: JSON.stringify({ amount, reason })
      });
      if (response.ok) {
        const data = await response.json();
        return { id: data.id || `ref-${Date.now()}`, amount };
      }
    } catch {
      // Standalone fallback
    }

    const payment = this.paymentsState.find(p => p.id === paymentId);
    const refundId = `ref-${Date.now()}`;
    if (payment) {
      payment.refundedAmount = (payment.refundedAmount || 0) + amount;
      payment.refunds.unshift({
        id: refundId,
        amount,
        reason,
        status: 'succeeded',
        createdAt: new Date().toISOString()
      });
    }
    return { id: refundId, amount };
  }

  public async getDeliveries(shopId: string): Promise<WebhookDelivery[]> {
    try {
      const response = await fetch(`${this.baseUrl}/v1/admin/shops/${shopId}/deliveries`, {
        headers: this.getAuthHeaders(false)
      });
      if (response.ok) {
        const list = await response.json();
        if (Array.isArray(list)) {
          return list.map((d: Record<string, unknown>, idx: number) => ({
            id: String(d.id ?? d.event_id ?? `del-${idx}`),
            eventType: String(d.eventType ?? d.event_type ?? 'payment.succeeded'),
            paymentId: String(d.paymentId ?? d.payment_id ?? `pay-${idx}`),
            url: String(d.url ?? 'https://demo-merchant.local/api/dobropay-webhook'),
            status: ((d.state ?? d.status) === 'delivered' ? 'delivered' : (d.state ?? d.status) === 'failed' ? 'failed' : 'retrying') as 'delivered' | 'retrying' | 'failed',
            statusCode: Number(d.last_result ?? d.statusCode ?? 200),
            attempts: Number(d.attempts ?? 1),
            createdAt: String(d.createdAt ?? new Date().toISOString()),
            payload: JSON.stringify(d, null, 2),
            signature: 'dp_sig_sha256_mock'
          }));
        }
      }
    } catch {
      // Standalone fallback
    }
    return [...this.deliveriesState];
  }

  public async getReceipts(shopId: string): Promise<FiscalReceipt[]> {
    try {
      const response = await fetch(`${this.baseUrl}/v1/admin/shops/${shopId}/receipts`, {
        headers: this.getAuthHeaders(false)
      });
      if (response.ok) {
        const list = await response.json();
        if (Array.isArray(list)) {
          return list.map((r: Record<string, unknown>, idx: number) => ({
            id: String(r.id ?? `rec-${idx}`),
            paymentId: String(r.paymentId ?? r.payment_id ?? `pay-${idx}`),
            orderNumber: String(r.orderNumber ?? r.orderId ?? r.externalOrderId ?? `ord-${idx}`),
            amount: Number(r.amount ?? 0),
            fiscalStatus: ((r.fiscalStatus ?? r.status) === 'pending' ? 'pending' : 'succeeded') as 'succeeded' | 'pending',
            fnNumber: String(r.fnNumber ?? '9999078900012345'),
            fdNumber: String(r.fdNumber ?? '10482'),
            fpdNumber: String(r.fpdNumber ?? '3849102847'),
            customerEmail: String(r.customerEmail ?? r.buyerEmail ?? 'customer@example.com'),
            emailStatus: ((r.emailStatus) === 'sending' ? 'sending' : 'sent') as 'sent' | 'sending',
            createdAt: String(r.createdAt ?? new Date().toISOString()),
            items: Array.isArray(r.items)
              ? (r.items as Array<Record<string, unknown>>).map((item) => ({
                  name: String(item.name ?? 'Товар'),
                  price: Number(item.price ?? 0),
                  quantity: Number(item.quantity ?? 1),
                  vat: String(item.vat ?? 'vat20')
                }))
              : [
                  {
                    name: `Заказ #${String(r.orderNumber ?? r.orderId ?? r.externalOrderId ?? idx)}`,
                    price: Number(r.amount ?? 0),
                    quantity: 1,
                    vat: 'vat20'
                  }
                ]
          }));
        }
      }
    } catch {
      // Standalone fallback
    }
    return [...this.receiptsState];
  }

  public async getApiKeys(shopId: string): Promise<ApiKey[]> {
    try {
      const response = await fetch(`${this.baseUrl}/v1/developer/shops/${shopId}/keys`, {
        headers: this.getAuthHeaders(false)
      });
      if (response.ok) {
        const list = await response.json();
        if (Array.isArray(list)) {
          return list.map((k: Record<string, unknown>, idx: number) => ({
            id: String(k.id ?? `key-${idx}`),
            name: String(k.name ?? 'API Key'),
            prefix: String(k.prefix ?? k.key_prefix ?? 'dp_live_...'),
            apiKey: String(k.apiKey ?? k.key_mask ?? 'dp_live_••••••••'),
            createdAt: String(k.createdAt ?? k.created_at ?? new Date().toISOString()),
            status: 'active' as const
          }));
        }
      }
    } catch {
      // Standalone fallback
    }
    return [...this.apiKeysState];
  }

  public async createApiKey(shopId: string, name: string, mode: 'test' | 'live' = 'test'): Promise<ApiKey> {
    try {
      const response = await fetch(`${this.baseUrl}/v1/developer/shops/${shopId}/keys`, {
        method: 'POST',
        headers: this.getAuthHeaders(true),
        body: JSON.stringify({ name, mode })
      });
      if (response.ok) {
        const k = await response.json();
        const created: ApiKey = {
          id: String(k.id ?? `key-${Date.now()}`),
          name: String(k.name ?? name),
          prefix: String(k.key_prefix ?? k.prefix ?? 'dp_...'),
          apiKey: String(k.api_key ?? k.apiKey ?? `dp_${mode}_${Date.now()}`),
          createdAt: String(k.created_at ?? k.createdAt ?? new Date().toISOString()),
          status: 'active'
        };
        this.apiKeysState.unshift(created);
        return created;
      }
    } catch {
      // Standalone fallback
    }

    const randomSuffix = Array.from({ length: 32 }, () => Math.floor(Math.random() * 16).toString(16)).join('');
    const fullKey = `dp_${mode}_${randomSuffix}`;
    const newKey: ApiKey = {
      id: `key-${Date.now()}`,
      name,
      prefix: `dp_${mode}_${randomSuffix.substring(0, 6)}...`,
      apiKey: fullKey,
      createdAt: new Date().toISOString(),
      status: 'active'
    };
    this.apiKeysState.unshift(newKey);
    return newKey;
  }

  public async revokeApiKey(shopId: string, keyId: string): Promise<void> {
    try {
      await fetch(`${this.baseUrl}/v1/developer/shops/${shopId}/keys/${keyId}`, {
        method: 'DELETE',
        headers: this.getAuthHeaders(false)
      });
    } catch {
      // Standalone fallback
    }
    const idx = this.apiKeysState.findIndex(k => k.id === keyId);
    if (idx !== -1) {
      this.apiKeysState.splice(idx, 1);
    }
  }

  public async getWebhookConfig(shopId: string): Promise<{ url: string; signingSecret: string; events: string[] }> {
    return { ...this.webhookConfigState };
  }

  public async updateWebhookConfig(shopId: string, config: { url: string; signingSecret: string; events: string[] }): Promise<void> {
    this.webhookConfigState = { ...config };
  }

  public async getCheckoutSession(token: string): Promise<CheckoutSession> {
    try {
      const response = await fetch(`${this.baseUrl}/checkout/${token}/session`, {
        method: 'GET',
        headers: { 'Accept': 'application/json' }
      });
      if (response.ok) {
        const data = await response.json();
        return data as CheckoutSession;
      }
    } catch {
      // Standalone fallback
    }

    try {
      const fallbackResponse = await fetch(`${this.baseUrl}/checkout/${token}`, {
        method: 'GET',
        headers: { 'Accept': 'application/json' }
      });
      if (fallbackResponse.ok) {
        const data = await fallbackResponse.json();
        return data as CheckoutSession;
      }
    } catch {
      // Standalone fallback
    }

    const matchingPayment = this.paymentsState.find(
      p => p.id === token || p.checkoutUrl?.includes(token) || token.startsWith('demo-checkout-token')
    );

    if (!matchingPayment) {
      throw new Error(`Сессия оплаты "${token}" не найдена или срок действия истек.`);
    }

    return {
      token,
      paymentId: matchingPayment.id,
      shopId: matchingPayment.shopId || this.shopState.id,
      shopName: this.shopState.name,
      orderId: matchingPayment.orderId || matchingPayment.externalOrderId,
      amount: matchingPayment.amount,
      currency: matchingPayment.currency,
      status: matchingPayment.status,
      paymentMethodType: matchingPayment.paymentMethodType,
      capture: matchingPayment.capture ?? true,
      expiresAt: new Date(Date.now() + 30 * 60 * 1000).toISOString(),
      createdAt: matchingPayment.createdAt,
      description: matchingPayment.description
    };
  }

  public async startCheckoutSession(
    token: string,
    method: PaymentMethodType,
    data?: Record<string, unknown>
  ): Promise<{ status: PaymentStatus; confirmationUrl?: string }> {
    try {
      const response = await fetch(`${this.baseUrl}/checkout/${token}/start`, {
        method: 'POST',
        headers: {
          'Accept': 'application/json',
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({ method, ...data })
      });
      if (response.ok) {
        const res = await response.json();
        return {
          status: res.status ?? 'pending',
          confirmationUrl: res.confirmationUrl || `/checkout/${token}/simulator?method=${method}`
        };
      }
    } catch {
      // Standalone fallback
    }

    const matchingPayment = this.paymentsState.find(
      p => p.id === token || p.checkoutUrl?.includes(token) || token.startsWith('demo-checkout-token')
    );
    if (matchingPayment) {
      matchingPayment.paymentMethodType = method;
      matchingPayment.updatedAt = new Date().toISOString();
      return {
        status: matchingPayment.status,
        confirmationUrl: `/checkout/${token}/simulator?method=${method}`
      };
    }

    return {
      status: 'pending',
      confirmationUrl: `/checkout/${token}/simulator?method=${method}`
    };
  }

  public async simulateCheckoutOutcome(token: string, outcome: PaymentStatus): Promise<{ status: PaymentStatus }> {
    try {
      const response = await fetch(`${this.baseUrl}/checkout/${token}/simulate/${outcome}`, {
        method: 'POST',
        headers: {
          'Accept': 'application/json'
        }
      });
      if (response.ok) {
        const data = await response.json();
        return { status: data.status || outcome };
      }
    } catch {
      // Fall through to update local state
    }

    const matchingPayment = this.paymentsState.find(
      p => p.id === token || p.checkoutUrl?.includes(token) || token.startsWith('demo-checkout-token')
    );
    if (matchingPayment) {
      matchingPayment.status = outcome;
      matchingPayment.updatedAt = new Date().toISOString();
    }
    return { status: outcome };
  }

  public addPayment(newPayment: Payment) {
    this.paymentsState.unshift(newPayment);
  }
}

export const apiClient = new DobropayApiClient();
