# Данные, владение БД и Row Level Security (RLS)

В Dobropay реализован строгий паттерн **Database-per-Service**: каждый сервис, владеющий данными, подключается к своему собственному физически отдельному контейнеру PostgreSQL. Прямые SQL-запросы между сервисами невозможны как архитектурно, так и на уровне сетевых и ролевых доступов.

## 1. Изолированные базы данных и владельцы

| Сервис-владелец | Контейнер БД | Имя БД | Таблицы | Миграции |
| --- | --- | --- | --- | --- |
| `Dobropay.Auth` | `auth-db` | `auth_db` | `accounts`, `staff_sessions` | `backend/Dobropay.Auth/Persistence/Migrations` |
| `Dobropay.DeveloperApi` | `merchant-dev-db` | `merchant_dev_db` | `shops`, `api_credentials`, `shop_memberships` | `backend/Dobropay.DeveloperApi/Persistence/Migrations` |
| `Dobropay.Payments` | `payments-db` | `payments_db` | `payments`, `checkout_links`, `payment_attempts`, `idempotency_records`, `provider_inbox`, `outbox_events` | `backend/Dobropay.Payments/Persistence/Migrations` |
| `Dobropay.Notifications` | `notifications-db` | `notifications_db` | `webhook_subscriptions`, `notification_inbox`, `webhook_deliveries`, `webhook_delivery_attempts`, `local_receiver_events` | `backend/Dobropay.Notifications/Persistence/Migrations` |
| `Dobropay.Receipts` | `receipts-db` | `receipts_db` | `receipt_inbox`, `demo_receipts` | `backend/Dobropay.Receipts/Persistence/Migrations` |

Дополнительно сервис `Dobropay.Notifications` использует **Redis** (`redis:6379`) исключительно для краткоживущих ключей TTL-лимитирования (`webhook-limit:<shop_id>`). При перезапуске или недоступности Redis все очереди и история доставок сохраняются в `notifications-db`.

## 2. Ролевая модель PostgreSQL

В каждом из пяти контейнеров PostgreSQL сервис инициализации `Dobropay.Setup` создаёт три специализированные роли:
1. `<service>_owner` (`NOLOGIN`) — владелец таблиц и `SECURITY DEFINER`-функций.
2. `<service>_migrator` (`LOGIN`, состоит в `<service>_owner`) — используется для применения DDL-миграций.
3. `<service>_runtime` (`LOGIN`, без `SUPERUSER`, без `BYPASSRLS`) — рабочая роль, под которой работает сам микросервис в runtime. Имеет минимальные права `SELECT, INSERT, UPDATE` на таблицы своей БД и `EXECUTE` на узкие функции поиска.

Право `CONNECT` на базу данных и `CREATE` в схеме `public` отозвано у `PUBLIC`.

## 3. Многоарендная изоляция через Row Level Security (RLS)

Для всех таблиц, хранящих данные магазинов (`merchant_dev_db`, `payments_db`, `notifications_db`, `receipts_db`), включены:
- `ENABLE ROW LEVEL SECURITY`
- `FORCE ROW LEVEL SECURITY`

Политика изоляции (`shop_isolation`) в `USING` и `WITH CHECK` проверяет условие:
```sql
shop_id = NULLIF(current_setting('app.shop_id', true), '')::uuid
```
(для таблицы `shops` в `merchant_dev_db` проверяется `id = NULLIF(current_setting('app.shop_id', true), '')::uuid`).

### Как сервисы работают с RLS в коде
- Каждый запрос к данным магазина открывает транзакцию через абстракцию `IDbSessionFactory` / `IDbContextTransaction` и первым действием вызывает параметризованный `SELECT set_config('app.shop_id', $1, true)` с локальной областью действия (`is_local = true`).
- По завершении транзакции (`COMMIT` или `ROLLBACK`) значение `app.shop_id` автоматически сбрасывается. Любой запрос без установленного контекста на том же соединении возвращает 0 строк.
- Для первичного разрешения контекста (по хешу API-ключа, по хешу checkout-токена, по ID попытки провайдера или при выборе следующего задания в фоновом воркере) используются узкие `SECURITY DEFINER` SQL-функции (`resolve_api_key`, `lookup_checkout`, `lookup_attempt_id`, `next_outbox`, `next_delivery`, `next_receipt`), возвращающие только идентификаторы (`id`, `shop_id`). После получения `shop_id` вся дальнейшая работа с бизнес-строками выполняется строго под контекстом найденного магазина.

## 4. Ссылочная целостность и идемпотентность

- В `payments_db` таблица `payments` имеет альтернативный ключ `(shop_id, id)`. Таблицы `checkout_links`, `payment_attempts` и `idempotency_records` связаны с `payments` по составному внешнему ключу `(shop_id, payment_id)`, что исключает перекрёстные ссылки между магазинами.
- Идемпотентность создания платежей обеспечивается первичным ключом `(shop_id, operation, idempotency_key)` в `idempotency_records`.

## 5. Резервное копирование и восстановление

Для каждого контейнера БД в локальной среде можно снять и восстановить дамп независимо:
```bash
docker compose exec payments-db pg_dump -U postgres -Fc payments_db > payments_db.dump
docker compose exec -T payments-db pg_restore -U postgres -d payments_db --clean < payments_db.dump
```
