import { api } from './api.js';
import { formatMoney, formatDateTime, copyToClipboard, showToast, validateInn, validateBik, validateAccount } from './utils.js';

class MerchantAdminApp {
  constructor() {
    this.currentTab = 'payments';
    this.shop = api.getShopData();
    this.payments = [];
    this.deliveries = [];
    this.receipts = [];
    this.apiKeys = [];
    this.selectedPayment = null;
    this.onboardingStep = 1;
    this.filters = {
      status: 'all',
      method: 'all',
      query: ''
    };
  }

  async init() {
    this.bindEvents();
    this.checkAuth();
    await this.loadData();
    const urlParams = new URLSearchParams(window.location.search);
    const initialTab = urlParams.get('tab') || window.location.hash.replace('#', '') || 'payments';
    this.switchTab(initialTab);
  }

  checkAuth() {
    const isAuth = api.isAuthenticated();
    const loginBtn = document.getElementById('topbar-login-btn');
    const userBadge = document.getElementById('topbar-user-badge');

    if (isAuth) {
      if (loginBtn) loginBtn.style.display = 'none';
      if (userBadge) userBadge.style.display = 'inline-flex';
    } else {
      if (loginBtn) loginBtn.style.display = 'inline-flex';
      if (userBadge) userBadge.style.display = 'none';
    }
  }

  async loadData() {
    this.payments = await api.getShopPayments(this.shop.id);
    this.deliveries = await api.getDeliveries(this.shop.id);
    this.receipts = await api.getReceipts(this.shop.id);
    this.apiKeys = await api.getApiKeys(this.shop.id);
    this.updateKpi();
    this.renderPaymentsTable();
    this.renderDeliveriesTable();
    this.renderReceiptsTable();
    this.renderApiKeysTable();
    this.renderWebhookConfig();
  }

  bindEvents() {
    // Navigation Tabs
    document.querySelectorAll('.admin-nav-item').forEach(item => {
      item.addEventListener('click', (e) => {
        const tab = e.currentTarget.dataset.tab;
        this.switchTab(tab);
      });
    });

    // Payment Filters
    document.getElementById('filter-status')?.addEventListener('change', (e) => {
      this.filters.status = e.target.value;
      this.renderPaymentsTable();
    });

    document.getElementById('filter-method')?.addEventListener('change', (e) => {
      this.filters.method = e.target.value;
      this.renderPaymentsTable();
    });

    document.getElementById('filter-search')?.addEventListener('input', (e) => {
      this.filters.query = e.target.value.toLowerCase().trim();
      this.renderPaymentsTable();
    });

    // Create Payment Modal
    document.getElementById('btn-open-create-payment')?.addEventListener('click', () => {
      this.openModal('modal-create-payment');
    });

    document.getElementById('form-create-payment')?.addEventListener('submit', (e) => {
      e.preventDefault();
      this.handleCreatePayment();
    });

    // Capture Payment Submit
    document.getElementById('form-capture-payment')?.addEventListener('submit', (e) => {
      e.preventDefault();
      this.handleCaptureSubmit();
    });

    // Refund Payment Submit
    document.getElementById('form-refund-payment')?.addEventListener('submit', (e) => {
      e.preventDefault();
      this.handleRefundSubmit();
    });

    // API Key Generation
    document.getElementById('btn-open-create-key')?.addEventListener('click', () => {
      this.openModal('modal-create-key');
    });

    document.getElementById('form-create-key')?.addEventListener('submit', (e) => {
      e.preventDefault();
      this.handleCreateApiKey();
    });

    // Webhook Settings Form
    document.getElementById('form-webhook-settings')?.addEventListener('submit', (e) => {
      e.preventDefault();
      this.handleSaveWebhookSettings();
    });

    // Auth Modal
    document.getElementById('topbar-login-btn')?.addEventListener('click', () => {
      this.openModal('modal-auth');
    });

    document.getElementById('btn-logout')?.addEventListener('click', () => {
      api.logout();
      this.checkAuth();
      showToast('Вы вышли из личного кабинета', 'info');
    });

    document.getElementById('form-login')?.addEventListener('submit', async (e) => {
      e.preventDefault();
      const email = document.getElementById('login-email').value;
      const pass = document.getElementById('login-password').value;
      try {
        await api.login(email, pass);
        this.closeModal('modal-auth');
        this.checkAuth();
        showToast('Вход выполнен успешно!', 'success');
      } catch (err) {
        showToast(err.message || 'Ошибка входа', 'error');
      }
    });

    document.getElementById('btn-fill-demo-auth')?.addEventListener('click', () => {
      document.getElementById('login-email').value = 'demo@dobropay.local';
      document.getElementById('login-password').value = 'local-demo-password';
    });

    // Onboarding Form Steps
    document.getElementById('btn-onboarding-step1-next')?.addEventListener('click', () => {
      this.setOnboardingStep(2);
    });

    document.getElementById('btn-onboarding-step2-prev')?.addEventListener('click', () => {
      this.setOnboardingStep(1);
    });

    document.getElementById('form-onboarding-step2')?.addEventListener('submit', (e) => {
      e.preventDefault();
      this.handleOnboardingSubmit();
    });

    // Drawer Close
    document.getElementById('btn-close-drawer')?.addEventListener('click', () => {
      this.closeDrawer();
    });

    // General Modal Closes
    document.querySelectorAll('.modal-close').forEach(btn => {
      btn.addEventListener('click', (e) => {
        const modal = e.target.closest('.modal-overlay');
        if (modal) modal.classList.remove('active');
      });
    });
  }

