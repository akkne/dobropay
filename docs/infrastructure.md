# Инфраструктура Dobropay

Локальный контур разворачивается через Docker Compose (`compose.yaml`) и полностью реализует паттерн **Database-per-Service**, брокер сообщений Kafka, кэш/лимитер Redis, SMTP-приёмник Mailpit и входной шлюз Caddy.

## 1. Контейнеры и зависимости

| Сервис Compose | Образ / Сборка | Назначение | Внешние порты (localhost) | Volume |
| --- | --- | --- | --- | --- |
| `auth-db` | `postgres:17.6` | Изолированная БД сервиса `auth` (`auth_db`) | `127.0.0.1:${AUTH_DB_PORT:-5433}:5432` | `auth_db_data` |
| `merchant-dev-db` | `postgres:17.6` | Изолированная БД сервиса `developer-api` (`merchant_dev_db`) | `127.0.0.1:${MERCHANT_DEV_DB_PORT:-5434}:5432` | `merchant_dev_db_data` |
| `payments-db` | `postgres:17.6` | Изолированная БД сервиса `payments` (`payments_db`) | `127.0.0.1:${PAYMENTS_DB_PORT:-5435}:5432` | `payments_db_data` |
| `notifications-db` | `postgres:17.6` | Изолированная БД сервиса `notifications` (`notifications_db`) | `127.0.0.1:${NOTIFICATIONS_DB_PORT:-5436}:5432` | `notifications_db_data` |
| `receipts-db` | `postgres:17.6` | Изолированная БД сервиса `receipts` (`receipts_db`) | `127.0.0.1:${RECEIPTS_DB_PORT:-5437}:5432` | `receipts_db_data` |
| `kafka` | `apache/kafka:4.1.2` | Брокер сообщений в режиме KRaft | Внутренняя сеть (`kafka:19092`) | `kafka_data` |
| `kafka-init` | `apache/kafka:4.1.2` | Идемпотентное создание топиков (`--partitions 3 --replication-factor 1`) | Одноразовый init-контейнер | — |
| `redis` | `redis:7.4.12-alpine` | TTL-лимитер исходящих webhook для `notifications` | Внутренняя сеть (`redis:6379`) | — |
| `mailpit` | `axllent/mailpit:v1.32.0` | Локальный SMTP-сервер и веб-интерфейс просмотра писем | `127.0.0.1:${MAILPIT_HTTP_PORT:-8025}:8025` | — |
| `setup` | `backend/Dobropay.Setup` | Настройка ролей, применение миграций во всех 5 БД и сидирование демо-данных | Одноразовый init-контейнер | — |
| `auth` | `backend/Dobropay.Auth` | Сервис аутентификации сотрудников | Внутренняя сеть (`:8080`) | — |
| `developer-api` | `backend/Dobropay.DeveloperApi` | Сервис магазинов, членства и API-ключей | Внутренняя сеть (`:8080`) | — |
| `payments` | `backend/Dobropay.Payments` | Сервис платежей, checkout и симулятора провайдера | Внутренняя сеть (`:8080`) | — |
| `notifications` | `backend/Dobropay.Notifications` | Сервис доставки webhook и демо-получатель | Внутренняя сеть (`:8080`) | — |
| `email` | `backend/Dobropay.Email` | Сервис отправки писем по SMTP | Внутренняя сеть (`:8080`) | — |
| `receipts` | `backend/Dobropay.Receipts` | Сервис формирования квитанций | Внутренняя сеть (`:8080`) | — |
| `merchant-admin` | `backend/Dobropay.MerchantAdmin` | Сервис личного кабинета магазина | Внутренняя сеть (`:8080`) | — |
| `gateway` | `caddy:2.10.2-alpine` | Входной HTTP-шлюз (маршрутизация публичных запросов) | `127.0.0.1:${GATEWAY_PORT:-8080}:8080` | — |

## 2. Маршрутизация Gateway (`infra/Caddyfile`)

Публичный вход открыт только через `gateway` (`http://localhost:8080`):
- `/v1/auth/*` → `auth:8080`
- `/v1/developer/*` → `developer-api:8080`
- `/v1/admin/*` → `merchant-admin:8080`
- `/v1/payments*` → `payments:8080`
- `/checkout/*` → `payments:8080`
- `/v1/provider/sandbox/events` → `payments:8080`
- `/local/webhooks/received` → `notifications:8080`
- `/swagger*` → `payments:8080`
- `/health/live`, `/health/ready` → проверка живости и готовности.

Все маршруты `/internal/*` недоступны снаружи через Gateway и вызываются только внутри приватной сети Compose с проверкой `X-Internal-Key`.

## 3. Переменные окружения (`.env`)

Все чувствительные параметры, строки подключения, адреса сервисов, имена хостов и порты вынесены в `.env` (шаблон — в `.env.example`):
- Пароли суперпользователей, миграторов и runtime-ролей для каждой из 5 БД (`AUTH_*`, `DEVELOPER_API_*`, `PAYMENTS_*`, `NOTIFICATIONS_*`, `RECEIPTS_*`).
- Секреты криптографии и межсервисной аутентификации (`CHECKOUT_SECRET`, `PROVIDER_SECRET`, `WEBHOOK_SECRET`, `INTERNAL_SERVICE_KEY`).
- Демо-данные для локального старта (`DEMO_STAFF_EMAIL`, `DEMO_STAFF_PASSWORD`, `DEMO_KEY_A`, `DEMO_KEY_B`, `DEMO_WEBHOOK_URL`, `DEMO_RECEIPT_EMAIL`).
- Настройки брокера, кэша и SMTP (`KAFKA_BOOTSTRAP_SERVERS`, `REDIS_CONNECTION`, `SMTP_HOST`, `SMTP_PORT`, `SMTP_FROM`).

## 4. Healthchecks и готовность к пилоту

- Каждый контейнер PostgreSQL проверяется через `pg_isready`.
- Брокер Kafka проверяется через `kafka-topics.sh --list`.
- Redis проверяется через `redis-cli ping`.
- Все микросервисы предоставляют `/health/live` и `/health/ready` (с проверкой реального подключения к своей БД через `IDbSessionFactory`).
- **Целевая конфигурация для пилота**:
  - Kafka кластер из 3 брокеров с `replication.factor=3`, `min.insync.replicas=2`, TLS и SASL/SCRAM ACL на топики для каждого сервиса.
  - Управляемые инстансы PostgreSQL с регулярным резервным копированием (WAL archiving) и TLS-соединениями.
  - Сбор метрик Prometheus/OpenTelemetry: lag групп потребителей Kafka, возраст неопубликованных записей `outbox_events`, количество зависших доставок в `webhook_deliveries` и ошибок SMTP.
