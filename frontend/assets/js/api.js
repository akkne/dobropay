/**
 * Dobropay API Client
 * Supports both Live API (via Gateway) and Standalone Mock Mode for development & preview.
 */

const STORAGE_KEYS = {
  TOKEN: 'dp_auth_token',
  ACCOUNT_ID: 'dp_account_id',
  CURRENT_SHOP: 'dp_current_shop_id',
  API_BASE: 'dp_api_base_url',
  USE_MOCKS: 'dp_use_mocks'
};

const DEFAULT_BASE_URL = 'https://dobroplay.online';

// Mock initial database
const MOCK_DB = {
  shop: {
    id: '11111111-1111-4111-8111-111111111111',
    name: 'Магазин мерча Dobropay',
    status: 'active',
    inn: '7707083893',
    ogrn: '1027700132195',
    legalName: 'ООО «Добропей Диджитал»',
    checkingAccount: '40702810938000012345',
    bik: '044525225'
  },
  payments: [
    {
      id: 'd9b3a5b0-8fc2-43bb-a5a1-778899aabbcc',
      externalOrderId: 'order-1042',
      amount: 129900,
      currency: 'RUB',
      status: 'succeeded',
      paymentMethodType: 'sbp',
      createdAt: new Date(Date.now() - 1000 * 60 * 18).toISOString(),
      updatedAt: new Date(Date.now() - 1000 * 60 * 16).toISOString(),
      capturedAmount: 129900,
      refundedAmount: 0,
      refunds: []
    },
    {
      id: 'e1c4a5b0-8fc2-43bb-b5a1-998877aabbdd',
      externalOrderId: 'order-1043',
      amount: 450000,
      currency: 'RUB',
      status: 'waiting_for_capture',
      paymentMethodType: 'bank_card',
      createdAt: new Date(Date.now() - 1000 * 60 * 35).toISOString(),
      updatedAt: new Date(Date.now() - 1000 * 60 * 34).toISOString(),
      capturedAmount: 0,
      refundedAmount: 0,
      cardLast4: '4242',
      cardBrand: 'mir',
      refunds: []
    },
    {
      id: 'f2d5a5b0-8fc2-43bb-c5a1-112233aabbcc',
      externalOrderId: 'order-1044',
      amount: 89000,
      currency: 'RUB',
      status: 'pending',
      paymentMethodType: 'crypto',
      cryptoNetwork: 'USDT_TRC20',
      cryptoAmount: 9.14,
      depositAddress: 'TXjS51QoP8iBhykLw9mYfR79r42XN8yFkM',
      confirmations: 1,
      requiredConfirmations: 3,
      createdAt: new Date(Date.now() - 1000 * 60 * 5).toISOString(),
      updatedAt: new Date(Date.now() - 1000 * 60 * 4).toISOString(),
      capturedAmount: 0,
      refundedAmount: 0,
      refunds: []
    },
    {
      id: 'a3e6a5b0-8fc2-43bb-d5a1-445566aabbcc',
      externalOrderId: 'order-1040',
      amount: 320000,
      currency: 'RUB',
      status: 'succeeded',
      paymentMethodType: 'bank_card',
      createdAt: new Date(Date.now() - 1000 * 60 * 180).toISOString(),
      updatedAt: new Date(Date.now() - 1000 * 60 * 179).toISOString(),
      capturedAmount: 320000,
      refundedAmount: 100000,
      cardLast4: '5555',
      cardBrand: 'mastercard',
      refunds: [
        {
          id: 'ref-001',
          amount: 100000,
          status: 'succeeded',
          reason: 'Частичный возврат (клиент вернул 1 позицию)',
          createdAt: new Date(Date.now() - 1000 * 60 * 60).toISOString()
        }
      ]
    },
    {
      id: 'b4f7a5b0-8fc2-43bb-e5a1-778899aabbcc',
      externalOrderId: 'order-1039',
      amount: 150000,
      currency: 'RUB',
      status: 'canceled',
      cancellationReason: 'insufficient_funds',
      paymentMethodType: 'bank_card',
      createdAt: new Date(Date.now() - 1000 * 60 * 300).toISOString(),
      updatedAt: new Date(Date.now() - 1000 * 60 * 299).toISOString(),
      refunds: []
    }
  ],
  apiKeys: [
    {
      id: 'k1-uuid',
      name: 'Основной рабочий сервер',
      prefix: 'dp_live_8f3a...',
      createdAt: new Date(Date.now() - 1000 * 60 * 60 * 24 * 14).toISOString(),
      status: 'active'
    },
    {
      id: 'k2-uuid',
      name: 'Основной ключ API',
      prefix: 'dp_live_shop_a_...',
      createdAt: new Date(Date.now() - 1000 * 60 * 60 * 24 * 2).toISOString(),
      status: 'active'
    }
  ],
  webhookConfig: {
    url: 'https://demo-merchant.local/api/dobropay-webhook',
    signingSecret: 'dp_sec_99a8b7c6d5e4f3a2b10987654321fedc',
    events: ['payment.succeeded', 'payment.waiting_for_capture', 'payment.canceled', 'refund.succeeded']
  },
  deliveries: [
    {
      id: 'del-001',
      eventType: 'payment.succeeded',
      paymentId: 'd9b3a5b0-8fc2-43bb-a5a1-778899aabbcc',
      url: 'https://demo-merchant.local/api/dobropay-webhook',
      status: 'delivered',
      statusCode: 200,
      attempts: 1,
      createdAt: new Date(Date.now() - 1000 * 60 * 16).toISOString(),
      payload: JSON.stringify({
        event_id: 'evt-901',
        event_type: 'payment.succeeded',
        occurred_at: new Date(Date.now() - 1000 * 60 * 16).toISOString(),
        payment: { id: 'd9b3a5b0-8fc2-43bb-a5a1-778899aabbcc', status: 'succeeded', amount: 129900, currency: 'RUB' }
      }, null, 2),
      signature: 'sha256=48fbc8a1c90192e...'
    },
    {
      id: 'del-002',
      eventType: 'payment.waiting_for_capture',
      paymentId: 'e1c4a5b0-8fc2-43bb-b5a1-998877aabbdd',
      url: 'https://demo-merchant.local/api/dobropay-webhook',
      status: 'delivered',
      statusCode: 200,
      attempts: 1,
      createdAt: new Date(Date.now() - 1000 * 60 * 34).toISOString(),
      payload: JSON.stringify({
        event_id: 'evt-902',
        event_type: 'payment.waiting_for_capture',
        occurred_at: new Date(Date.now() - 1000 * 60 * 34).toISOString(),
        payment: { id: 'e1c4a5b0-8fc2-43bb-b5a1-998877aabbdd', status: 'waiting_for_capture', amount: 450000, currency: 'RUB' }
      }, null, 2),
      signature: 'sha256=10f2c4a980...'
    },
    {
      id: 'del-003',
      eventType: 'payment.canceled',
      paymentId: 'b4f7a5b0-8fc2-43bb-e5a1-778899aabbcc',
      url: 'https://demo-merchant.local/api/dobropay-webhook',
      status: 'retrying',
      statusCode: 503,
      attempts: 2,
      nextRetryAt: new Date(Date.now() + 1000 * 60 * 4).toISOString(),
      createdAt: new Date(Date.now() - 1000 * 60 * 15).toISOString(),
      payload: JSON.stringify({
        event_id: 'evt-903',
        event_type: 'payment.canceled',
        payment: { id: 'b4f7a5b0-8fc2-43bb-e5a1-778899aabbcc', status: 'canceled', amount: 150000, currency: 'RUB' }
      }, null, 2),
      signature: 'sha256=99f8e7d6c...'
    }
  ],
  receipts: [
    {
      id: 'rec-001',
      paymentId: 'd9b3a5b0-8fc2-43bb-a5a1-778899aabbcc',
      orderNumber: '1042',
      amount: 129900,
      fiscalStatus: 'succeeded',
      fnNumber: '9999078900012345',
      fdNumber: '4021',
      fpdNumber: '3948572019',
      customerEmail: 'customer@client.ru',
      emailStatus: 'sent',
      createdAt: new Date(Date.now() - 1000 * 60 * 16).toISOString(),
      items: [
        { name: 'Керамическая кружка Dobropay (ручная работа)', price: 129900, quantity: 1, vat: 'vat20' }
      ]
    },
    {
      id: 'rec-002',
      paymentId: 'a3e6a5b0-8fc2-43bb-d5a1-445566aabbcc',
      orderNumber: '1040',
      amount: 320000,
      fiscalStatus: 'succeeded',
      fnNumber: '9999078900012345',
      fdNumber: '4019',
      fpdNumber: '1092837465',
      customerEmail: 'artur@buyer.ru',
      emailStatus: 'sent',
      createdAt: new Date(Date.now() - 1000 * 60 * 179).toISOString(),
      items: [
        { name: 'Худи Dobropay Oversize Navy L', price: 320000, quantity: 1, vat: 'vat20' }
      ]
    }
  ]
};