  switchTab(tab) {
    this.currentTab = tab;
    window.location.hash = tab;

    document.querySelectorAll('.admin-nav-item').forEach(i => {
      i.classList.toggle('active', i.dataset.tab === tab);
    });

    document.querySelectorAll('.admin-view').forEach(v => {
      v.style.display = v.id === `view-${tab}` ? 'flex' : 'none';
    });
  }

  updateKpi() {
    let todayVolume = 0;
    let succeededCount = 0;
    let holdAmount = 0;
    let refundAmount = 0;

    for (const p of this.payments) {
      if (p.status === 'succeeded') {
        todayVolume += p.capturedAmount || p.amount;
        succeededCount++;
      } else if (p.status === 'waiting_for_capture') {
        holdAmount += p.amount;
      }
      if (p.refundedAmount) {
        refundAmount += p.refundedAmount;
      }
    }

    document.getElementById('kpi-volume').textContent = formatMoney(todayVolume);
    document.getElementById('kpi-succeeded').textContent = succeededCount;
    document.getElementById('kpi-hold').textContent = formatMoney(holdAmount);
    document.getElementById('kpi-refunds').textContent = formatMoney(refundAmount);
  }

  renderPaymentsTable() {
    const tbody = document.getElementById('payments-tbody');
    if (!tbody) return;

    const filtered = this.payments.filter(p => {
      if (this.filters.status !== 'all' && p.status !== this.filters.status) return false;
      if (this.filters.method !== 'all' && p.paymentMethodType !== this.filters.method) return false;
      if (this.filters.query) {
        const matchOrder = (p.externalOrderId || '').toLowerCase().includes(this.filters.query);
        const matchId = p.id.toLowerCase().includes(this.filters.query);
        if (!matchOrder && !matchId) return false;
      }
      return true;
    });

    if (filtered.length === 0) {
      tbody.innerHTML = `<tr><td colspan="6" style="text-align: center; color: rgba(9, 49, 141, 0.5); padding: 30px;">Платежи не найдены</td></tr>`;
      return;
    }

    tbody.innerHTML = filtered.map(p => {
      const badgeClass = this.getStatusBadgeClass(p.status);
      const statusTitle = this.getStatusTitle(p.status);
      const methodName = this.getMethodShortName(p.paymentMethodType);
      
      let quickAction = '';
      if (p.status === 'waiting_for_capture') {
        quickAction = `<button class="button sm primary btn-quick-capture" data-id="${p.id}">Списать</button>`;
      } else if (p.status === 'succeeded') {
        quickAction = `<button class="button sm outline btn-quick-refund" data-id="${p.id}">Возврат</button>`;
      }

      return `
        <tr>
          <td>
            <div style="font-weight: 600; color: var(--blue);">${p.externalOrderId || '—'}</div>
            <div class="mono" style="font-size: 11px; color: rgba(9, 49, 141, 0.5);">${p.id.substring(0, 8)}...</div>
          </td>
          <td style="font-size: 13px; color: rgba(9, 49, 141, 0.7);">${formatDateTime(p.createdAt)}</td>
          <td>
            <div style="font-weight: 700; font-size: 15px; color: var(--blue);">${formatMoney(p.amount)}</div>
            ${p.refundedAmount ? `<div style="font-size: 11px; color: var(--red);">Возврат: ${formatMoney(p.refundedAmount)}</div>` : ''}
          </td>
          <td><span class="badge neutral" style="font-size: 11px;">${methodName}</span></td>
          <td><span class="badge ${badgeClass}">${statusTitle}</span></td>
          <td class="td-actions">
            <button class="button sm ghost btn-view-payment" data-id="${p.id}">Детали</button>
            ${quickAction}
          </td>
        </tr>
      `;
    }).join('');

    // Bind row action clicks
    tbody.querySelectorAll('.btn-view-payment').forEach(b => {
      b.addEventListener('click', (e) => this.openPaymentDrawer(e.currentTarget.dataset.id));
    });

    tbody.querySelectorAll('.btn-quick-capture').forEach(b => {
      b.addEventListener('click', (e) => this.openCaptureModal(e.currentTarget.dataset.id));
    });

    tbody.querySelectorAll('.btn-quick-refund').forEach(b => {
      b.addEventListener('click', (e) => this.openRefundModal(e.currentTarget.dataset.id));
    });
  }

