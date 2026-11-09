import React, { useState } from 'react';
import { ApiKey } from '../../types/ApiKey';
import { ApiKeyMode } from '../../types/ApiKeyMode';
import { ApiKeyRow } from './ApiKeyRow';
import { CreateApiKeyForm } from './CreateApiKeyForm';
import { Button } from '../ui/Button';

export interface ApiKeysSectionProps {
  apiKeys: ApiKey[];
  canCreateLiveKey: boolean;
  onCreateKey: (name: string, mode: ApiKeyMode) => Promise<void>;
  onRevokeKey: (keyId: string) => void;
}

export const ApiKeysSection: React.FC<ApiKeysSectionProps> = ({
  apiKeys,
  canCreateLiveKey,
  onCreateKey,
  onRevokeKey
}) => {
  const [isCreating, setIsCreating] = useState(false);

  return (
    <div className="bg-white border border-dp-line rounded-card p-5 shadow-sm space-y-4">
      <div className="flex items-center justify-between pb-3 border-b border-dp-line">
        <div>
          <h3 className="text-sm font-bold text-dp-blue">API-ключи мерчанта</h3>
          <p className="text-xs text-dp-blue/60">
            Используются для серверной авторизации через заголовок <code>X-Api-Key</code>
          </p>
        </div>
        {!isCreating && (
          <Button variant="primary-blue" size="sm" onClick={() => setIsCreating(true)}>
            + Новый ключ
          </Button>
        )}
      </div>

      {isCreating && (
        <CreateApiKeyForm
          canCreateLiveKey={canCreateLiveKey}
          onCreate={async (name, mode) => {
            await onCreateKey(name, mode);
            setIsCreating(false);
          }}
          onCancel={() => setIsCreating(false)}
        />
      )}

      <div className="space-y-2.5">
        {apiKeys.map((key) => (
          <ApiKeyRow key={key.id} apiKey={key} onRevoke={onRevokeKey} />
        ))}
        {apiKeys.length === 0 && !isCreating && (
          <div className="text-center py-8 text-xs text-dp-blue/50">
            У вас пока нет активных API-ключей. Выпустите первый ключ для интеграции.
          </div>
        )}
      </div>
    </div>
  );
};