export class DobropayApiClient {
  constructor() {
    this.baseUrl = localStorage.getItem(STORAGE_KEYS.API_BASE) || DEFAULT_BASE_URL;
    this.token = localStorage.getItem(STORAGE_KEYS.TOKEN) || null;
    this.useMocks = localStorage.getItem(STORAGE_KEYS.USE_MOCKS) !== 'false'; // default true if standalone
  }

  setToken(token, accountId = null) {
    this.token = token;
    if (token) {
      localStorage.setItem(STORAGE_KEYS.TOKEN, token);
      if (accountId) localStorage.setItem(STORAGE_KEYS.ACCOUNT_ID, accountId);
    } else {
      localStorage.removeItem(STORAGE_KEYS.TOKEN);
      localStorage.removeItem(STORAGE_KEYS.ACCOUNT_ID);
    }
  }

  getToken() {
    return this.token || localStorage.getItem(STORAGE_KEYS.TOKEN);
  }

  setBaseUrl(url) {
    this.baseUrl = url.replace(/\/$/, '');
    localStorage.setItem(STORAGE_KEYS.API_BASE, this.baseUrl);
  }

  setUseMocks(enabled) {
    this.useMocks = !!enabled;
    localStorage.setItem(STORAGE_KEYS.USE_MOCKS, this.useMocks ? 'true' : 'false');
  }