  openPaymentDrawer(paymentId) {
    const p = this.payments.find(x => x.id === paymentId);
    if (!p) return;
    this.selectedPayment = p;

    document.getElementById('drawer-order-id').textContent = p.externalOrderId || p.id;
    document.getElementById('drawer-id').textContent = p.id;
    document.getElementById('drawer-status').className = `badge ${this.getStatusBadgeClass(p.status)}`;
    document.getElementById('drawer-status').textContent = this.getStatusTitle(p.status);
    document.getElementById('drawer-amount').textContent = formatMoney(p.amount);
    document.getElementById('drawer-created').textContent = formatDateTime(p.createdAt);
    document.getElementById('drawer-method').textContent = this.getMethodShortName(p.paymentMethodType);

    const remaining = p.amount - (p.refundedAmount || 0);
    document.getElementById('drawer-remaining').textContent = formatMoney(remaining);

    // Actions in drawer footer
    const footer = document.getElementById('drawer-actions-container');
    if (p.status === 'waiting_for_capture') {
      footer.innerHTML = `
        <button class="button primary flex-1" id="drawer-btn-capture">Подтвердить списание (Capture)</button>
        <button class="button outline flex-1" id="drawer-btn-cancel">Отменить холд (Cancel)</button>
      `;
      document.getElementById('drawer-btn-capture').addEventListener('click', () => {
        this.closeDrawer();
        this.openCaptureModal(p.id);
      });
      document.getElementById('drawer-btn-cancel').addEventListener('click', async () => {
        if (confirm('Вы уверены, что хотите отменить холд и разблокировать средства клиенту?')) {
          await api.cancelPayment(this.shop.id, p.id);
          this.closeDrawer();
          await this.loadData();
          showToast('Холд успешно отменён', 'info');
        }
      });
    } else if (p.status === 'succeeded' && remaining > 0) {
      footer.innerHTML = `
        <button class="button primary flex-1" id="drawer-btn-refund">Оформить возврат (Refund)</button>
        <a href="checkout.html?token=${p.id}&status=succeeded" target="_blank" class="button outline flex-1">Открыть чек</a>
      `;
      document.getElementById('drawer-btn-refund').addEventListener('click', () => {
        this.closeDrawer();
        this.openRefundModal(p.id);
      });
    } else {
      footer.innerHTML = `<button class="button outline full" id="drawer-btn-close-sub">Закрыть</button>`;
      document.getElementById('drawer-btn-close-sub').addEventListener('click', () => this.closeDrawer());
    }

    document.getElementById('drawer-overlay').classList.add('active');
  }

  closeDrawer() {
    document.getElementById('drawer-overlay').classList.remove('active');
  }

  openCaptureModal(paymentId) {
    const p = this.payments.find(x => x.id === paymentId);
    if (!p) return;
    this.selectedPayment = p;

    document.getElementById('capture-order-title').textContent = `Заказ №${p.externalOrderId || p.id.substring(0, 8)}`;
    document.getElementById('capture-total-hint').textContent = `Полная сумма заказа: ${formatMoney(p.amount)}`;
    document.getElementById('capture-amount-input').value = (p.amount / 100).toFixed(2);
    document.getElementById('capture-amount-input').max = (p.amount / 100).toFixed(2);

    this.openModal('modal-capture');
  }

