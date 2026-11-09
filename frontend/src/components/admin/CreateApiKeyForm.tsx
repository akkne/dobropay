import React, { useState } from 'react';
import { Button } from '../ui/Button';
import { ApiKeyMode } from '../../types/ApiKeyMode';

export interface CreateApiKeyFormProps {
  canCreateLiveKey: boolean;
  onCreate: (name: string, mode: ApiKeyMode) => Promise<void>;
  onCancel: () => void;
}

export const CreateApiKeyForm: React.FC<CreateApiKeyFormProps> = ({ canCreateLiveKey, onCreate, onCancel }) => {
  const [name, setName] = useState('');
  const [mode, setMode] = useState<ApiKeyMode>('test');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!name.trim()) return;
    setBusy(true);
    setError(null);
    try {
      await onCreate(name.trim(), mode);
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : 'Не удалось создать ключ');
    } finally {
      setBusy(false);
    }
  };

  return (
    <form onSubmit={handleSubmit} className="p-4 bg-dp-tint rounded-card border border-dp-line space-y-3">
      <h4 className="text-xs font-bold text-dp-blue">Выпуск нового API-ключа</h4>
      <div className="space-y-1">
        <label className="text-[11px] font-semibold text-dp-blue">Название ключа</label>
        <input
          type="text"
          placeholder="Например: интеграция сайта"
          value={name}
          onChange={(e) => setName(e.target.value)}
          className="w-full px-3 py-2 text-xs rounded-card border border-dp-line bg-white focus:outline-none focus:border-dp-blue"
          autoFocus
        />
      </div>
      <fieldset className="space-y-2 text-xs text-dp-blue">
        <legend className="font-semibold">Режим ключа</legend>
        <label className="flex items-center gap-2"><input type="radio" name="key-mode" checked={mode === 'test'} onChange={() => setMode('test')} /> Тестовый</label>
        <label className="flex items-center gap-2"><input type="radio" name="key-mode" checked={mode === 'live'} disabled={!canCreateLiveKey} onChange={() => setMode('live')} /> Боевой</label>
        {!canCreateLiveKey && <p className="text-dp-blue/60">Боевой ключ станет доступен после одобрения анкеты.</p>}
      </fieldset>
      {error && <p role="alert" className="text-xs text-dp-red">{error}</p>}
      <div className="flex justify-end gap-2 pt-1">
        <Button type="button" variant="outline" size="sm" onClick={onCancel}>
          Отмена
        </Button>
        <Button type="submit" variant="primary-blue" size="sm" disabled={!name.trim() || busy}>
          {busy ? 'Создаём…' : 'Сгенерировать ключ'}
        </Button>
      </div>
    </form>
  );
};
