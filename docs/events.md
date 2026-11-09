# События Kafka, Outbox/Inbox и надёжная доставка

Асинхронное взаимодействие между сервисами `Dobropay.Payments`, `Dobropay.Notifications` и `Dobropay.Receipts` построено поверх Apache Kafka (режим KRaft) с использованием паттернов **Transactional Outbox** и **Transactional Inbox**.

## 1. Топики и группы потребителей

| Топик | Producer | Consumer Group | Назначение |
| --- | --- | --- | --- |
| `dobropay.provider-events.v1` | `Dobropay.Payments` (outbox входящего callback) | `payments-provider-events-v1` (`Dobropay.Payments`) | Асинхронная сверка принятого callback провайдера и применение перехода статуса платежа. |
| `dobropay.payment-events.v1` | `Dobropay.Payments` (outbox бизнес-событий) | `notifications-payment-events-v1` (`Dobropay.Notifications`), `receipts-payment-events-v1` (`Dobropay.Receipts`) | Уведомление смежных сервисов о переходе платежа в терминальный статус (`payment.succeeded`, `payment.canceled`). |
| `dobropay.dlq.payments-provider-events.v1` | `Dobropay.Payments` | Инструмент диагностики и контролируемого повтора | Dead Letter Queue для неподдерживаемых версий/типов событий входящего контура. |
| `dobropay.dlq.notifications-payment-events.v1` | `Dobropay.Notifications` | Инструмент диагностики и контролируемого повтора | Dead Letter Queue для неподдерживаемых событий в Notifications. |
| `dobropay.dlq.receipts-payment-events.v1` | `Dobropay.Receipts` | Инструмент диагностики и контролируемого повтора | Dead Letter Queue для неподдерживаемых событий в Receipts. |

Ключ партиционирования (`Message.Key`) во всех топиках — `payment_id` в формате `N` (UUID без дефисов), что гарантирует упорядоченность событий одного платежа внутри партиции.

## 2. Конверт события (`EventEnvelope`)

Каждое сообщение в Kafka сериализуется в JSON со следующей структурой:

```json
{
  "event_id": "0195f1a2-3b4c-7d8e-9f01-23456789abcd",
  "event_type": "payment.succeeded",
  "schema_version": 1,
  "occurred_at": "2026-11-07T14:00:00Z",
  "shop_id": "11111111-1111-4111-8111-111111111111",
  "payment_id": "99999999-9999-4999-8999-999999999999",
  "correlation_id": "0HN7...",
  "payload": {
    "id": "99999999-9999-4999-8999-999999999999",
    "status": "succeeded",
    "amount": 129900,
    "currency": "RUB"
  }
}
```

### Схемы `payload` по типам событий (`schema_version = 1`)
- `provider.event.accepted` (топик `dobropay.provider-events.v1`):
  - `eventId` (`string`), `shopId` (`uuid`), `attemptId` (`uuid`), `externalAttemptId` (`string`), `amount` (`long`), `currency` (`"RUB"`), `outcome` (`"succeeded" | "canceled"`).
- `payment.succeeded` и `payment.canceled` (топик `dobropay.payment-events.v1`):
  - `id` (`uuid`), `status` (`"succeeded" | "canceled"`), `amount` (`long`), `currency` (`"RUB"`).

Карточные реквизиты, секреты, токены checkout и персональные данные покупателя в события Kafka не передаются.

## 3. Гарантии доставки, Outbox, Inbox и управление Offset

1. **Публикация из Outbox**:
   - При приёме callback провайдера или смене статуса платежа запись в таблицу `outbox_events` (`published = false`) создаётся в той же транзакции PostgreSQL, что и основное изменение.
   - Фоновый воркер `PaymentEventWorker` выбирает неопубликованное событие с блокировкой `FOR UPDATE SKIP LOCKED`, публикует его через `IEventPublisher` (`Acks.All`, `EnableIdempotence = true`) и помечает `published = true`.
2. **Потребление и ручной коммит Offset**:
   - Все потребители работают с `EnableAutoCommit = false` и `EnableAutoOffsetStore = false`.
   - Смещение (`Commit`) подтверждается **только после** успешной фиксации транзакции в собственной БД сервиса (запись в `notification_inbox` + создание `webhook_deliveries` либо запись в `receipt_inbox` и обработка фискального документа).
3. **Дедупликация и защита от повторов**:
   - Таблицы `provider_inbox`, `notification_inbox` и `receipt_inbox` имеют уникальные ограничения по идентификатору события (`provider_event_id` / `event_id`). Повторная доставка того же сообщения из Kafka обрабатывается идемпотентно (`ON CONFLICT DO NOTHING`) и не создаёт дублей бизнес-операций, webhook-доставок или квитанций.
4. **Неопределённый исход SMTP в Receipts**:
   - Перед вызовом сервиса `Email` статус квитанции переводится в `sending`. Если сетевой вызов к `Email` завершается с неопределённым исключением (например, обрыв соединения после отправки), статус остаётся `sending`, предотвращая бесконтрольную повторную рассылку дублирующихся писем.

## 4. Контролируемый повтор из DLQ

При попадании сообщения в топик `dobropay.dlq.<consumer>.v1` оператор может просмотреть его содержимое и после устранения причины переотправить в целевой топик:
```bash
docker compose exec kafka /opt/kafka/bin/kafka-console-consumer.sh \
  --bootstrap-server kafka:19092 \
  --topic dobropay.dlq.notifications-payment-events.v1 \
  --from-beginning --timeout-ms 5000
```
