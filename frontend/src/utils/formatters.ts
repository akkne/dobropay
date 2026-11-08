export function formatMoney(kopecks: number | null | undefined, showFraction = true): string {
  if (kopecks === null || kopecks === undefined || isNaN(kopecks)) return '0 ₽';
  const rubles = kopecks / 100;
  return new Intl.NumberFormat('ru-RU', {
    style: 'currency',
    currency: 'RUB',
    minimumFractionDigits: showFraction ? (kopecks % 100 === 0 ? 0 : 2) : 0,
    maximumFractionDigits: 2
  }).format(rubles).replace('RUB', '₽');
}

export function formatDateTime(isoString: string | null | undefined): string {
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

export function formatCardNumber(value: string): string {
  const clean = value.replace(/\D/g, '').substring(0, 19);
  return clean.replace(/(\d{4})(?=\d)/g, '$1 ');
}

export function formatExpiryDate(value: string): string {
  const clean = value.replace(/\D/g, '').substring(0, 4);
  if (clean.length >= 3) {
    return `${clean.substring(0, 2)}/${clean.substring(2)}`;
  }
  return clean;
}

export function formatTimer(seconds: number): string {
  const m = Math.floor(seconds / 60);
  const s = seconds % 60;
  return `${m.toString().padStart(2, '0')}:${s.toString().padStart(2, '0')}`;
}

export function calculateCryptoAmount(amountKopecks: number, network: string): string {
  const rubles = amountKopecks / 100;
  switch (network) {
    case 'usdt':
      return (rubles / 94.5).toFixed(2);
    case 'ton':
      return (rubles / 510.0).toFixed(4);
    case 'btc':
      return (rubles / 6250000.0).toFixed(6);
    default:
      return (rubles / 94.5).toFixed(2);
  }
}
