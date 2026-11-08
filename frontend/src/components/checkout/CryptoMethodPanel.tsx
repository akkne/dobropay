import React, { useState, useMemo } from 'react';
import { generateQrSvgString } from '../../utils/qrcode';
import { formatMoney } from '../../utils/formatters';
import { Button } from '../ui/Button';

interface CryptoMethodPanelProps {
  rubAmount: number;
  onCopyText: (text: string, msg: string) => void;
}

type NetworkKey = 'USDT_TRC20' | 'TON' | 'BTC';

interface NetworkSpec {
  currency: string;
  name: string;
  rate: number;
  address: string;
  reqConfirmations: number;
}

const NETWORKS: Record<NetworkKey, NetworkSpec> = {
  USDT_TRC20: {
    currency: 'USDT',
    name: 'USDT (TRC-20)',
    rate: 97.40,
    address: 'TXjS51QoP8iBhykLw9mYfR79r42XN8yFkM',
    reqConfirmations: 3
  },
  TON: {
    currency: 'TON',
    name: 'TON',
    rate: 485.20,
    address: 'EQBvW8m53GoU-VJJ5au2PfqqsoTVFaP_DL',
    reqConfirmations: 1
  },
  BTC: {
    currency: 'BTC',
    name: 'Bitcoin',
    rate: 6450000.00,
    address: 'bc1qxy2kgdygjrsqtzq2n0yrf2493p83kkfjhx0wlh',
    reqConfirmations: 2
  }
};

export const CryptoMethodPanel: React.FC<CryptoMethodPanelProps> = ({ rubAmount, onCopyText }) => {
  const [selectedNetwork, setSelectedNetwork] = useState<NetworkKey>('USDT_TRC20');

  const currentNet = NETWORKS[selectedNetwork];
  const rubles = rubAmount / 100;
  const cryptoAmount = (rubles / currentNet.rate).toFixed(currentNet.currency === 'BTC' ? 6 : 4);

  const qrSvg = useMemo(() => generateQrSvgString(currentNet.address, 160), [currentNet.address]);

  return (
    <section className="method-panel active" aria-labelledby="tab-crypto">
      <div className="crypto-network-pills">
        {(Object.keys(NETWORKS) as NetworkKey[]).map((key) => (
          <button
            key={key}
            type="button"
            className={`network-pill ${selectedNetwork === key ? 'active' : ''}`}
            onClick={() => setSelectedNetwork(key)}
          >
            {NETWORKS[key].name}
          </button>
        ))}
      </div>

      <div className="crypto-rate-box">
        <div>
          <div style={{ color: 'rgba(9, 49, 141, 0.65)', fontSize: '11px', textTransform: 'uppercase' }}>
            Сумма перевода
          </div>
          <div className="crypto-amount-large">
            {cryptoAmount} {currentNet.currency}
          </div>
        </div>
        <div style={{ textAlign: 'right' }}>
          <div style={{ color: 'rgba(9, 49, 141, 0.65)', fontSize: '11px' }}>Курс конвертации</div>
          <div style={{ fontWeight: 600, color: 'var(--blue)' }}>
            1 {currentNet.currency} ≈ {formatMoney(currentNet.rate * 100)}
          </div>
        </div>
      </div>

      <div className="crypto-address-box">
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'baseline' }}>
          <span className="form-label">Адрес кошелька для депозита</span>
          <Button
            type="button"
            variant="ghost"
            size="sm"
            onClick={() => onCopyText(currentNet.address, 'Адрес криптокошелька скопирован!')}
          >
            Скопировать
          </Button>
        </div>
        <div className="crypto-address-field">
          <span className="crypto-address-text">{currentNet.address}</span>
        </div>
      </div>

      <div style={{ display: 'flex', justifyContent: 'center', margin: '12px 0' }}>
        <div className="sbp-qr-wrapper" style={{ padding: '12px' }} dangerouslySetInnerHTML={{ __html: qrSvg }} />
      </div>

      <div className="confirmations-tracker">
        <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: '12px', fontWeight: 550 }}>
          <span>Статус сети</span>
          <span style={{ color: 'var(--blue)' }}>1 из {currentNet.reqConfirmations} подтверждений</span>
        </div>
        <div className="progress-bar">
          <div
            className="progress-bar-fill"
            style={{ width: `${(1 / currentNet.reqConfirmations) * 100}%` }}
          />
        </div>
      </div>
    </section>
  );
};
