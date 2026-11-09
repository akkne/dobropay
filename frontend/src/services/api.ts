import { Shop } from '../types/Shop';
import { Payment } from '../types/Payment';
import { WebhookDelivery } from '../types/WebhookDelivery';
import { FiscalReceipt } from '../types/FiscalReceipt';
import { ApiKey } from '../types/ApiKey';
import { CheckoutSession } from '../types/CheckoutSession';
import { PaymentMethodType } from '../types/PaymentMethodType';
import { PaymentStatus } from '../types/PaymentStatus';
import { DomainConstants } from '../constants/domain';

export class DobropayApiClient {
  private token: string | null = null;

  public constructor() {
    if (typeof window !== 'undefined') {
      this.token = sessionStorage.getItem(DomainConstants.StorageKeys.AuthToken);
    }
  }

  public setToken(token: string | null): void {
    this.token = token;
    if (typeof window !== 'undefined') {
      if (token) sessionStorage.setItem(DomainConstants.StorageKeys.AuthToken, token);
      else sessionStorage.removeItem(DomainConstants.StorageKeys.AuthToken);
    }
  }

  public getToken(): string | null {
    return this.token;
  }

  private async request<T>(path: string, init: RequestInit = {}): Promise<T> {
    const headers = new Headers(init.headers);
    headers.set('Accept', 'application/json');
    if (init.body) headers.set('Content-Type', 'application/json');
    if (this.token) headers.set('Authorization', `Bearer ${this.token}`);
    const response = await fetch(path, { ...init, headers, cache: 'no-store' });
    if (!response.ok) {
      throw new Error(`Запрос не выполнен (HTTP ${response.status})`);
    }
    const body = await response.text();
    return body ? JSON.parse(body) as T : undefined as T;
  }

  private async mutate<T>(path: string, operation: string, body: unknown): Promise<T> {
    const storageKey = `dp_idempotency_${operation}`;
    const idempotencyKey = sessionStorage.getItem(storageKey) || crypto.randomUUID();
    sessionStorage.setItem(storageKey, idempotencyKey);
    const result = await this.request<T>(path, {
      method: 'POST',
      headers: { 'Idempotency-Key': idempotencyKey },
      body: JSON.stringify(body)
    });
    sessionStorage.removeItem(storageKey);
    return result;
  }

  public async login(email: string, password: string): Promise<void> {
    const result = await this.request<{ access_token: string }>('/v1/auth/login', {
      method: 'POST',
      body: JSON.stringify({ email, password })
    });
    this.setToken(result.access_token);
  }

  public async register(email: string, password: string): Promise<void> {
    const result = await this.request<{ access_token: string }>('/v1/auth/register', {
      method: 'POST',
      body: JSON.stringify({ email, password })
    });
    this.setToken(result.access_token);
  }

  public async logout(): Promise<void> {
    try {
      await this.request('/v1/auth/logout', { method: 'POST' });
    } finally {
      this.setToken(null);
    }
  }

  public async getShops(): Promise<Shop[]> {
    const shops = await this.request<Array<{ id: string; name: string; verification_status: string }>>('/v1/developer/shops');
    return shops.map(shop => ({ id: shop.id, name: shop.name, status: shop.verification_status }));
  }

  public async createShop(name: string): Promise<Shop> {
    const created = await this.request<{ id: string; name: string }>('/v1/developer/shops', {
      method: 'POST', body: JSON.stringify({ name })
    });
    return { id: created.id, name: created.name, status: 'under_review' };
  }

  public async getPayments(shopId: string): Promise<Payment[]> {
    const rows = await this.request<Array<Record<string, any>>>(`/v1/admin/shops/${shopId}/payments`);
    return rows.map(p => ({
      id: p.id,
      shopId,
      orderId: p.externalOrderId ?? p.id,
      externalOrderId: p.externalOrderId ?? p.id,
      amount: p.amount,
      currency: p.currency,
      status: p.status as PaymentStatus,
      paymentMethodType: p.paymentMethodType as PaymentMethodType,
      capture: p.capture,
      capturedAmount: p.capturedAmount ?? undefined,
      refundedAmount: p.refundedAmount,
      cancellationReason: p.cancellationReason ?? undefined,
      createdAt: p.createdAt,
      updatedAt: p.updatedAt,
      refunds: []
    }));
  }

