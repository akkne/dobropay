import React from 'react';

interface SandboxSimulatorBarProps {
  onSimulate: (outcome: 'succeeded' | 'waiting_for_capture' | 'canceled') => void;
  onReset: () => void;
}

export const SandboxSimulatorBar: React.FC<SandboxSimulatorBarProps> = ({ onSimulate, onReset }) => {
  return (
    <aside className="sandbox-bar" aria-label="Панель песочницы разработчика">
      <div className="sandbox-bar-title">
        <span className="sandbox-chip">Sandbox</span>
        <span>Симулятор провайдера и платежей</span>
      </div>
      <div className="sandbox-actions">
        <button
          type="button"
          className="sandbox-btn success"
          title="Симулировать успешную оплату"
          onClick={() => onSimulate('succeeded')}
        >
          ✓ Успех (Succeeded)
        </button>
        <button
          type="button"
          className="sandbox-btn warning"
          title="Симулировать холд суммы"
          onClick={() => onSimulate('waiting_for_capture')}
        >
          ⏱ Холд (WaitingForCapture)
        </button>
        <button
          type="button"
          className="sandbox-btn danger"
          title="Симулировать отказ"
          onClick={() => onSimulate('canceled')}
        >
          ✕ Отказ (Canceled)
        </button>
        <button
          type="button"
          className="sandbox-btn"
          title="Сбросить в исходное состояние"
          onClick={onReset}
        >
          ↺ Сбросить
        </button>
      </div>
    </aside>
  );
};
