import React, { useMemo } from 'react';
import { generateQrSvgString } from '../../utils/qrcode';
import { Button } from '../ui/Button';

interface SbpMethodPanelProps {
  qrPayload?: string;
  onSimulateSuccess: () => void;
}

export const SbpMethodPanel: React.FC<SbpMethodPanelProps> = ({
  qrPayload = 'https://qr.nspk.ru/AD10005TB5978E49A924845579C247B6',
  onSimulateSuccess
}) => {
  const qrSvg = useMemo(() => generateQrSvgString(qrPayload, 210), [qrPayload]);

  return (
    <section className="method-panel active" aria-labelledby="tab-sbp">
      <div className="sbp-panel-content">
        <div className="sbp-logo-badge">
          <svg width="18" height="18" viewBox="0 0 32 32">
            <polygon points="16,2 30,28 2,28" fill="#F59ACC" stroke="#09318D" strokeWidth="2" />
            <polygon points="16,9 25,25 7,25" fill="#09318D" />
          </svg>
          <span>Система быстрых платежей</span>
        </div>

        <div
          className="sbp-qr-wrapper"
          dangerouslySetInnerHTML={{ __html: qrSvg }}
        />

        <p className="sbp-instruction">
          Отсканируйте QR-код в приложении любого банка или камерой смартфона для мгновенной оплаты без комиссии
        </p>

        <Button
          variant="primary"
          size="lg"
          full
          onClick={onSimulateSuccess}
        >
          Открыть в приложении банка
        </Button>

        <div className="sbp-pulse-indicator">
          <span className="pulse-dot" />
          <span>Ожидаем подтверждения оплаты банком...</span>
        </div>
      </div>
    </section>
  );
};