  async request(path, options = {}) {
    const url = `${this.baseUrl}${path}`;
    const headers = {
      'Content-Type': 'application/json',
      ...options.headers
    };

    const token = this.getToken();
    if (token && !headers['Authorization']) {
      headers['Authorization'] = `Bearer ${token}`;
    }

    try {
      const response = await fetch(url, {
        ...options,
        headers
      });

      if (response.status === 401) {
        // unauthorized
      }

      const contentType = response.headers.get('content-type') || '';
      let data = null;
      if (contentType.includes('application/json')) {
        data = await response.json();
      } else {
        data = await response.text();
      }

      if (!response.ok) {
        const error = new Error((data && data.error) || response.statusText || 'Request failed');
        error.status = response.status;
        error.data = data;
        throw error;
      }

      return data;
    } catch (err) {
      if (this.useMocks) {
        console.warn(`[Dobropay API] Network request to ${path} failed or in mock mode. Using fallback simulation.`, err);
        return null;
      }
      throw err;
    }
  }

  // --- Auth ---
  async login(email, password) {
    try {
      const res = await this.request('/v1/auth/login', {
        method: 'POST',
        body: JSON.stringify({ email, password })
      });
      if (res && res.access_token) {
        this.setToken(res.access_token, res.account_id);
        return res;
      }
    } catch (e) {
      if (!this.useMocks) throw e;
    }

    // Mock Login
    const mockToken = 'mock-bearer-token-64chars-hex-11223344556677889900aabbccddeeff';
    this.setToken(mockToken, '33333333-3333-4333-8333-333333333333');
    return {
      access_token: mockToken,
      token_type: 'Bearer',
      expires_in: 28800,
      account_id: '33333333-3333-4333-8333-333333333333'
    };
  }

