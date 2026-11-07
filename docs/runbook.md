# Эксплуатационный Runbook

Все процедуры ниже реально доступны и проверяемы в локальном контуре Dobropay.

## 1. Диагностика зависшего платежа (`payments-db`)

Проверьте состояние платежа, попытки, входящего callback и очереди outbox в контейнере `payments-db`:
```bash
docker compose exec payments-db psql -U postgres -d payments_db -c "SELECT id, shop_id, status, amount, created_at FROM payments WHERE id='<payment-id>'"
docker compose exec payments-db psql -U postgres -d payments_db -c "SELECT id, external_id, state, created_at FROM payment_attempts WHERE payment_id='<payment-id>'"
docker compose exec payments-db psql -U postgres -d payments_db -c "SELECT id, provider_event_id, state, attempts, error FROM provider_inbox WHERE payment_id='<payment-id>'"
docker compose exec payments-db psql -U postgres -d payments_db -c "SELECT id, topic, published, occurred_at FROM outbox_events WHERE payment_id='<payment-id>'"
```
- `status = pending` без записи в `payment_attempts` — покупатель ещё не запустил оплату на странице checkout.
- `outbox_events.published = false` — событие ожидает отправки в Kafka (проверьте доступность брокера `kafka` и логи `payments`).
- `provider_inbox.state = accepted` и `error = attempt_not_ready` — callback пришёл раньше фиксации `external_id`, воркер повторит обработку до 5 раз.

## 2. Перезапуск Kafka и проверка Consumer Lag

При остановке `kafka` (`docker compose stop kafka`) создание платежей и приём callback провайдера продолжают работать, сохраняя события в `outbox_events` (`payments-db`). После запуска (`docker compose start kafka`) `PaymentEventWorker` автоматически публикует накопленные записи.

Проверка групп потребителей и отставания (lag):
```bash
docker compose exec kafka /opt/kafka/bin/kafka-consumer-groups.sh \
  --bootstrap-server kafka:19092 --describe --all-groups
```

## 3. Диагностика доставки Webhook и сбоя Redis (`notifications-db` и `redis`)

```bash
docker compose exec notifications-db psql -U postgres -d notifications_db -c "SELECT id, shop_id, event_id, state, attempts, last_result, next_at FROM webhook_deliveries"
docker compose exec notifications-db psql -U postgres -d notifications_db -c "SELECT delivery_id, attempted_at, http_status, result FROM webhook_delivery_attempts ORDER BY attempted_at DESC LIMIT 10"
```
- Локальный приёмник `/local/webhooks/receiver` возвращает `503` на первую попытку каждого `event_id` и `200` на вторую.
- При недоступности `redis` (`docker compose stop redis`) воркер `Notifications` не теряет доставку, а откладывает её со статусом `pending` (`last_result = redis_unavailable`) на 5 секунд без расходования попыток. После `docker compose start redis` доставка штатно завершается.

## 4. Диагностика квитанций и SMTP (`receipts-db` и `mailpit`)

```bash
docker compose exec receipts-db psql -U postgres -d receipts_db -c "SELECT id, shop_id, payment_id, event_id, email_state, created_at FROM demo_receipts"
```
- Отправленные демонстрационные квитанции доступны в веб-интерфейсе Mailpit: `http://localhost:8025` или по API `http://localhost:8025/api/v1/messages`.
- Квитанции создаются только для `payment.succeeded`; для `payment.canceled` запись в `receipt_inbox` фиксируется без создания `demo_receipts`.
- Если `email_state = sending`, значит во время вызова SMTP произошёл сетевой сбой с неопределённым исходом; автоматическая повторная отправка заблокирована во избежание дублирования писем до ручной сверки.
