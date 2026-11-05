# Dobropay

Локальная песочница платёжного шлюза. Магазин создаёт платёж в RUB по API, покупатель выбирает исход в гостевом checkout, Worker применяет входящее событие и доставляет подписанный webhook. Деньги не обрабатываются. Реальный провайдер и внешний деплой не реализованы.

## Быстрый старт

Нужны Docker Desktop и Docker Compose. Из корня репозитория:

```bash
cp .env.example .env
docker compose up -d --build
docker compose ps
curl -fsS http://localhost:8080/health/ready
```

`setup` применяет EF Core migrations и повторяемо создаёт два магазина. Демо-ключи перечислены в `.env.example`; настройка webhook у обоих магазинов указывает на локальный получатель. Данные PostgreSQL лежат в именованном томе и переживают `docker compose restart`.

## Оплата от API до webhook

Нужен `jq`. Команды используют ключ первого магазина из `.env.example`:

```bash
PAYMENT=$(curl -fsS -X POST http://localhost:8080/v1/payments \
  -H 'X-Api-Key: dp_test_shop_a_123456789' \
  -H 'Idempotency-Key: demo-order-1' \
  -H 'Content-Type: application/json' \
  -d '{"amount":129900,"currency":"RUB","externalOrderId":"order-1"}')
echo "$PAYMENT" | jq .
ID=$(echo "$PAYMENT" | jq -r .id)
CHECKOUT=$(echo "$PAYMENT" | jq -r .checkoutUrl)
curl -fsS -o /dev/null -X POST "$CHECKOUT/start"
curl -fsS -o /dev/null -X POST "$CHECKOUT/simulate/succeeded"
sleep 6
curl -fsS -H 'X-Api-Key: dp_test_shop_a_123456789' "http://localhost:8080/v1/payments/$ID" | jq .
curl -fsS http://localhost:8080/local/webhooks/received | jq .
```

Ожидаются статус `succeeded` и запись получателя с `verified: true`, `attempts: 2`: первый ответ — `503`, второй — `200`. Для отмены создайте новый платёж с другим `Idempotency-Key` и вызовите `/simulate/canceled`. Checkout доступен браузеру по `checkoutUrl`; `GET` только показывает состояние.

Повторите `POST` с тем же ключом и телом: вернётся тот же `id`. Измените сумму с тем же ключом: получите `409`. Используйте ключ второго магазина при чтении первого платежа: получите `404`.

## Разработка

```bash
dotnet build Dobropay.slnx -warnaserror
dotnet test Dobropay.slnx --no-build
```

Перед локальным коммитом выполняются сборка без предупреждений и тесты. Полный набор в CI запускается на PR и после мержа в `main`, без отдельного прогона после каждого коммита. Запуск миграций и сброс данных описаны в [локальной разработке](docs/local-development.md). Контракт API — в [docs/api.md](docs/api.md), OpenAPI — `http://localhost:8080/swagger/v1/swagger.json`.
