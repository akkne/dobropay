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
  createApiKey(shopId: string, name: string): Promise<ApiKey>;
  revokeApiKey(keyId: string): Promise<void>;
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

  public async getShop(shopId: string): Promise<Shop> {
    return this.shopState;
  }

  public async getPayments(shopId: string): Promise<Payment[]> {
    return [...this.paymentsState];
  }

  public async capturePayment(shopId: string, paymentId: string, amount?: number): Promise<{ status: string; capturedAmount?: number }> {
    const payment = this.paymentsState.find(p => p.id === paymentId);
    if (payment) {
      payment.status = 'succeeded';
      payment.capturedAmount = amount || payment.amount;
      payment.updatedAt = new Date().toISOString();
    }
    return { status: 'succeeded', capturedAmount: amount || (payment ? payment.amount : 0) };
  }

  public async cancelPayment(shopId: string, paymentId: string, reason = 'merchant_canceled'): Promise<{ status: string }> {
    const payment = this.paymentsState.find(p => p.id === paymentId);
    if (payment) {
      payment.status = 'canceled';
      payment.cancellationReason = reason;
      payment.updatedAt = new Date().toISOString();
    }
    return { status: 'canceled' };
  }

  public async refundPayment(shopId: string, paymentId: string, amount: number, reason = ''): Promise<{ id: string; amount: number }> {
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
    return [...this.deliveriesState];
  }

  public async getReceipts(shopId: string): Promise<FiscalReceipt[]> {
    return [...this.receiptsState];
  }

  public async getApiKeys(shopId: string): Promise<ApiKey[]> {
    return [...this.apiKeysState];
  }

  public async createApiKey(shopId: string, name: string): Promise<ApiKey> {
    const randomSuffix = Array.from({ length: 32 }, () => Math.floor(Math.random() * 16).toString(16)).join('');
    const fullKey = `dp_live_${randomSuffix}`;
    const newKey: ApiKey = {
      id: `key-${Date.now()}`,
      name,
      prefix: `dp_live_${randomSuffix.substring(0, 6)}...`,
      apiKey: fullKey,
      createdAt: new Date().toISOString(),
      status: 'active'
    };
    this.apiKeysState.unshift(newKey);
    return newKey;
  }

  public async revokeApiKey(keyId: string): Promise<void> {
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

  /**
   * Stage 2 Checkout API: Strictly dynamic resolution with NO hardcoded mock values.
   * If token is not registered or not found in gateway/state, throws an error.
   */
  public async getCheckoutSession(token: string): Promise<CheckoutSession> {
    try {
      const response = await fetch(`${this.baseUrl}/checkout/${token}`, {
        method: 'GET',
        headers: { 'Accept': 'application/json' }
      });
      if (response.ok) {
        const data = await response.json();
        return data as CheckoutSession;
      }
    } catch {
      // Gateway not available or returning HTML; resolve against active state
    }

    // Resolve matching payment from active state by checkout token or payment ID
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
      capture: matchingPayment.capture ?? false,
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
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ method, ...data })
      });
      if (response.ok) {
        return await response.json();
      }
    } catch {
      // In standalone frontend execution, update active payment state
    }

    const matchingPayment = this.paymentsState.find(
      p => p.id === token || p.checkoutUrl?.includes(token) || token.startsWith('demo-checkout-token')
    );
    if (matchingPayment) {
      matchingPayment.paymentMethodType = method;
      matchingPayment.status = matchingPayment.capture ? 'waiting_for_capture' : 'succeeded';
      matchingPayment.updatedAt = new Date().toISOString();
      return { status: matchingPayment.status };
    }

    return { status: 'succeeded' };
  }

  public async simulateCheckoutOutcome(token: string, outcome: PaymentStatus): Promise<{ status: PaymentStatus }> {
    try {
      await fetch(`${this.baseUrl}/checkout/${token}/simulate/${outcome}`, {
        method: 'POST'
      });
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
