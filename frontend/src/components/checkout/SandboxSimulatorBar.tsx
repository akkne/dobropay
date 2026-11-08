import React from 'react';
import { PaymentStatus } from '../../types/PaymentStatus';
import { SandboxActionButton } from './SandboxActionButton';

export interface SandboxSimulatorBarProps {
  onSimulate: (status: PaymentStatus) => void;
  onReset: () => void;
}

export const SandboxSimulatorBar: React.FC<SandboxSimulatorBarProps> = ({ onSimulate, onReset }) => {
  return (
    <div className="fixed bottom-4 left-1/2 -translate-x-1/2 z-40 bg-white/95 backdrop-blur-md border border-dp-line rounded-pill px-4 py-2 shadow-window flex items-center gap-2 max-w-[95vw] overflow-x-auto">
      <div className="flex items-center gap-1.5 text-xs font-bold text-dp-blue whitespace-nowrap pr-1 border-r border-dp-line">
        <span className="w-2 h-2 rounded-full bg-dp-pink" />
        <span>Песочница:</span>
      </div>

      <div className="flex items-center gap-1.5 whitespace-nowrap">
        <SandboxActionButton
          label="Succeeded"
          variant="success"
          onClick={() => onSimulate('succeeded')}
        />
        <SandboxActionButton
          label="Hold"
          variant="warning"
          onClick={() => onSimulate('waiting_for_capture')}
        />
        <SandboxActionButton
          label="Canceled"
          variant="danger"
          onClick={() => onSimulate('canceled')}
        />
        <SandboxActionButton
          label="Сброс"
          variant="neutral"
          onClick={onReset}
        />
      </div>
    </div>
  );
};
