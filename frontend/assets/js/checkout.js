import { api } from './api.js';
import { formatMoney, formatDateTime, copyToClipboard, showToast, detectCardBrand, formatCardNumber, formatExpiryDate, validateCardNumberLuhn } from './utils.js';
import { renderQrSvg } from './qrcode.js';

class CheckoutApp {
  constructor() {
    this.token = this.extractToken();
    this.data = null;
    this.activeMethod = 'sbp';
    this.activeCryptoNetwork = 'USDT_TRC20';
    this.timerInterval = null;
    this.remainingSeconds = 30 * 60; // 30 minutes default
    this.currentStatus = 'pending';
  }

  extractToken() {
    const params = new URLSearchParams(window.location.search);
    if (params.get('token')) return params.get('token');
    const pathParts = window.location.pathname.split('/');
    const lastPart = pathParts[pathParts.length - 1];
    return lastPart && lastPart !== 'checkout.html' && lastPart !== '' ? lastPart : 'demo-checkout-token-2026';
  }

  async init() {
    this.bindEvents();
    await this.loadCheckoutData();
    this.startTimer();
    this.render();
  }

  async loadCheckoutData() {
    this.data = await api.getCheckoutDetails(this.token);
    this.currentStatus = this.data.status || 'pending';
    if (this.data.expiresAt) {
      const diff = Math.max(0, Math.floor((new Date(this.data.expiresAt) - Date.now()) / 1000));
      this.remainingSeconds = diff > 0 ? diff : 0;
    }
  }

  startTimer() {
    if (this.timerInterval) clearInterval(this.timerInterval);
    const updateDisplay = () => {
      const minutes = Math.floor(this.remainingSeconds / 60);
      const seconds = this.remainingSeconds % 60;
      const timerEl = document.getElementById('checkout-timer');
      const badgeEl = document.getElementById('checkout-timer-badge');

      if (timerEl) {
        timerEl.textContent = `${String(minutes).padStart(2, '0')}:${String(seconds).padStart(2, '0')}`;
      }

      if (badgeEl) {
        if (this.remainingSeconds < 300) {
          badgeEl.classList.add('warning');
        } else {
          badgeEl.classList.remove('warning');
        }
      }

      if (this.remainingSeconds <= 0) {
        clearInterval(this.timerInterval);
        this.setStatus('canceled', 'Время действия платёжной ссылки истекло (30 минут)');
      } else {
        this.remainingSeconds--;
      }
    };

    updateDisplay();
    this.timerInterval = setInterval(updateDisplay, 1000);
  }

  bindEvents() {
    // Method Tabs
    document.querySelectorAll('.method-tab').forEach(tab => {
      tab.addEventListener('click', (e) => {
        const method = e.currentTarget.dataset.method;
        this.switchMethod(method);
      });
    });

    // Card Input Formats
    const cardInput = document.getElementById('card-number');
    if (cardInput) {
      cardInput.addEventListener('input', (e) => {
        const formatted = formatCardNumber(e.target.value);
        e.target.value = formatted;
        this.updateCardBrand(formatted);
      });
    }

    const expiryInput = document.getElementById('card-expiry');
    if (expiryInput) {
      expiryInput.addEventListener('input', (e) => {
        e.target.value = formatExpiryDate(e.target.value);
      });
    }

    const cvvInput = document.getElementById('card-cvv');
    if (cvvInput) {
      cvvInput.addEventListener('input', (e) => {
        e.target.value = e.target.value.replace(/\D/g, '').substring(0, 4);
      });
    }

    // Card Payment Submit
    const cardForm = document.getElementById('card-payment-form');
    if (cardForm) {
      cardForm.addEventListener('submit', (e) => {
        e.preventDefault();
        this.handleCardSubmit();
      });
    }

    // SBP DeepLink
    const sbpBtn = document.getElementById('sbp-deeplink-btn');
    if (sbpBtn) {
      sbpBtn.addEventListener('click', () => {
        showToast('Открытие приложения банка через СБП...', 'info');
        setTimeout(() => {
          this.setStatus('succeeded');
        }, 3000);
      });
    }

    // Crypto Network Pills
    document.querySelectorAll('.network-pill').forEach(pill => {
      pill.addEventListener('click', (e) => {
        const net = e.currentTarget.dataset.network;
        this.switchCryptoNetwork(net);
      });
    });

    // Copy Crypto Address
    const copyAddrBtn = document.getElementById('copy-crypto-address-btn');
    if (copyAddrBtn) {
      copyAddrBtn.addEventListener('click', () => {
        const addr = document.getElementById('crypto-address-text')?.textContent;
        if (addr) copyToClipboard(addr, 'Адрес криптокошелька скопирован!');
      });
    }

    // Copy Crypto Amount
    const copyAmountBtn = document.getElementById('copy-crypto-amount-btn');
    if (copyAmountBtn) {
      copyAmountBtn.addEventListener('click', () => {
        const amt = document.getElementById('crypto-amount-text')?.textContent;
        if (amt) copyToClipboard(amt, 'Сумма скопирована!');
      });
    }

    // 3DS Modal Events
    document.getElementById('submit-3ds-btn')?.addEventListener('click', () => {
      this.closeModal('modal-3ds');
      showToast('3-D Secure проверка пройдена', 'success');
      setTimeout(() => this.setStatus('succeeded'), 800);
    });

    document.getElementById('cancel-3ds-btn')?.addEventListener('click', () => {
      this.closeModal('modal-3ds');
      this.setStatus('canceled', 'Отказ при проверке 3-D Secure');
    });

    // Receipt Modal Close
    document.querySelectorAll('.modal-close, .modal-backdrop').forEach(el => {
      el.addEventListener('click', (e) => {
        const modal = e.target.closest('.modal-overlay');
        if (modal) modal.classList.remove('active');
      });
    });

    // View Receipt Button
    document.getElementById('view-receipt-btn')?.addEventListener('click', () => {
      this.openReceiptModal();
    });

    // Retry Button
    document.getElementById('retry-payment-btn')?.addEventListener('click', () => {
      this.resetCheckout();
    });
  }

