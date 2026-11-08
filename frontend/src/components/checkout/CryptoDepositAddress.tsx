import React, { useState } from 'react';
import { generateQrSvgString } from '../../utils/qrcode';
import { Button } from '../ui/Button';

export interface CryptoDepositAddressProps {
  address: string;
}

export const CryptoDepositAddress: React.FC<CryptoDepositAddressProps> = ({ address }) => {
  const [copied, setCopied] = useState(false);
  const qrSvg = generateQrSvgString(address, 160);

  const handleCopy = () => {
    navigator.clipboard.writeText(address);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

  return (
    <div className="flex flex-col items-center space-y-3">
      <div
        className="w-[180px] h-[180px] p-2 bg-white rounded-card border-2 border-dp-line shadow-sm flex items-center justify-center"
        dangerouslySetInnerHTML={{ __html: qrSvg }}
      />

      <div className="w-full space-y-1">
        <label className="text-xs font-semibold text-dp-blue">Адрес для пополнения</label>
        <div className="flex items-center gap-2">
          <input
            type="text"
            readOnly
            value={address}
            className="flex-1 px-3 py-2 rounded-card border border-dp-line bg-dp-tint font-mono text-xs text-dp-blue select-all"
          />
          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={handleCopy}
          >
            {copied ? 'Скопировано!' : 'Копировать'}
          </Button>
        </div>
      </div>
    </div>
  );
};