  async handleCaptureSubmit() {
    const p = this.selectedPayment;
    if (!p) return;

    const rubles = parseFloat(document.getElementById('capture-amount-input').value);
    const kopecks = Math.round(rubles * 100);

    if (isNaN(kopecks) || kopecks <= 0 || kopecks > p.amount) {
      showToast('Некорректная сумма списания', 'error');
      return;
    }

    try {
      await api.capturePayment(this.shop.id, p.id, kopecks);
      this.closeModal('modal-capture');
      await this.loadData();
      showToast(`Списание на сумму ${formatMoney(kopecks)} подтверждено!`, 'success');
    } catch (err) {
      showToast(err.message || 'Ошибка списания', 'error');
    }
  }

  openRefundModal(paymentId) {
    const p = this.payments.find(x => x.id === paymentId);
    if (!p) return;
    this.selectedPayment = p;

    const remaining = p.amount - (p.refundedAmount || 0);
    document.getElementById('refund-order-title').textContent = `Заказ №${p.externalOrderId || p.id.substring(0, 8)}`;
    document.getElementById('refund-available-hint').textContent = `Доступно для возврата: ${formatMoney(remaining)}`;
    document.getElementById('refund-amount-input').value = (remaining / 100).toFixed(2);
    document.getElementById('refund-amount-input').max = (remaining / 100).toFixed(2);
    document.getElementById('refund-reason-input').value = 'Возврат товара покупателем';

    this.openModal('modal-refund');
  }

  async handleRefundSubmit() {
    const p = this.selectedPayment;
    if (!p) return;

    const rubles = parseFloat(document.getElementById('refund-amount-input').value);
    const kopecks = Math.round(rubles * 100);
    const reason = document.getElementById('refund-reason-input').value.trim();
    const remaining = p.amount - (p.refundedAmount || 0);

    if (isNaN(kopecks) || kopecks <= 0 || kopecks > remaining) {
      showToast('Сумма возврата превышает доступный остаток', 'error');
      return;
    }

    try {
      await api.refundPayment(this.shop.id, p.id, kopecks, reason);
      this.closeModal('modal-refund');
      await this.loadData();
      showToast(`Возврат ${formatMoney(kopecks)} успешно проведён!`, 'success');
    } catch (err) {
      showToast(err.message || 'Ошибка возврата', 'error');
    }
  }

  async handleCreatePayment() {
    const amountRubles = parseFloat(document.getElementById('new-payment-amount').value);
    const kopecks = Math.round(amountRubles * 100);
    const orderId = document.getElementById('new-payment-order-id').value.trim();
    const method = document.getElementById('new-payment-method').value;
    const capture = document.getElementById('new-payment-capture-toggle').checked;

    if (isNaN(kopecks) || kopecks <= 0) {
      showToast('Укажите корректную сумму', 'error');
      return;
    }

    const newPayment = {
      id: `pay-${Date.now()}`,
      externalOrderId: orderId || `order-${Math.floor(1000 + Math.random() * 9000)}`,
      amount: kopecks,
      currency: 'RUB',
      status: 'pending',
      paymentMethodType: method,
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
      capturedAmount: 0,
      refundedAmount: 0,
      refunds: []
    };

    this.payments.unshift(newPayment);
    this.closeModal('modal-create-payment');
    this.updateKpi();
    this.renderPaymentsTable();

    // Show generated checkout link dialog
    const checkoutUrl = `checkout.html?token=${newPayment.id}`;
    document.getElementById('created-checkout-url').value = window.location.origin + window.location.pathname.replace('index.html', '') + checkoutUrl;
    document.getElementById('created-checkout-open-btn').href = checkoutUrl;
    this.openModal('modal-payment-created');
  }

