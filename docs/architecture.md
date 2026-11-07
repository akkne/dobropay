# Архитектура микросервисов Dobropay

Dobropay построен по микросервисной архитектуре с паттерном **Database-per-Service**. Каждый доменный сервис разворачивается как отдельный процесс и контейнер, имеет собственную конфигурацию и, при наличии состояния, подключается исключительно к своему собственному физически изолированному контейнеру PostgreSQL.

## 1. Состав сервисов и границы ответственности

| Сервис | Контейнер | База данных (контейнер / БД) | Ответственность |
| --- | --- | --- | --- |
| `Dobropay.Auth` | `auth` | `auth-db` / `auth_db` | Аутентификация сотрудников, хранение аккаунтов и активных сессий, проверка токенов по внутреннему API. |
| `Dobropay.DeveloperApi` | `developer-api` | `merchant-dev-db` / `merchant_dev_db` | Магазины, членство сотрудников, серверные API-ключи (SHA-256 хеши), делегирование настроек webhook в Notifications. |
| `Dobropay.Payments` | `payments` | `payments-db` / `payments_db` | Создание и чтение платежей, идемпотентность, гостевой checkout, симулятор провайдера, приём callback в `provider_inbox`, публикация и обработка событий через Kafka. |
| `Dobropay.Notifications` | `notifications` | `notifications-db` / `notifications_db` + `redis` | Подписки на webhook, потребление событий платежей из Kafka, TTL-лимитирование через Redis, HMAC-SHA256 подпись и доставка с повторами, локальный демо-приёмник. |
| `Dobropay.Receipts` | `receipts` | `receipts-db` / `receipts_db` | Потребление событий успешной оплаты из Kafka, формирование демонстрационной квитанции, отправка писем через сервис Email. |
| `Dobropay.Email` | `email` | Без собственной БД (`mailpit` SMTP) | Приём внутренних запросов на отправку писем и передача в SMTP-сервер. |
| `Dobropay.MerchantAdmin` | `merchant-admin` | Без собственной БД | API личного кабинета магазина: проверка токена через Auth, проверка членства через Developer API, чтение платежей, доставок и квитанций через внутренние API сервисов-владельцев. |

## 2. Диаграмма связей (Database-per-Service & Kafka)

```mermaid
flowchart TB
  Client["Клиент / Магазин / Покупатель"] --> Gateway["Gateway (Caddy :8080)"]

  Gateway --> Auth["Dobropay.Auth"]
  Gateway --> DevApi["Dobropay.DeveloperApi"]
  Gateway --> Admin["Dobropay.MerchantAdmin"]
  Gateway --> Payments["Dobropay.Payments"]
  Gateway --> Notifications["Dobropay.Notifications (Local Webhook Receiver)"]

  Auth --> AuthDb[("auth-db (PostgreSQL)")]
  DevApi --> DevDb[("merchant-dev-db (PostgreSQL)")]
  Payments --> PayDb[("payments-db (PostgreSQL)")]
  Notifications --> NotifDb[("notifications-db (PostgreSQL)")]
  Notifications --> Redis[("redis (Rate Limiter)")]
  Receipts["Dobropay.Receipts"] --> RecDb[("receipts-db (PostgreSQL)")]

  Payments -->|Internal HTTP| DevApi
  DevApi -->|Internal HTTP| Auth
  DevApi -->|Internal HTTP| Notifications
  Admin -->|Internal HTTP| Auth
  Admin -->|Internal HTTP| DevApi
  Admin -->|Internal HTTP| Payments
  Admin -->|Internal HTTP| Notifications
  Admin -->|Internal HTTP| Receipts

  Payments <-->|Kafka: provider-events.v1| Kafka[("Apache Kafka (KRaft)")]
  Payments -->|Kafka: payment-events.v1| Kafka
  Kafka -->|Kafka: payment-events.v1| Notifications
  Kafka -->|Kafka: payment-events.v1| Receipts

  Receipts -->|Internal HTTP| Email["Dobropay.Email"]
  Email -->|SMTP| Mailpit["Mailpit (:8025)"]
```

## 3. Сквозной сценарий (Sequence Diagram)

```mermaid
sequenceDiagram
  participant Shop as Магазин
  participant GW as Gateway
  participant Pay as Payments
  participant Dev as DeveloperApi
  participant PayDB as payments-db
  participant Kafka as Kafka
  participant Notif as Notifications
  participant Rec as Receipts
  participant Email as Email / Mailpit

  Shop->>GW: POST /v1/payments (X-Api-Key, Idempotency-Key)
  GW->>Pay: POST /v1/payments
  Pay->>Dev: POST /internal/v1/keys/resolve
  Dev-->>Pay: shopId, shopName
  Pay->>PayDB: TX (set_config shop_id, insert payment + idempotency + checkout_link)
  Pay-->>Shop: 201 Created (id, pending, checkoutUrl)

  Shop->>GW: POST /checkout/{token}/start
  GW->>Pay: POST /checkout/{token}/start
  Pay->>PayDB: TX (create/reuse payment_attempt + external_id)

  Shop->>GW: POST /checkout/{token}/simulate/succeeded
  GW->>Pay: POST /v1/provider/sandbox/events (HMAC-SHA256)
  Pay->>PayDB: TX (insert provider_inbox + outbox_event)
  Pay-->>Shop: 202 Accepted

  Pay->>Kafka: Publish dobropay.provider-events.v1 (from outbox)
  Kafka->>Pay: Consume dobropay.provider-events.v1
  Pay->>PayDB: TX (verify attempt/amount, pending -> succeeded, insert outbox payment.succeeded)
  Pay->>Kafka: Publish dobropay.payment-events.v1

  Kafka->>Notif: Consume dobropay.payment-events.v1
  Notif->>Notif: Save inbox + delivery, check Redis rate-limit, send signed webhook

  Kafka->>Rec: Consume dobropay.payment-events.v1
  Rec->>Rec: Save inbox + demo_receipt (pending)
  Rec->>Email: POST /internal/v1/email
  Email-->>Rec: 202 Accepted (state -> sent)
```

## 4. Синхронные и асинхронные взаимодействия

- **Синхронные вызовы (HTTP)**:
  - Используются только для валидации ключей/сессий/членства, настройки подписок и агрегации чтения в `MerchantAdmin`, а также вызова `Email` из `Receipts`.
  - Защищены заголовком `X-Internal-Key` (сравнение хешей за константное время) и изолированы внутри Docker-сети.
- **Асинхронные события (Kafka + Transactional Outbox/Inbox)**:
  - Переход статуса платежа и бизнес-реакции (`Notifications`, `Receipts`) развязаны через Transactional Outbox в `payments-db` и Transactional Inbox в сервисах-потребителях.
  - Даже при временной недоступности Kafka или соседних сервисов принятые платежи и callback'и провайдера надёжно сохраняются в БД и доставляются после восстановления.
