# Dobropay

Локальная песочница платёжного шлюза на микросервисной архитектуре с паттерном **Database-per-Service**. Магазин создаёт платёж в `RUB` по API, покупатель выбирает исход в гостевом checkout, событие проходит через Transactional Outbox и Apache Kafka, после чего `Notifications` доставляет подписанный HMAC-SHA256 webhook с лимитированием в Redis, а `Receipts` формирует демонстрационную квитанцию и отправляет письмо через сервис `Email` в Mailpit.

## Обязательные стандарты для разработчиков и агентов

Перед началом любой работы с проектом обязательно ознакомьтесь с:
- **[AGENTS.md](AGENTS.md)** — главный стандарт работы агента с проектом;
- **[docs/code-style.md](docs/code-style.md)** — строгий Code Style (Clean Code без ИИ-артефактов, ООП: сначала абстракция — потом реализация, **один класс — один файл**, никаких магических строк, все секреты и настройки в `.env`);
- **[docs/git-process.md](docs/git-process.md)** — регламент работы с Git, ветками, коммитами и чистотой рабочего дерева;
- **[docs/README.md](docs/README.md)** — полная карта документации ([архитектура](docs/architecture.md), [инфраструктура](docs/infrastructure.md), [данные и RLS](docs/data.md), [события Kafka](docs/events.md), [API](docs/api.md), [runbook](docs/runbook.md)).

## Архитектура контура (Database-per-Service)

Все серверные проекты находятся в `backend/`:
- **`Dobropay.Auth`** (`auth`) — аутентификация сотрудников и сессии; собственная БД `auth-db` (`auth_db`).
- **`Dobropay.DeveloperApi`** (`developer-api`) — магазины, членство, API-ключи и настройка подписок; собственная БД `merchant-dev-db` (`merchant_dev_db`).
- **`Dobropay.Payments`** (`payments`) — платежи, идемпотентность, checkout, симулятор провайдера, outbox/inbox и обработчик Kafka; собственная БД `payments-db` (`payments_db`).
- **`Dobropay.Notifications`** (`notifications`) — подписки, доставка подписанных webhook, повторы, локальный приёмник; собственная БД `notifications-db` (`notifications_db`) + `redis`.
- **`Dobropay.Receipts`** (`receipts`) — демонстрационные квитанции и задания на отправку писем; собственная БД `receipts-db` (`receipts_db`).
- **`Dobropay.Email`** (`email`) — отправка писем по SMTP в `mailpit` (без собственной БД).
- **`Dobropay.MerchantAdmin`** (`merchant-admin`) — кабинет магазина, агрегирующий данные через внутренние API сервисов (без собственной БД).
- **Общие пакеты**: `Dobropay.Contracts` (версионированные контракты и константы), `Dobropay.Messaging` (абстракции и реализации транспорта Kafka и подключений к БД), `Dobropay.Setup` (миграции 5 изолированных БД, настройка ролей RLS и демо-данных).

## Быстрый старт

Требуются Docker Desktop (Docker Compose), .NET 10 SDK, `curl` и `jq`:

```bash
cp .env.example .env
docker compose up -d --build
docker compose ps
curl -fsS http://localhost:8080/health/ready
```

## Сквозное демо: от входа сотрудника до webhook и письма

```bash
# 1. Вход сотрудника через Auth
TOKEN=$(curl -fsS -X POST http://localhost:8080/v1/auth/login \
  -H 'Content-Type: application/json' \
  -d '{"email":"demo@dobropay.local","password":"local-demo-password"}' | jq -r .access_token)

# 2. Создание платежа магазина A
PAYMENT=$(curl -fsS -X POST http://localhost:8080/v1/payments \
  -H 'X-Api-Key: dp_test_shop_a_123456789' \
  -H 'Idempotency-Key: demo-order-1' \
  -H 'Content-Type: application/json' \
  -d '{"amount":129900,"currency":"RUB","externalOrderId":"order-1"}')
echo "$PAYMENT" | jq .
ID=$(echo "$PAYMENT" | jq -r .id)
CHECKOUT=$(echo "$PAYMENT" | jq -r .checkoutUrl)

# 3. Покупатель начинает оплату и подтверждает успех в симуляторе
curl -fsS -o /dev/null -X POST "$CHECKOUT/start"
curl -fsS -o /dev/null -X POST "$CHECKOUT/simulate/succeeded"
sleep 6

# 4. Проверка статуса платежа по API магазина и в кабинете MerchantAdmin
curl -fsS -H 'X-Api-Key: dp_test_shop_a_123456789' "http://localhost:8080/v1/payments/$ID" | jq .
curl -fsS -H "Authorization: Bearer $TOKEN" \
  "http://localhost:8080/v1/admin/shops/11111111-1111-4111-8111-111111111111/payments/$ID" | jq .

# 5. Проверка доставленного подписанного webhook и отправленной демо-квитанции
curl -fsS http://localhost:8080/local/webhooks/received | jq .
curl -fsS -H "Authorization: Bearer $TOKEN" \
  "http://localhost:8080/v1/admin/shops/11111111-1111-4111-8111-111111111111/receipts" | jq .
curl -fsS http://localhost:8025/api/v1/messages | jq '.messages | length'
```

## Сборка и тестирование

```bash
dotnet build Dobropay.slnx -warnaserror
dotnet test Dobropay.slnx --no-build
```
