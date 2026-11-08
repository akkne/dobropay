export function detectCardBrand(cardNumber: string): { brand: 'mir' | 'visa' | 'mastercard' | 'generic'; name: string } {
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

export function validateCardNumberLuhn(cardNumber: string): boolean {
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

export function validateInn(inn: string): boolean {
  const clean = (inn || '').replace(/\D/g, '');
  if (clean.length !== 10 && clean.length !== 12) return false;

  const checkDigit = (digits: number[], coefficients: number[]) => {
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

export function validateBik(bik: string): boolean {
  const clean = (bik || '').replace(/\D/g, '');
  return clean.length === 9 && clean.startsWith('04');
}

export function validateAccount(acc: string): boolean {
  const clean = (acc || '').replace(/\D/g, '');
  return clean.length === 20;
}
