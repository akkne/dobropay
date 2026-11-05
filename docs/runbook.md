# Локальный runbook

Все действия ниже доступны в локальной песочнице. Для поиска платежа используйте `GET /v1/payments/{id}` с ключом его магазина. Для технической диагностики через PostgreSQL:

```bash
docker compose exec db psql -U postgres -d dobropay -c "SELECT id,shop_id,status,amount,created_at FROM payments WHERE id='<payment-id>'"
docker compose exec db psql -U postgres -d dobropay -c "SELECT id,external_id,state,created_at FROM payment_attempts WHERE payment_id='<payment-id>'"
docker compose exec db psql -U postgres -d dobropay -c "SELECT id,provider_event_id,state,attempts,error FROM provider_inbox ORDER BY received_at DESC LIMIT 10"
docker compose exec db psql -U postgres -d dobropay -c "SELECT d.id,d.state,d.attempts,d.last_result,e.id AS event_id FROM webhook_deliveries d JOIN outbox_events e ON e.id=d.event_id WHERE e.payment_id='<payment-id>'"
```

`pending` без попытки означает, что покупатель ещё не нажал кнопку. Попытка `unknown` без внешнего ID означает, что результат вызова адаптера не известен; M1 не запускает автоматическое уточнение. Сохранённый `provider_inbox` с `pending` и `error=attempt_not_ready` повторится до шести раз. Состояние `rejected` означает несовпадение идентификатора, суммы или валюты. Событие в `processed` и платёж в терминальном статусе означают, что транзакция подтверждения завершена.

При ошибке webhook проверьте `webhook_deliveries.last_result`, HTTP-код в `webhook_delivery_attempts` и `docker compose logs worker api`. Демо-получатель намеренно отвечает `503` при первом вызове на событие. Worker повторяет до пяти раз; `event_id` сохраняется. Если Worker остановлен, событие остаётся в outbox/очереди; `docker compose start worker` продолжает доставку. Зависшее задание `processing` освобождается после 30 секунд аренды. Ручной повтор окончательно `failed` в M1 не предусмотрен.
