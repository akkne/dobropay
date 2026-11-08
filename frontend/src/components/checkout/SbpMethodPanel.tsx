import React from 'react';
import { SbpQrDisplay } from './SbpQrDisplay';
import { SbpBankDeeplinkButton } from './SbpBankDeeplinkButton';
import { SbpStatusTicker } from './SbpStatusTicker';

export interface SbpMethodPanelProps {
  payload?: string;
  deeplink?: string;
}

export const SbpMethodPanel: React.FC<SbpMethodPanelProps> = ({
  payload = 'https://qr.nspk.ru/AD100004738291048291847291048291?type=02&bank=100000000004&crc=AB12',
  deeplink = 'https://qr.nspk.ru/AD100004738291048291847291048291'
}) => {
  return (
    <div className="flex flex-col items-center space-y-5 py-2">
      <SbpQrDisplay payload={payload} />
      <SbpStatusTicker />
      <div className="w-full pt-1">
        <SbpBankDeeplinkButton deeplink={deeplink} />
      </div>
    </div>
  );
};
