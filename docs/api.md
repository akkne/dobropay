# Контракт M1

Суммы — целые копейки, валюта только `RUB`. Статусы платежа: `pending`, `succeeded`, `canceled`. Серверный ключ передаётся в `X-Api-Key`; ключ не передают покупателю.

## Создание

`POST /v1/payments`, заголовок `Idempotency-Key` обязателен (1–200 символов).

```bash
curl -i -X POST http://localhost:8080/v1/payments \
  -H 'X-Api-Key: dp_test_shop_a_123456789' -H 'Idempotency-Key: order-42' \
  -H 'Content-Type: application/json' \
  -d '{"amount":129900,"currency":"RUB","externalOrderId":"order-42"}'
```

Ответ `201`: `{"id":"<uuid>","status":"pending","amount":129900,"currency":"RUB","checkoutUrl":"http://localhost:8080/checkout/<token>"}`. `externalOrderId` необязателен. Повтор с тем же ключом и нормализованным телом возвращает `200` и тот же ресурс. Для сравнения тела внешний ID обрезается по краям; сумма и валюта сравниваются по значениям. Другие параметры с тем же ключом дают `409 {"error":"idempotency_conflict"}`. Неверная сумма/валюта или отсутствие ключа — `400`; неверный API-ключ — `401`.

## Чтение

```bash
curl -i -H 'X-Api-Key: dp_test_shop_a_123456789' http://localhost:8080/v1/payments/<id>
```

`200`: `id`, `status`, `amount`, `currency`, `externalOrderId`. Неизвестный и чужой ID дают одинаковый `404 {"error":"payment_not_found"}`. Без действительного ключа — `401`.

## Checkout и симулятор

`GET /checkout/{token}` показывает имя магазина, сумму и статус. Токен привязан к платежу и действует 30 минут. `POST /checkout/{token}/start` создаёт или повторно использует попытку. В локальном режиме `POST /checkout/{token}/simulate/succeeded` и `/canceled` отправляют подписанное событие в `POST /local/provider/events`. Эти маршруты доступны только при `Sandbox:Enabled=true`. Возврат браузера и `GET` не меняют статус. Истёкший токен даёт `410`; начатая попытка остаётся в БД.

## Webhook магазина

Worker отправляет JSON с `event_id` (UUID), `event_type` (`payment.succeeded` или `payment.canceled`), `occurred_at` (UTC) и `payment` (`id`, `status`, `amount`, `currency`). Тот же `event_id` используется при повторной доставке. Заголовки:

- `X-Dobropay-Timestamp`: Unix seconds.
- `X-Dobropay-Signature`: `sha256=<lowercase hex HMAC-SHA256>`.

Подписываются исходные байты `UTF8(timestamp + ".") || raw_body`. Получатель проверяет формат, допустимое отклонение времени (в демо ±300 секунд), вычисляет HMAC на **исходном теле** и сравнивает подписи за постоянное время. Ответ 2xx подтверждает получение. Демо-получатель находится на `/local/webhooks/receiver`; первый запрос на каждый `event_id` возвращает `503`, второй — `200`. Состояние видно через `GET /local/webhooks/received`.

OpenAPI генерируется API на `/swagger/v1/swagger.json`; интерактивная страница — `/swagger`.