  logout() {
    this.setToken(null);
  }

  isAuthenticated() {
    return !!this.getToken();
  }

  // --- Checkout ---
  async getCheckoutDetails(token) {
    // Attempt real GET /checkout/{token} or status
    try {
      const res = await this.request(`/checkout/${token}/status`);
      if (res) return res;
    } catch (e) {
      if (!this.useMocks) throw e;
    }

    // Mock checkout state
    return {
      shopName: MOCK_DB.shop.name,
      amount: 129900,
      currency: 'RUB',
      externalOrderId: 'order-1042',
      status: 'pending',
      expiresAt: new Date(Date.now() + 1000 * 60 * 29 + 1000 * 45).toISOString(),
      paymentMethods: ['sbp', 'bank_card', 'crypto'],
      sbp: {
        qrPayload: 'https://qr.nspk.ru/AD10005TB5978E49A924845579C247B6?type=02&bank=100000000111&crc=D4A2',
        qrcId: 'AD10005TB5978E49A924845579C247B6'
      },
      crypto: {
        rates: {
          USDT_TRC20: { rate: 97.40, currency: 'USDT', network: 'TRC-20', address: 'TXjS51QoP8iBhykLw9mYfR79r42XN8yFkM', reqConfirmations: 3 },
          TON: { rate: 485.20, currency: 'TON', network: 'The Open Network', address: 'EQBvW8m53GoU-VJJ5au2PfqqsoTVFaP_DL', reqConfirmations: 1 },
          BTC: { rate: 6450000.00, currency: 'BTC', network: 'Bitcoin SegWit', address: 'bc1qxy2kgdygjrsqtzq2n0yrf2493p83kkfjhx0wlh', reqConfirmations: 2 }
        }
      }
    };
  }

  async startCheckoutAttempt(token, method, options = {}) {
    try {
      const res = await this.request(`/checkout/${token}/start`, {
        method: 'POST',
        body: JSON.stringify({ paymentMethodType: method, ...options })
      });
      if (res) return res;
    } catch (e) {
      if (!this.useMocks) throw e;
    }

    return { success: true, status: 'pending', method };
  }

  async simulateOutcome(token, outcome) {
    try {
      const res = await this.request(`/checkout/${token}/simulate/${outcome}`, {
        method: 'POST'
      });
      if (res) return res;
    } catch (e) {
      if (!this.useMocks) throw e;
    }

    return { simulatedOutcome: outcome, timestamp: new Date().toISOString() };
  }

  // --- Merchant Admin ---
  async getShopPayments(shopId) {
    try {
      const res = await this.request(`/v1/admin/shops/${shopId}/payments`);
      if (res) return res;
    } catch (e) {
      if (!this.useMocks) throw e;
    }
    return MOCK_DB.payments;
  }

  async capturePayment(shopId, paymentId, amount = null) {
    const key = `cap-${Date.now()}`;
    try {
      const res = await this.request(`/v1/admin/shops/${shopId}/payments/${paymentId}/capture`, {
        method: 'POST',
        headers: { 'Idempotency-Key': key },
        body: JSON.stringify({ amount })
      });
      if (res) return res;
    } catch (e) {
      if (!this.useMocks) throw e;
    }

    const p = MOCK_DB.payments.find(x => x.id === paymentId);
    if (p) {
      p.status = 'succeeded';
      p.capturedAmount = amount || p.amount;
      p.updatedAt = new Date().toISOString();
    }
    return { status: 'succeeded', capturedAmount: amount || (p && p.amount) };
  }