  switchMethod(method) {
    this.activeMethod = method;
    document.querySelectorAll('.method-tab').forEach(t => {
      t.classList.toggle('active', t.dataset.method === method);
    });
    document.querySelectorAll('.method-panel').forEach(p => {
      p.classList.toggle('active', p.id === `panel-${method}`);
    });

    if (method === 'sbp') {
      this.renderSbpQr();
    } else if (method === 'crypto') {
      this.renderCryptoDetails();
    }
  }

  switchCryptoNetwork(network) {
    this.activeCryptoNetwork = network;
    document.querySelectorAll('.network-pill').forEach(p => {
      p.classList.toggle('active', p.dataset.network === network);
    });
    this.renderCryptoDetails();
  }

  updateCardBrand(cardNumber) {
    const brandInfo = detectCardBrand(cardNumber);
    const brandContainer = document.getElementById('card-brand-display');
    if (!brandContainer) return;

    if (brandInfo.brand === 'mir') {
      brandContainer.innerHTML = `<span class="badge neutral" style="font-weight:700;color:#009949;">МИР</span>`;
    } else if (brandInfo.brand === 'visa') {
      brandContainer.innerHTML = `<span class="badge neutral" style="font-weight:700;color:#1A1F71;">VISA</span>`;
    } else if (brandInfo.brand === 'mastercard') {
      brandContainer.innerHTML = `<span class="badge neutral" style="font-weight:700;color:#EB001B;">MC</span>`;
    } else {
      brandContainer.innerHTML = `<span class="badge neutral" style="font-size:11px;">КАРТА</span>`;
    }
  }

  render() {
    if (!this.data) return;

    // Fill Shop & Order Info
    document.getElementById('shop-name').textContent = this.data.shopName || 'Магазин Dobropay';
    document.getElementById('order-id').textContent = `Заказ №${this.data.externalOrderId || '1042'}`;
    const formattedAmount = formatMoney(this.data.amount);
    document.getElementById('order-amount').textContent = formattedAmount;
    document.getElementById('card-pay-btn-amount').textContent = formattedAmount;

    // Query params for direct tab/status preview
    const urlParams = new URLSearchParams(window.location.search);
    if (urlParams.get('tab')) {
      this.activeMethod = urlParams.get('tab');
    }
    if (urlParams.get('status')) {
      this.currentStatus = urlParams.get('status');
    }

    // Render active panel
    this.switchMethod(this.activeMethod);

    if (this.currentStatus !== 'pending') {
      this.setStatus(this.currentStatus);
    }
  }

  renderSbpQr() {
    const qrContainer = document.getElementById('sbp-qr-code');
    const payload = (this.data && this.data.sbp && this.data.sbp.qrPayload) || 'https://qr.nspk.ru/AD10005TB5978E49A924845579C247B6';
    renderQrSvg(payload, qrContainer, 210);
  }

