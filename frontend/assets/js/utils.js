/**
 * Dobropay UI Utilities
 */

export function formatMoney(kopecks, showFraction = true) {
  if (kopecks === null || kopecks === undefined || isNaN(kopecks)) return '0 ₽';
  const rubles = kopecks / 100;
  return new Intl.NumberFormat('ru-RU', {
    style: 'currency',
    currency: 'RUB',
    minimumFractionDigits: showFraction ? (kopecks % 100 === 0 ? 0 : 2) : 0,
    maximumFractionDigits: 2
  }).format(rubles).replace('RUB', '₽');
}

export function formatDateTime(isoString) {
  if (!isoString) return '—';
  const date = new Date(isoString);
  if (isNaN(date.getTime())) return isoString;
  return new Intl.DateTimeFormat('ru-RU', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit'
  }).format(date);
}

export function copyToClipboard(text, message = 'Скопировано в буфер обмена') {
  if (navigator.clipboard && window.isSecureContext) {
    navigator.clipboard.writeText(text).then(() => showToast(message, 'success'));
  } else {
    const input = document.createElement('textarea');
    input.value = text;
    input.style.position = 'fixed';
    input.style.opacity = '0';
    document.body.appendChild(input);
    input.focus();
    input.select();
    try {
      document.execCommand('copy');
      showToast(message, 'success');
    } catch {
      showToast('Не удалось скопировать', 'error');
    }
    document.body.removeChild(input);
  }
}

export function showToast(message, type = 'info') {
  let container = document.getElementById('toast-container');
  if (!container) {
    container = document.createElement('div');
    container.id = 'toast-container';
    container.className = 'toast-container';
    document.body.appendChild(container);
  }

  const toast = document.createElement('div');
  toast.className = `toast ${type}`;
  toast.textContent = message;
  container.appendChild(toast);

  setTimeout(() => {
    toast.style.opacity = '0';
    toast.style.transform = 'translateY(8px)';
    toast.style.transition = 'all 0.25s ease';
    setTimeout(() => toast.remove(), 250);
  }, 3200);
}

export function detectCardBrand(cardNumber) {
  const clean = cardNumber.replace(/\D/g, '');
  if (/^2/.test(clean)) {
    return { brand: 'mir', name: 'МИР' };
  }
  if (/^4/.test(clean)) {
    return { brand: 'visa', name: 'Visa' };
  }
  if (/^(5[1-5]|2[2-7])/.test(clean)) {
    return { brand: 'mastercard', name: 'Mastercard' };
  }
  return { brand: 'generic', name: 'Банковская карта' };
}

export function validateCardNumberLuhn(cardNumber) {
  const clean = cardNumber.replace(/\D/g, '');
  if (clean.length < 13 || clean.length > 19) return false;
  let sum = 0;
  let shouldDouble = false;
  for (let i = clean.length - 1; i >= 0; i--) {
    let digit = parseInt(clean.charAt(i), 10);
    if (shouldDouble) {
      digit *= 2;
      if (digit > 9) digit -= 9;
    }
    sum += digit;
    shouldDouble = !shouldDouble;
  }
  return sum % 10 === 0;
}

export function formatCardNumber(value) {
  const clean = value.replace(/\D/g, '').substring(0, 19);
  return clean.replace(/(\d{4})(?=\d)/g, '$1 ');
}

export function formatExpiryDate(value) {
  const clean = value.replace(/\D/g, '').substring(0, 4);
  if (clean.length >= 3) {
    return `${clean.substring(0, 2)}/${clean.substring(2)}`;
  }
  return clean;
}

// INN validation for Russian entities (10 digits) and entrepreneurs (12 digits)
export function validateInn(inn) {
  const clean = (inn || '').replace(/\D/g, '');
  if (clean.length !== 10 && clean.length !== 12) return false;
  
  const checkDigit = (digits, coefficients) => {
    let sum = 0;
    for (let i = 0; i < coefficients.length; i++) {
      sum += digits[i] * coefficients[i];
    }
    return (sum % 11) % 10;
  };

  const d = clean.split('').map(Number);
  if (clean.length === 10) {
    const c10 = checkDigit(d, [2, 4, 10, 3, 5, 9, 4, 6, 8]);
    return c10 === d[9];
  } else {
    const c11 = checkDigit(d, [7, 2, 4, 10, 3, 5, 9, 4, 6, 8]);
    const c12 = checkDigit(d, [3, 7, 2, 4, 10, 3, 5, 9, 4, 6, 8]);
    return c11 === d[10] && c12 === d[11];
  }
}

// BIK validation (9 digits, starts with 04)
export function validateBik(bik) {
  const clean = (bik || '').replace(/\D/g, '');
  return clean.length === 9 && clean.startsWith('04');
}

// Checking account validation (20 digits)
export function validateAccount(acc) {
  const clean = (acc || '').replace(/\D/g, '');
  return clean.length === 20;
}