  public capturePayment(shopId: string, paymentId: string, amount?: number): Promise<unknown> {
    return this.mutate(`/v1/admin/shops/${shopId}/payments/${paymentId}/capture`,
      `capture_${paymentId}_${amount ?? 'full'}`, { amount });
  }

  public cancelPayment(shopId: string, paymentId: string, reason?: string): Promise<unknown> {
    return this.mutate(`/v1/admin/shops/${shopId}/payments/${paymentId}/cancel`,
      `cancel_${paymentId}_${reason ?? ''}`, { reason });
  }

  public refundPayment(shopId: string, paymentId: string, amount: number, reason?: string): Promise<unknown> {
    return this.mutate(`/v1/admin/shops/${shopId}/payments/${paymentId}/refunds`,
      `refund_${paymentId}_${amount}_${reason ?? ''}`, { amount, reason });
  }

  public async getDeliveries(shopId: string): Promise<WebhookDelivery[]> {
    const rows = await this.request<Array<{ event_id: string; state: string; attempts: number; last_result: string | null }>>(`/v1/admin/shops/${shopId}/deliveries`);
    return rows.map(row => ({
      id: row.event_id,
      state: row.state,
      attempts: row.attempts,
      lastResult: row.last_result
    }));
  }

  public async getReceipts(shopId: string): Promise<FiscalReceipt[]> {
    const rows = await this.request<Array<Record<string, any>>>(`/v1/admin/shops/${shopId}/receipts`);
    return rows.map(r => ({
      id: r.id, paymentId: r.paymentId, orderNumber: r.orderNumber,
      amount: r.amount, fiscalStatus: r.fiscalStatus, fnNumber: r.fnNumber ?? '',
      fdNumber: r.fdNumber == null ? '' : String(r.fdNumber), fpdNumber: r.fpdNumber ?? '',
      customerEmail: r.customerEmail ?? '',
      createdAt: r.createdAt,
      items: (r.items ?? []).map((item: Record<string, any>) => ({
        name: item.description ?? item.name, price: item.amount ?? item.price,
        quantity: item.quantity, vat: item.vatCode ?? item.vat
      }))
    }));
  }

  public async getApiKeys(shopId: string): Promise<ApiKey[]> {
    const rows = await this.request<Array<{ id: string; name: string; key_prefix: string; key_mask: string; created_at: string }>>(`/v1/developer/shops/${shopId}/keys`);
    return rows.map(k => ({ id: k.id, name: k.name, prefix: k.key_mask || k.key_prefix, createdAt: k.created_at, status: 'active' }));
  }

  public async createApiKey(shopId: string, name: string): Promise<ApiKey> {
    const k = await this.request<{ id: string; name: string; api_key: string; key_prefix: string; created_at: string }>(`/v1/developer/shops/${shopId}/keys`, {
      method: 'POST', body: JSON.stringify({ name, mode: 'live' })
    });
    return { id: k.id, name: k.name, prefix: k.key_prefix, apiKey: k.api_key, createdAt: k.created_at, status: 'active' };
  }

  public revokeApiKey(shopId: string, keyId: string): Promise<void> {
    return this.request(`/v1/developer/shops/${shopId}/keys/${keyId}`, { method: 'DELETE' });
  }

  public async getCheckoutSession(token: string): Promise<CheckoutSession> {
    const session = await this.request<CheckoutSession>(`/checkout/${encodeURIComponent(token)}/session`);
    return { ...session, amount: Math.round(session.amount * 100) };
  }

}

export const apiClient = new DobropayApiClient();
