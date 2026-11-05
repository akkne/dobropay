# Архитектура M0–M1

Merchants владеет `shops`, `api_credentials`; Payments — `payments`, `payment_attempts`, `idempotency_records`, `provider_inbox`, `outbox_events`; Checkout — гостевым маршрутом и `checkout_links`; Notifications — `webhook_subscriptions`, `webhook_deliveries`, `webhook_delivery_attempts`. API и Worker обращаются к общей PostgreSQL. `Payments` единолично меняет статус платежа; состояние попытки хранится отдельно.

```mermaid
flowchart LR
  Shop[Магазин] --> API[API]
  Buyer[Покупатель] --> Checkout[Checkout в API]
  Checkout --> Simulator[Локальный симулятор]
  Simulator --> Inbox[(PostgreSQL inbox)]
  API --> DB[(PostgreSQL)]
  Inbox --> Worker[Worker]
  Worker --> DB
  Worker --> Receiver[Локальный получатель webhook]
```

```mermaid
sequenceDiagram
  participant M as Магазин
  participant A as API/Checkout
  participant P as Simulator
  participant D as PostgreSQL
  participant W as Worker
  M->>A: POST payment + Idempotency-Key
  A->>D: payment + idempotency + token (транзакция)
  A-->>M: id, pending, checkoutUrl
  A->>D: attempt + operationKey (транзакция)
  A->>P: create attempt
  P->>A: signed event
  A->>D: inbox до ACK
  W->>D: inbox + payment transition + outbox (транзакция)
  W->>D: delivery lease
  W->>M: signed webhook
  W->>D: result + delivery attempt
```

Платёж переходит только `pending → succeeded|canceled`; терминальный статус не меняется. Попытка остаётся `unknown` до подтверждения. Входящие повторы ограничены уникальным `provider_event_id`. Если внешний ID ещё не записан, Worker откладывает обработку с ограничением числа попыток. Таймаут адаптера сохраняет неизвестный исход; уточнение запланировано для M2. Истечение checkout-токена не меняет платёж.

API определяет магазин по хешу серверного ключа через узкую `SECURITY DEFINER` функцию. Checkout делает точный поиск по хешу токена. Для таблиц магазина включены `ENABLE` и `FORCE ROW LEVEL SECURITY`; политики проверяют `NULLIF(current_setting('app.shop_id',true),'')::uuid` в `USING` и `WITH CHECK`. Runtime-роли не имеют `BYPASSRLS`. Каждый бизнес-запрос открывает транзакцию и задаёт проверенный контекст через параметризованный `set_config`. Worker может читать очередь глобально, но меняет бизнес-строки с контекстом магазина. Задание доставки фиксируется и получает аренду до сетевого запроса; истекшая аренда повторно выбирается.

Запланировано: аккаунты и членство, возвраты, управление подписками, реальный провайдер, сверка и фискализация. Целевая карта подробнее находится в `docs/architecture/ARCHITECTURE.md`.
