# Инфраструктура Dobropay

`compose.yaml` запускает пять отдельных PostgreSQL (`auth-db`, `merchant-dev-db`, `payments-db`, `notifications-db`, `receipts-db`), Kafka, Redis, семь серверных сервисов, Next.js, Caddy и одноразовый `setup`. Базы доступны с хоста только на `127.0.0.1`; внутренние HTTP-маршруты и инфраструктура не проксируются через Caddy. `setup` применяет миграции без начальных аккаунтов и магазинов.

Конфигурация берётся из `.env` по шаблону [.env.example](../.env.example). Compose требует непустые пароли пяти БД, ключи межсервисного взаимодействия, домен, публичный URL, внешний SMTP с TLS и учётными данными, а также параметры облачной кассы. Секреты не входят в репозиторий.

Caddy маршрутизирует `/v1/auth/*`, `/v1/developer/*`, `/v1/admin/*`, `/v1/payments*`, `/v1/refunds*`, `/v1/payment-methods*`, `/v1/checkout/*`, `/checkout/*/session`, `/checkout/*/start`, `/v1/provider/*` и `/v1/receipts*`. Все остальные страницы обслуживает Next.js. Публичных маршрутов локального webhook-приёмника и симуляции на `/checkout/*/simulate/*` нет.

`SANDBOX_ENABLED=false` блокирует изменяющие запросы в Payments и возвращает `503` на `/health/ready`. Это явная блокировка при отсутствии реального платёжного адаптера. Compose содержит один брокер Kafka с фактором репликации 1; требования к инфраструктуре выпуска перечислены в [плане подготовки](production-readiness.md).
