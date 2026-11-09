import React, { useState } from 'react';
import { ApiKey } from '../../types/ApiKey';
import { formatDateTime } from '../../utils/formatters';
import { Button } from '../ui/Button';

export interface ApiKeyRowProps {
  apiKey: ApiKey;
  onRevoke: (id: string) => void;
}

export const ApiKeyRow: React.FC<ApiKeyRowProps> = ({ apiKey, onRevoke }) => {
  const [copied, setCopied] = useState(false);
  const [showFull, setShowFull] = useState(false);

  const handleCopy = () => {
    if (!apiKey.apiKey) return;
    navigator.clipboard.writeText(apiKey.apiKey);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

  return (
    <div className="flex flex-col sm:flex-row sm:items-center justify-between p-3.5 bg-dp-tint rounded-card border border-dp-line gap-3">
      <div className="space-y-1">
        <div className="flex items-center gap-2">
          <span className="font-bold text-xs text-dp-blue">{apiKey.name}</span>
          <span className="px-2 py-0.5 rounded text-[10px] font-bold bg-dp-green-blush text-dp-green border border-dp-green/20">
            {apiKey.status}
          </span>
        </div>
        <div className="flex items-center gap-2">
          <code className="text-xs font-mono text-dp-blue font-medium bg-white px-2 py-1 rounded border border-dp-line">
            {showFull && apiKey.apiKey ? apiKey.apiKey : apiKey.prefix}
          </code>
          {apiKey.apiKey && (
            <button
              type="button"
              onClick={() => setShowFull(!showFull)}
              className="text-[11px] text-dp-blue/60 hover:text-dp-blue underline cursor-pointer"
            >
              {showFull ? 'Скрыть' : 'Показать'}
            </button>
          )}
        </div>
        <div className="text-[10px] text-dp-blue/50">
          Создан: {formatDateTime(apiKey.createdAt)}
        </div>
      </div>

      <div className="flex items-center gap-2 self-end sm:self-center">
        {apiKey.apiKey && <Button variant="outline" size="sm" onClick={handleCopy}>
          {copied ? 'Скопировано!' : 'Копировать'}
        </Button>}
        <button
          type="button"
          onClick={() => onRevoke(apiKey.id)}
          className="px-2.5 py-1 text-xs font-semibold text-dp-red hover:bg-dp-red-blush rounded-pill transition-colors"
        >
          Отозвать
        </button>
      </div>
    </div>
  );
};