  async cancelPayment(shopId, paymentId, reason = 'merchant_canceled') {
    const key = `can-${Date.now()}`;
    try {
      const res = await this.request(`/v1/admin/shops/${shopId}/payments/${paymentId}/cancel`, {
        method: 'POST',
        headers: { 'Idempotency-Key': key },
        body: JSON.stringify({ reason })
      });
      if (res) return res;
    } catch (e) {
      if (!this.useMocks) throw e;
    }

    const p = MOCK_DB.payments.find(x => x.id === paymentId);
    if (p) {
      p.status = 'canceled';
      p.cancellationReason = reason;
      p.updatedAt = new Date().toISOString();
    }
    return { status: 'canceled' };
  }

  async refundPayment(shopId, paymentId, amount, reason = '') {
    const key = `ref-${Date.now()}`;
    try {
      const res = await this.request(`/v1/admin/shops/${shopId}/payments/${paymentId}/refunds`, {
        method: 'POST',
        headers: { 'Idempotency-Key': key },
        body: JSON.stringify({ amount, reason })
      });
      if (res) return res;
    } catch (e) {
      if (!this.useMocks) throw e;
    }

    const p = MOCK_DB.payments.find(x => x.id === paymentId);
    if (p) {
      p.refundedAmount = (p.refundedAmount || 0) + amount;
      p.refunds = p.refunds || [];
      const refundRecord = {
        id: `ref-${Date.now()}`,
        amount,
        reason,
        status: 'succeeded',
        createdAt: new Date().toISOString()
      };
      p.refunds.unshift(refundRecord);
      return refundRecord;
    }
    return { id: `ref-${Date.now()}`, amount, status: 'succeeded' };
  }

  async getDeliveries(shopId) {
    try {
      const res = await this.request(`/v1/admin/shops/${shopId}/deliveries`);
      if (res) return res;
    } catch (e) {
      if (!this.useMocks) throw e;
    }
    return MOCK_DB.deliveries;
  }

  async getReceipts(shopId) {
    try {
      const res = await this.request(`/v1/admin/shops/${shopId}/receipts`);
      if (res) return res;
    } catch (e) {
      if (!this.useMocks) throw e;
    }
    return MOCK_DB.receipts;
  }

  async createApiKey(shopId, name = 'Новый API ключ') {
    try {
      const res = await this.request(`/v1/developer/shops/${shopId}/keys`, {
        method: 'POST',
        body: JSON.stringify({ name })
      });
      if (res) return res;
    } catch (e) {
      if (!this.useMocks) throw e;
    }

    const randomSuffix = Array.from({ length: 32 }, () => Math.floor(Math.random() * 16).toString(16)).join('');
    const fullKey = `dp_live_${randomSuffix}`;
    const newKeyRecord = {
      id: `key-${Date.now()}`,
      name,
      prefix: `dp_live_${randomSuffix.substring(0, 6)}...`,
      apiKey: fullKey,
      createdAt: new Date().toISOString(),
      status: 'active'
    };
    MOCK_DB.apiKeys.unshift(newKeyRecord);
    return newKeyRecord;
  }

  async getApiKeys(shopId) {
    return MOCK_DB.apiKeys;
  }

  async revokeApiKey(keyId) {
    const idx = MOCK_DB.apiKeys.findIndex(k => k.id === keyId);
    if (idx !== -1) {
      MOCK_DB.apiKeys.splice(idx, 1);
    }
    return { success: true };
  }

  async getWebhookConfig(shopId) {
    return MOCK_DB.webhookConfig;
  }

  async updateWebhookConfig(shopId, config) {
    try {
      const res = await this.request(`/v1/developer/shops/${shopId}/webhooks`, {
        method: 'PUT',
        body: JSON.stringify(config)
      });
      if (res) return res;
    } catch (e) {
      if (!this.useMocks) throw e;
    }

    MOCK_DB.webhookConfig = { ...MOCK_DB.webhookConfig, ...config };
    return MOCK_DB.webhookConfig;
  }

  async submitShopOnboarding(data) {
    // 3-step onboarding simulation
    MOCK_DB.shop = {
      ...MOCK_DB.shop,
      ...data,
      status: 'under_review'
    };
    return { success: true, shop: MOCK_DB.shop };
  }

  getShopData() {
    return MOCK_DB.shop;
  }
}

export const api = new DobropayApiClient();
