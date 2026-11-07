# Контракт API Dobropay

Все публичные маршруты обслуживаются через Gateway (`http://localhost:8080`). Суммы платежей передаются целым числом в копейках, поддерживаемая валюта — `RUB`. Статусы платежа: `pending`, `succeeded`, `canceled`.

## 1. Аутентификация сотрудника (`Dobropay.Auth`)

### `POST /v1/auth/login`
Аутентификация сотрудника по email и паролю.
```bash
curl -fsS -X POST http://localhost:8080/v1/auth/login \
  -H 'Content-Type: application/json' \
  -d '{"email":"demo@dobropay.local","password":"local-demo-password"}'
```
Ответ `200 OK`:
```json
{
  "access_token": "<64-char-hex>",
  "token_type": "Bearer",
  "expires_in": 28800,
  "account_id": "33333333-3333-4333-8333-333333333333"
}
```
Неверные учётные данные возвращают `401 Unauthorized`.

## 2. Управление магазином, ключами и webhook (`Dobropay.DeveloperApi`)

Запросы требуют заголовок `Authorization: Bearer <access_token>` сотрудника, являющегося участником магазина.

### `POST /v1/developer/shops/{shopId}/keys`
Выпуск нового серверного API-ключа для магазина.
```bash
curl -fsS -X POST http://localhost:8080/v1/developer/shops/11111111-1111-4111-8111-111111111111/keys \
  -H "Authorization: Bearer $TOKEN"
```
Ответ `201 Created`: `{"id":"<uuid>","shopId":"<uuid>","apiKey":"dp_live_..."}`. В БД сохраняется только SHA-256 хеш ключа.

### `PUT /v1/developer/shops/{shopId}/webhooks`
Настройка подписки магазина на исходящие webhook (делегируется в `Dobropay.Notifications`).
```bash
curl -fsS -X PUT http://localhost:8080/v1/developer/shops/11111111-1111-4111-8111-111111111111/webhooks \
  -H "Authorization: Bearer $TOKEN" \
  -H 'Content-Type: application/json' \
  -d '{"url":"http://notifications:8080/local/webhooks/receiver","signingSecret":"local-webhook-secret-change-me-32-bytes"}'
```

## 3. API платежей магазина (`Dobropay.Payments`)

Серверный ключ передаётся в заголовке `X-Api-Key`.

### `POST /v1/payments`
Обязателен заголовок `Idempotency-Key` (1–200 символов).
```bash
curl -i -X POST http://localhost:8080/v1/payments \
  -H 'X-Api-Key: dp_test_shop_a_123456789' \
  -H 'Idempotency-Key: order-42' \
  -H 'Content-Type: application/json' \
  -d '{"amount":129900,"currency":"RUB","externalOrderId":"order-42"}'
```
Ответ `201 Created`:
```json
{
  "id": "<uuid>",
  "status": "pending",
  "amount": 129900,
  "currency": "RUB",
  "checkoutUrl": "http://localhost:8080/checkout/<token>"
}
```
Повтор с тем же `Idempotency-Key` и нормализованным телом возвращает `200 OK` и тот же платёж. Повтор с другим телом при том же ключе возвращает `409 Conflict` (`{"error":"idempotency_conflict"}`).

### `GET /v1/payments/{id}`
```bash
curl -i -H 'X-Api-Key: dp_test_shop_a_123456789' http://localhost:8080/v1/payments/<id>
```
Ответ `200 OK`: `{"id":"<uuid>","status":"succeeded","amount":129900,"currency":"RUB","externalOrderId":"order-42"}`. Чужой и несуществующий ID возвращают одинаковый `404 Not Found` (`{"error":"payment_not_found"}`).

## 4. Гостевой Checkout и симулятор провайдера (`Dobropay.Payments`)

- `GET /checkout/{token}` — HTML-страница с названием магазина, суммой и текущим статусом. Срок действия ссылки — 30 минут (после истечения возвращается `410 Gone`).
- `POST /checkout/{token}/start` — создаёт или переиспользует попытку оплаты (`payment_attempts`) и обращается к адаптеру провайдера.
- `POST /checkout/{token}/simulate/succeeded` и `POST /checkout/{token}/simulate/canceled` — отправляют подписанный `X-Provider-Signature` (HMAC-SHA256) серверный callback на `POST /v1/provider/sandbox/events`.

## 5. Кабинет магазина (`Dobropay.MerchantAdmin`)

Требуется заголовок `Authorization: Bearer <access_token>`. Если сотрудник не состоит в магазине, возвращается `404 Not Found`.
- `GET /v1/admin/shops/{shopId}/payments` — список платежей магазина.
- `GET /v1/admin/shops/{shopId}/payments/{id}` — детали платежа.
- `GET /v1/admin/shops/{shopId}/deliveries` — история доставки исходящих webhook.
- `GET /v1/admin/shops/{shopId}/receipts` — список демонстрационных квитанций и статус отправки email.

## 6. Исходящий Webhook магазина (`Dobropay.Notifications`)

`Notifications` отправляет `POST` на URL подписки магазина с телом:
```json
{
  "event_id": "<uuid>",
  "event_type": "payment.succeeded",
  "occurred_at": "2026-11-07T14:00:00Z",
  "payment": {
    "id": "<uuid>",
    "status": "succeeded",
    "amount": 129900,
    "currency": "RUB"
  }
}
```
Заголовки подписи:
- `X-Dobropay-Timestamp`: Unix timestamp в секундах.
- `X-Dobropay-Signature`: `sha256=<lowercase hex HMAC-SHA256>` от байтов `UTF8(timestamp + ".") || raw_body`.

Локальный демо-приёмник доступен на `/local/webhooks/receiver` (первая попытка на `event_id` возвращает `503`, вторая — `200`). Просмотр принятых уведомлений: `GET /local/webhooks/received`. OpenAPI доступен по адресу `http://localhost:8080/swagger/v1/swagger.json`.
