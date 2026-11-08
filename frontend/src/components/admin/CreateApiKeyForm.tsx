import React, { useState } from 'react';
import { Button } from '../ui/Button';

export interface CreateApiKeyFormProps {
  onCreate: (name: string) => void;
  onCancel: () => void;
}

export const CreateApiKeyForm: React.FC<CreateApiKeyFormProps> = ({ onCreate, onCancel }) => {
  const [name, setName] = useState('');

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!name.trim()) return;
    onCreate(name.trim());
    setName('');
  };

  return (
    <form onSubmit={handleSubmit} className="p-4 bg-dp-tint rounded-card border border-dp-line space-y-3">
      <h4 className="text-xs font-bold text-dp-blue">Выпуск нового API-ключа</h4>
      <div className="space-y-1">
        <label className="text-[11px] font-semibold text-dp-blue">Название ключа</label>
        <input
          type="text"
          placeholder="Например: Production Backend Server"
          value={name}
          onChange={(e) => setName(e.target.value)}
          className="w-full px-3 py-2 text-xs rounded-card border border-dp-line bg-white focus:outline-none focus:border-dp-blue"
          autoFocus
        />
      </div>
      <div className="flex justify-end gap-2">
        <Button type="button" variant="outline" size="sm" onClick={onCancel}>
          Отмена
        </Button>
        <Button type="submit" variant="primary-blue" size="sm" disabled={!name.trim()}>
          Сгенерировать ключ
        </Button>
      </div>
    </form>
  );
};
