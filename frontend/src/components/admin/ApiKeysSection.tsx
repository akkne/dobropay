import React, { useState } from 'react';
import { ApiKey } from '../../types/ApiKey';
import { formatDateTime } from '../../utils/formatters';
import { Badge } from '../ui/Badge';
import { Button } from '../ui/Button';
import { Modal } from '../ui/Modal';

interface ApiKeysSectionProps {
  apiKeys: ApiKey[];
  onCreateKey: (name: string) => Promise<ApiKey>;
  onRevokeKey: (id: string) => void;
}

export const ApiKeysSection: React.FC<ApiKeysSectionProps> = ({
  apiKeys,
  onCreateKey,
  onRevokeKey
}) => {
  const [isCreateOpen, setIsCreateOpen] = useState(false);
  const [keyName, setKeyName] = useState('');
  const [revealedKey, setRevealedKey] = useState<string | null>(null);

  const handleCreateSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const created = await onCreateKey(keyName.trim() || 'API Ключ сервера');
    setIsCreateOpen(false);
    setKeyName('');
    if (created.apiKey) {
      setRevealedKey(created.apiKey);
    }
  };

  return (
    <div className="section-card">
      <div className="section-head">
        <div>
          <h2 className="section-title">API-ключи магазина</h2>
          <p style={{ fontSize: '13px', color: 'rgba(9, 49, 141, 0.65)', marginTop: '4px' }}>
            Используются для серверных запросов к API Добропей через заголовок <code>X-Api-Key</code>
          </p>
        </div>
        <Button variant="primary" onClick={() => setIsCreateOpen(true)}>
          + Выпустить новый API-ключ
        </Button>
      </div>

      <div className="table-responsive">
        <table className="data-table">
          <thead>
            <tr>
              <th>Название</th>
              <th>Ключ (префикс)</th>
              <th>Дата выпуска</th>
              <th>Статус</th>
              <th>Действия</th>
            </tr>
          </thead>
          <tbody>
            {apiKeys.map((k) => (
              <tr key={k.id}>
                <td style={{ fontWeight: 600, color: 'var(--blue)' }}>{k.name}</td>
                <td>
                  <span className="mono" style={{ fontSize: '13px', fontWeight: 600 }}>
                    {k.prefix}
                  </span>
                </td>
                <td style={{ fontSize: '13px', color: 'rgba(9, 49, 141, 0.65)' }}>
                  {formatDateTime(k.createdAt)}
                </td>
                <td>
                  <Badge variant={k.status === 'active' ? 'succeeded' : 'canceled'}>
                    {k.status === 'active' ? 'Активен' : 'Отозван'}
                  </Badge>
                </td>
                <td className="td-actions">
                  {k.status === 'active' && (
                    <Button variant="outline" size="sm" onClick={() => onRevokeKey(k.id)}>
                      Отозвать
                    </Button>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {/* Modal: Create Key */}
      <Modal isOpen={isCreateOpen} onClose={() => setIsCreateOpen(false)}>
        <h3 style={{ fontSize: '20px', fontWeight: 700, color: 'var(--blue)', marginBottom: '6px' }}>
          Выпуск нового API-ключа
        </h3>
        <p style={{ fontSize: '13px', color: 'rgba(9, 49, 141, 0.65)', marginBottom: '18px' }}>
          Ключ позволяет программно управлять платежами от имени данного магазина
        </p>

        <form onSubmit={handleCreateSubmit}>
          <div className="form-group">
            <label className="form-label" htmlFor="create-key-name">
              Название ключа
            </label>
            <input
              type="text"
              id="create-key-name"
              className="form-input"
              placeholder="Бэкенд мобильного приложения"
              value={keyName}
              onChange={(e) => setKeyName(e.target.value)}
              required
            />
          </div>

          <Button type="submit" variant="primary" size="lg" full>
            Сгенерировать ключ
          </Button>
        </form>
      </Modal>

      {/* Modal: Key Revealed */}
      <Modal isOpen={!!revealedKey} onClose={() => setRevealedKey(null)}>
        <h3 style={{ fontSize: '20px', fontWeight: 700, color: 'var(--blue)', marginBottom: '6px' }}>
          API-ключ успешно создан
        </h3>
        <p style={{ fontSize: '13px', color: 'var(--red)', fontWeight: 600, marginBottom: '14px' }}>
          ⚠️ Скопируйте ключ сейчас. В целях безопасности мы не храним его в открытом виде и больше не сможем вам его показать!
        </p>

        <div className="form-group">
          <div
            className="form-input mono"
            style={{ wordBreak: 'break-all', background: 'var(--tint)', userSelect: 'all', fontSize: '13px' }}
          >
            {revealedKey}
          </div>
        </div>

        <Button
          variant="primary"
          size="lg"
          full
          onClick={() => {
            if (revealedKey) navigator.clipboard?.writeText(revealedKey);
            setRevealedKey(null);
          }}
        >
          Скопировать ключ в буфер
        </Button>
      </Modal>
    </div>
  );
};
