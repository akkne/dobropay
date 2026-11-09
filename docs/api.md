# Публичный API Dobropay

Запросы проходят через Caddy по HTTPS-домену из `.env`. Суммы в API платежей передаются целым числом в копейках; валюта `RUB`.

## Аккаунты и магазины

- `POST /v1/auth/register`, `POST /v1/auth/login`, `GET /v1/auth/me`, `POST /v1/auth/refresh`, `POST /v1/auth/logout` — аккаунт и сессия сотрудника.
- `GET/POST /v1/developer/shops` — список доступных магазинов и создание магазина. Требуется `Authorization: Bearer <access_token>`.
- `GET/PUT /v1/developer/shops/{shopId}/onboarding`, `POST /v1/developer/shops/{shopId}/onboarding/submit` — реквизиты и заявка на проверку.
- `GET/POST /v1/developer/shops/{shopId}/keys`, `DELETE /v1/developer/shops/{shopId}/keys/{keyId}` — выпуск и отзыв API-ключей. Владелец магазина имеет права Developer на управление ключами. Тестовый ключ доступен сразу после создания магазина, даже если анкета не заполнена; боевой ключ выдаётся только после одобрения заявки.

## Платежи и кабинет

- `POST /v1/payments`, `GET /v1/payments/{id}`, `POST /v1/payments/{id}/capture`, `POST /v1/payments/{id}/cancel`, `POST /v1/refunds`, `GET /v1/refunds/{id}` — серверный API с `X-Api-Key` и `Idempotency-Key` для изменяющих операций.
- `GET /checkout/{token}/session` — данные платёжной сессии. Текущий публичный checkout показывает только статус.
- `GET /v1/admin/shops/{shopId}/payments`, `GET /v1/admin/shops/{shopId}/deliveries`, `GET /v1/admin/shops/{shopId}/receipts` — чтение в кабинете сотрудника.
- `GET /v1/receipts/lookup` — поиск фискального документа.

При `SANDBOX_ENABLED=false` все изменяющие запросы к Payments возвращают `503`, пока не реализован реальный эквайринг. Существующие банковские и СБП адаптеры синтетические; этот флаг нельзя использовать как переключатель боевых операций. Публичной настройки webhook-подписки пока нет, доступен только внутренний API Notifications. Детали ограничений — в [production-readiness.md](production-readiness.md).
