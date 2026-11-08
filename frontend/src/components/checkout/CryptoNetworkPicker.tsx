import React from 'react';

export type CryptoNetwork = 'usdt' | 'ton' | 'btc';

export interface CryptoNetworkPickerProps {
  network: CryptoNetwork;
  onChange: (network: CryptoNetwork) => void;
}

export const CryptoNetworkPicker: React.FC<CryptoNetworkPickerProps> = ({ network, onChange }) => {
  const networks: Array<{ id: CryptoNetwork; label: string; badge: string }> = [
    { id: 'usdt', label: 'USDT', badge: 'TRC-20' },
    { id: 'ton', label: 'TON', badge: 'The Open Network' },
    { id: 'btc', label: 'BTC', badge: 'Bitcoin' },
  ];

  return (
    <div className="grid grid-cols-3 gap-2">
      {networks.map((item) => (
        <button
          key={item.id}
          type="button"
          onClick={() => onChange(item.id)}
          className={`p-2.5 rounded-card border text-left cursor-pointer transition-all ${
            network === item.id
              ? 'bg-dp-tint border-dp-blue ring-1 ring-dp-blue'
              : 'bg-white border-dp-line hover:border-dp-blue/40'
          }`}
        >
          <div className="font-bold text-xs text-dp-blue">{item.label}</div>
          <div className="text-[10px] text-dp-blue/60 truncate">{item.badge}</div>
        </button>
      ))}
    </div>
  );
};
