import React from 'react';

export interface CryptoRateTrackerProps {
  cryptoAmount: string;
  cryptoSymbol: string;
  rateDescription: string;
  confirmations: number;
  requiredConfirmations: number;
}

export const CryptoRateTracker: React.FC<CryptoRateTrackerProps> = ({
  cryptoAmount,
  cryptoSymbol,
  rateDescription,
  confirmations,
  requiredConfirmations
}) => {
  const percent = Math.min(100, Math.round((confirmations / requiredConfirmations) * 100));

  return (
    <div className="bg-dp-tint border border-dp-line rounded-card p-3.5 space-y-2.5">
      <div className="flex items-center justify-between text-xs">
        <span className="text-dp-blue/70">Сумма к отправке</span>
        <span className="font-mono font-bold text-sm text-dp-blue">
          {cryptoAmount} {cryptoSymbol}
        </span>
      </div>

      <div className="flex items-center justify-between text-[11px] text-dp-blue/60 border-t border-dashed border-dp-line/80 pt-2">
        <span>Курс конвертации</span>
        <span className="font-mono">{rateDescription}</span>
      </div>

      <div className="space-y-1 pt-1">
        <div className="flex items-center justify-between text-[11px] text-dp-blue/70">
          <span>Подтверждения сети</span>
          <span className="font-mono font-semibold">
            {confirmations} из {requiredConfirmations}
          </span>
        </div>
        <div className="w-full h-1.5 bg-white rounded-full overflow-hidden border border-dp-line">
          <div
            className="h-full bg-dp-blue transition-all duration-500 rounded-full"
            style={{ width: `${percent}%` }}
          />
        </div>
      </div>
    </div>
  );
};
