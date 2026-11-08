import React from 'react';
import { generateQrSvgString } from '../../utils/qrcode';

export interface SbpQrDisplayProps {
  payload: string;
}

export const SbpQrDisplay: React.FC<SbpQrDisplayProps> = ({ payload }) => {
  const qrSvg = generateQrSvgString(payload, 210);

  return (
    <div className="flex flex-col items-center">
      <div
        className="w-[230px] h-[230px] p-3 bg-white rounded-card border-2 border-dp-line shadow-sm flex items-center justify-center transition-transform hover:scale-[1.01]"
        dangerouslySetInnerHTML={{ __html: qrSvg }}
      />
      <div className="mt-3 text-xs text-dp-blue/70 text-center font-medium max-w-[260px]">
        Отсканируйте QR-код в приложении любого банка или камерой смартфона
      </div>
    </div>
  );
};