  renderDeliveriesTable() {
    const tbody = document.getElementById('deliveries-tbody');
    if (!tbody) return;

    if (this.deliveries.length === 0) {
      tbody.innerHTML = `<tr><td colspan="6" style="text-align: center; color: rgba(9, 49, 141, 0.5); padding: 24px;">Журнал доставок пуст</td></tr>`;
      return;
    }

    tbody.innerHTML = this.deliveries.map(d => {
      const isOk = d.statusCode === 200;
      return `
        <tr>
          <td><span class="mono" style="font-weight:600; color:var(--blue);">${d.eventType}</span></td>
          <td class="mono" style="font-size:12px; color:rgba(9, 49, 141, 0.7); max-width: 220px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap;">${d.url}</td>
          <td>
            <span class="badge ${isOk ? 'succeeded' : 'waiting'}">
              HTTP ${d.statusCode}
            </span>
          </td>
          <td style="text-align:center; font-weight:600;">${d.attempts}</td>
          <td style="font-size:12px; color:rgba(9, 49, 141, 0.65);">${formatDateTime(d.createdAt)}</td>
          <td class="td-actions">
            <button class="button sm ghost btn-inspect-delivery" data-id="${d.id}">Инспектор</button>
            <button class="button sm outline btn-retry-delivery" data-id="${d.id}">Повторить</button>
          </td>
        </tr>
      `;
    }).join('');

    tbody.querySelectorAll('.btn-inspect-delivery').forEach(b => {
      b.addEventListener('click', (e) => this.openDeliveryInspector(e.currentTarget.dataset.id));
    });

    tbody.querySelectorAll('.btn-retry-delivery').forEach(b => {
      b.addEventListener('click', (e) => {
        showToast('Повторная отправка Webhook запущена...', 'info');
        setTimeout(() => {
          showToast('Webhook успешно доставлен (HTTP 200)', 'success');
        }, 1200);
      });
    });
  }

  openDeliveryInspector(deliveryId) {
    const d = this.deliveries.find(x => x.id === deliveryId);
    if (!d) return;

    document.getElementById('inspect-event-type').textContent = d.eventType;
    document.getElementById('inspect-url').textContent = d.url;
    document.getElementById('inspect-signature').textContent = d.signature;
    document.getElementById('inspect-payload-code').textContent = d.payload;

    this.openModal('modal-inspect-delivery');
  }

  renderReceiptsTable() {
    const tbody = document.getElementById('receipts-tbody');
    if (!tbody) return;

    tbody.innerHTML = this.receipts.map(r => `
      <tr>
        <td>
          <div style="font-weight:600; color:var(--blue);">Заказ №${r.orderNumber}</div>
          <div class="mono" style="font-size:11px; color:rgba(9, 49, 141, 0.5);">${r.id}</div>
        </td>
        <td class="mono" style="font-size:12px;">ФД №${r.fdNumber} · ФПД ${r.fpdNumber}</td>
        <td style="font-weight:700; color:var(--blue);">${formatMoney(r.amount)}</td>
        <td style="font-size:13px;">${r.customerEmail}</td>
        <td><span class="badge paid">Фискализирован</span></td>
        <td><span class="badge succeeded">Отправлен</span></td>
        <td class="td-actions">
          <button class="button sm ghost btn-preview-receipt" data-id="${r.id}">Печатная форма</button>
        </td>
      </tr>
    `).join('');

    tbody.querySelectorAll('.btn-preview-receipt').forEach(b => {
      b.addEventListener('click', (e) => this.openReceiptPreview(e.currentTarget.dataset.id));
    });
  }

  openReceiptPreview(receiptId) {
    const r = this.receipts.find(x => x.id === receiptId);
    if (!r) return;

    document.getElementById('admin-receipt-order-no').textContent = r.orderNumber;
    document.getElementById('admin-receipt-fn').textContent = r.fnNumber;
    document.getElementById('admin-receipt-fd').textContent = `${r.fdNumber} / ${r.fpdNumber}`;
    document.getElementById('admin-receipt-date').textContent = formatDateTime(r.createdAt);
    document.getElementById('admin-receipt-amount').textContent = formatMoney(r.amount);

    this.openModal('modal-admin-receipt');
  }

  renderApiKeysTable() {
    const tbody = document.getElementById('keys-tbody');
    if (!tbody) return;

    tbody.innerHTML = this.apiKeys.map(k => `
      <tr>
        <td style="font-weight:600; color:var(--blue);">${k.name}</td>
        <td><span class="mono" style="font-size:13px; font-weight:600;">${k.prefix}</span></td>
        <td style="font-size:13px; color:rgba(9, 49, 141, 0.65);">${formatDateTime(k.createdAt)}</td>
        <td><span class="badge succeeded">Активен</span></td>
        <td class="td-actions">
          <button class="button sm outline btn-revoke-key" data-id="${k.id}">Отозвать</button>
        </td>
      </tr>
    `).join('');

    tbody.querySelectorAll('.btn-revoke-key').forEach(b => {
      b.addEventListener('click', async (e) => {
        if (confirm('Отозвать данный API-ключ? Интеграции, использующие его, перестанут работать.')) {
          await api.revokeApiKey(e.currentTarget.dataset.id);
          await this.loadData();
          showToast('API-ключ отозван', 'info');
        }
      });
    });
  }

