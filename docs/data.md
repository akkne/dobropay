# Данные и владение БД (Database-per-Service & EF Core)

В Dobropay реализован строгий паттерн **Database-per-Service**: каждый сервис, владеющий данными, подключается к своему собственному физически отдельному контейнеру PostgreSQL исключительно через **Entity Framework Core** (без использования raw SQL). Прямые обращения между базами данных разных сервисов запрещены.

## 1. Изолированные базы данных и владельцы

| Сервис-владелец | Контейнер БД | Имя БД | `DbContext` | Сущности (таблицы) | Миграции |
| --- | --- | --- | --- | --- | --- |
| `Dobropay.Auth` | `auth-db` | `auth_db` | `AuthDbContext` | `Account` (`accounts`), `StaffSession` (`staff_sessions`) | `backend/Dobropay.Auth/Persistence/Migrations` |
| `Dobropay.DeveloperApi` | `merchant-dev-db` | `merchant_dev_db` | `MerchantDbContext` | `Shop` (`shops`), `ApiCredential` (`api_credentials`), `ShopMembership` (`shop_memberships`) | `backend/Dobropay.DeveloperApi/Persistence/Migrations` |
| `Dobropay.Payments` | `payments-db` | `payments_db` | `PaymentsDbContext` | `Payment` (`payments`), `CheckoutLink` (`checkout_links`), `PaymentAttempt` (`payment_attempts`), `IdempotencyRecord` (`idempotency_records`), `ProviderInbox` (`provider_inbox`), `OutboxEvent` (`outbox_events`) | `backend/Dobropay.Payments/Persistence/Migrations` |
| `Dobropay.Notifications` | `notifications-db` | `notifications_db` | `NotificationsDbContext` | `WebhookSubscription` (`webhook_subscriptions`), `NotificationInbox` (`notification_inbox`), `WebhookDelivery` (`webhook_deliveries`), `WebhookDeliveryAttempt` (`webhook_delivery_attempts`), `LocalReceiverEvent` (`local_receiver_events`) | `backend/Dobropay.Notifications/Persistence/Migrations` |
| `Dobropay.Receipts` | `receipts-db` | `receipts_db` | `ReceiptsDbContext` | `ReceiptInbox` (`receipt_inbox`), `DemoReceipt` (`demo_receipts`) | `backend/Dobropay.Receipts/Persistence/Migrations` |

Дополнительно сервис `Dobropay.Notifications` использует **Redis** (`redis:6379`) через абстракции `IRedisConnectionProvider` и `IRateLimiter` для краткоживущих ключей TTL-лимитирования.

## 2. Доступ к данным только через EF Core и ООП-абстракции

1. **Никакого Raw SQL**:
   - В коде проекта полностью запрещены `NpgsqlCommand`, `ExecuteSqlRaw` и строковые SQL-запросы.
   - Схема каждой базы данных описывается строго через Fluent API (`IEntityTypeConfiguration<T>`) и Code-First миграции EF Core.
2. **Сначала абстракция, потом реализация**:
   - Каждый сервис определяет интерфейс единицы работы (`IUnitOfWork` / `IDatabaseHealthChecker`) и типизированные интерфейсы репозиториев, а затем реализует их поверх своего `DbContext`.
   - Конфигурация подключения передаётся из переменных окружения (`.env`) в типизированный класс `DatabaseOptions` и регистрируется через метод расширения DI.
3. **Один класс — один файл**:
   - Каждая сущность БД, каждая конфигурация `IEntityTypeConfiguration<T>`, каждый `DbContext`, каждый интерфейс и класс реализации находятся в отдельных собственных файлах.