  renderCryptoDetails() {
    const netConfig = (this.data && this.data.crypto && this.data.crypto.rates && this.data.crypto.rates[this.activeCryptoNetwork]) || {
      rate: 97.40,
      currency: 'USDT',
      network: 'TRC-20',
      address: 'TXjS51QoP8iBhykLw9mYfR79r42XN8yFkM',
      reqConfirmations: 3
    };

    const rubAmount = (this.data.amount / 100);
    const cryptoAmount = (rubAmount / netConfig.rate).toFixed(netConfig.currency === 'BTC' ? 6 : 4);

    document.getElementById('crypto-rate-text').textContent = `1 ${netConfig.currency} ≈ ${formatMoney(netConfig.rate * 100)}`;
    document.getElementById('crypto-amount-text').textContent = `${cryptoAmount} ${netConfig.currency}`;
    document.getElementById('crypto-address-text').textContent = netConfig.address;
    document.getElementById('crypto-confirmations-label').textContent = `1 из ${netConfig.reqConfirmations} подтверждений`;
    document.getElementById('crypto-progress-bar').style.width = `${(1 / netConfig.reqConfirmations) * 100}%`;

    const qrContainer = document.getElementById('crypto-qr-code');
    renderQrSvg(netConfig.address, qrContainer, 160);
  }

  handleCardSubmit() {
    const num = document.getElementById('card-number').value;
    const exp = document.getElementById('card-expiry').value;
    const cvv = document.getElementById('card-cvv').value;

    if (!num || num.replace(/\D/g, '').length < 16) {
      showToast('Введите корректный номер карты', 'error');
      return;
    }
    if (!exp || exp.length < 5) {
      showToast('Укажите срок действия карты (ММ/ГГ)', 'error');
      return;
    }
    if (!cvv || cvv.length < 3) {
      showToast('Укажите CVC/CVV код', 'error');
      return;
    }

    // Open 3-D Secure simulated prompt
    this.openModal('modal-3ds');
  }

  async simulate(outcome) {
    showToast(`Запуск симуляции: ${outcome}...`, 'info');
    await api.simulateOutcome(this.token, outcome);
    this.setStatus(outcome);
  }

  setStatus(status, reason = '') {
    this.currentStatus = status;

    const mainPanel = document.getElementById('checkout-main-panel');
    const outcomePanel = document.getElementById('checkout-outcome-panel');
    const succView = document.getElementById('outcome-succeeded');
    const waitView = document.getElementById('outcome-waiting');
    const cancView = document.getElementById('outcome-canceled');

    if (status === 'pending') {
      mainPanel.style.display = 'block';
      outcomePanel.style.display = 'none';
      return;
    }

    mainPanel.style.display = 'none';
    outcomePanel.style.display = 'block';
    succView.style.display = 'none';
    waitView.style.display = 'none';
    cancView.style.display = 'none';

    if (status === 'succeeded') {
      succView.style.display = 'flex';
      document.getElementById('succ-tx-id').textContent = `tx-${this.token.substring(0, 12)}`;
      document.getElementById('succ-method').textContent = this.getMethodName(this.activeMethod);
      document.getElementById('succ-amount').textContent = formatMoney(this.data.amount);
      document.getElementById('succ-date').textContent = formatDateTime(new Date().toISOString());
    } else if (status === 'waiting_for_capture') {
      waitView.style.display = 'flex';
      document.getElementById('wait-amount').textContent = formatMoney(this.data.amount);
      document.getElementById('wait-date').textContent = formatDateTime(new Date().toISOString());
    } else {
      cancView.style.display = 'flex';
      document.getElementById('canc-reason').textContent = reason || 'Платёж отклонён банком или пользователем';
    }
  }

  resetCheckout() {
    this.remainingSeconds = 30 * 60;
    this.setStatus('pending');
    this.startTimer();
    showToast('Форма оплаты сброшена в исходное состояние', 'info');
  }

  getMethodName(m) {
    if (m === 'sbp') return 'СБП (Система быстрых платежей)';
    if (m === 'bank_card') return 'Банковская карта (МИР/Visa/MC)';
    if (m === 'crypto') return `Криптовалюта (${this.activeCryptoNetwork.replace('_', ' ')})`;
    return m;
  }

  openReceiptModal() {
    const modal = document.getElementById('modal-receipt');
    if (!modal) return;

    document.getElementById('receipt-order-no').textContent = this.data.externalOrderId || '1042';
    document.getElementById('receipt-amount').textContent = formatMoney(this.data.amount);
    document.getElementById('receipt-date').textContent = formatDateTime(new Date().toISOString());
    document.getElementById('receipt-method').textContent = this.getMethodName(this.activeMethod);

    modal.classList.add('active');
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
  const app = new CheckoutApp();
  app.init();
  window.dobropayCheckout = app;
});