  async handleCreateApiKey() {
    const name = document.getElementById('new-key-name').value.trim() || 'API Ключ приложения';
    const keyRecord = await api.createApiKey(this.shop.id, name);
    this.closeModal('modal-create-key');
    await this.loadData();

    document.getElementById('revealed-api-key').textContent = keyRecord.apiKey;
    document.getElementById('btn-copy-revealed-key').onclick = () => {
      copyToClipboard(keyRecord.apiKey, 'API ключ скопирован!');
    };
    this.openModal('modal-key-revealed');
  }

  async renderWebhookConfig() {
    const cfg = await api.getWebhookConfig(this.shop.id);
    if (!cfg) return;

    document.getElementById('webhook-url-input').value = cfg.url || '';
    document.getElementById('webhook-secret-input').value = cfg.signingSecret || '';

    ['payment.succeeded', 'payment.waiting_for_capture', 'payment.canceled', 'refund.succeeded'].forEach(ev => {
      const checkbox = document.getElementById(`ev-${ev.replace('.', '_')}`);
      if (checkbox) checkbox.checked = cfg.events.includes(ev);
    });
  }

  async handleSaveWebhookSettings() {
    const url = document.getElementById('webhook-url-input').value.trim();
    const signingSecret = document.getElementById('webhook-secret-input').value.trim();

    const events = [];
    ['payment.succeeded', 'payment.waiting_for_capture', 'payment.canceled', 'refund.succeeded'].forEach(ev => {
      const checkbox = document.getElementById(`ev-${ev.replace('.', '_')}`);
      if (checkbox && checkbox.checked) events.push(ev);
    });

    await api.updateWebhookConfig(this.shop.id, { url, signingSecret, events });
    showToast('Настройки Webhook сохранены!', 'success');
  }

  setOnboardingStep(step) {
    this.onboardingStep = step;
    document.querySelectorAll('.wizard-step-node').forEach(node => {
      const nodeStep = parseInt(node.dataset.step, 10);
      node.classList.toggle('active', nodeStep === step);
      node.classList.toggle('completed', nodeStep < step);
    });

    document.querySelectorAll('.wizard-card-step').forEach(card => {
      card.style.display = parseInt(card.dataset.step, 10) === step ? 'block' : 'none';
    });
  }

  async handleOnboardingSubmit() {
    const inn = document.getElementById('onboard-inn').value.trim();
    const ogrn = document.getElementById('onboard-ogrn').value.trim();
    const legalName = document.getElementById('onboard-legal-name').value.trim();
    const checking = document.getElementById('onboard-account').value.trim();
    const bik = document.getElementById('onboard-bik').value.trim();

    if (!validateInn(inn)) {
      showToast('ИНН должен содержать 10 или 12 цифр и верный контрольный разряд', 'error');
      return;
    }
    if (!validateBik(bik)) {
      showToast('БИК банка должен состоять из 9 цифр и начинаться на 04', 'error');
      return;
    }
    if (!validateAccount(checking)) {
      showToast('Расчётный счёт должен состоять ровно из 20 цифр', 'error');
      return;
    }

    await api.submitShopOnboarding({ inn, ogrn, legalName, checkingAccount: checking, bik });
    this.setOnboardingStep(3);
    showToast('Заявка на подключение магазина успешно отправлена!', 'success');
  }

  getStatusBadgeClass(s) {
    if (s === 'succeeded') return 'succeeded';
    if (s === 'waiting_for_capture') return 'waiting';
    if (s === 'canceled') return 'canceled';
    return 'neutral';
  }

  getStatusTitle(s) {
    if (s === 'succeeded') return 'Оплачено';
    if (s === 'waiting_for_capture') return 'В холде (Ожидает)';
    if (s === 'canceled') return 'Отменено';
    if (s === 'pending') return 'Ожидает оплаты';
    return s;
  }

  getMethodShortName(m) {
    if (m === 'sbp') return 'СБП';
    if (m === 'bank_card') return 'Карта';
    if (m === 'crypto') return 'Крипта';
    return m;
  }

  openModal(id) {
    const m = document.getElementById(id);
    if (m) m.classList.add('active');
  }

  closeModal(id) {
    const m = document.getElementById(id);
    if (m) m.classList.remove('active');
  }
}

document.addEventListener('DOMContentLoaded', () => {
  const app = new MerchantAdminApp();
  app.init();
  window.dobropayAdmin = app;
});
