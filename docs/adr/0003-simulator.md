# ADR 0003: Локальный симулятор провайдера и Transactional Inbox

Адаптер `IPaymentProvider` (`SandboxProvider`) изолирует вызовы к платёжному провайдеру. Действия покупателя на странице checkout отправляют HMAC-подписанный callback на входящий маршрут `POST /v1/provider/sandbox/events`. Сервис `Payments` сохраняет событие в `provider_inbox` и `outbox_events` в одной транзакции до выдачи `202 Accepted`, а сверку суммы, валюты и переход статуса выполняет асинхронно через топик Kafka `dobropay.provider-events.v1`.
