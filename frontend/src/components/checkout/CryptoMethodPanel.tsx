import React, { useState } from 'react';
import { CryptoNetworkPicker, CryptoNetwork } from './CryptoNetworkPicker';
import { CryptoDepositAddress } from './CryptoDepositAddress';
import { CryptoRateTracker } from './CryptoRateTracker';
import { calculateCryptoAmount } from '../../utils/formatters';

export interface CryptoMethodPanelProps {
  amount: number;
}

export const CryptoMethodPanel: React.FC<CryptoMethodPanelProps> = ({ amount }) => {
  const [network, setNetwork] = useState<CryptoNetwork>('usdt');

  const networkDetails: Record<
    CryptoNetwork,
    { address: string; symbol: string; rate: string; reqConf: number; conf: number }
  > = {
    usdt: {
      address: 'TR7NHqjeKQxGTCi8q8ZY4pL8otSzgjLj6t',
      symbol: 'USDT',
      rate: '1 USDT ≈ 94.50 ₽',
      reqConf: 3,
      conf: 1
    },
    ton: {
      address: 'EQCD39VS5jcptHL8vMjEXrzGaRcCVYto7HUn4bpAOg8xqB2N',
      symbol: 'TON',
      rate: '1 TON ≈ 510.00 ₽',
      reqConf: 5,
      conf: 2
    },
    btc: {
      address: 'bc1qar0srrr7xfkvy5l643lydnw9re59gtzzwf5mdq',
      symbol: 'BTC',
      rate: '1 BTC ≈ 6 250 000 ₽',
      reqConf: 2,
      conf: 0
    }
  };

  const details = networkDetails[network];
  const cryptoAmount = calculateCryptoAmount(amount, network);

  return (
    <div className="space-y-4 py-2">
      <CryptoNetworkPicker network={network} onChange={setNetwork} />
      <CryptoDepositAddress address={details.address} />
      <CryptoRateTracker
        cryptoAmount={cryptoAmount}
        cryptoSymbol={details.symbol}
        rateDescription={details.rate}
        confirmations={details.conf}
        requiredConfirmations={details.reqConf}
      />
    </div>
  );
};
