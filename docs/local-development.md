# Локальная разработка

Требуются .NET SDK 10, Docker Desktop/Compose; для примеров README также `curl` и `jq`. Скопируйте `.env.example` в `.env`. `.env` не попадает в Git. Значения в примере служат только локальной песочнице.

`docker compose up -d --build` запускает PostgreSQL, одноразовый `setup`, API и Worker. `setup` вызывает `Database.MigrateAsync()` для code-first EF Core migrations в `src/Dobropay.Infrastructure/Migrations`, затем повторяемо создаёт два магазина: `11111111-1111-4111-8111-111111111111` и `22222222-2222-4222-8222-222222222222`. Ключи берутся из `DEMO_KEY_A/B`, в БД хранятся SHA-256 хеши. `DEMO_WEBHOOK_URL` в Compose фиксирован как `http://api:8080/local/webhooks/receiver`.

Параметры: `POSTGRES_PASSWORD`, `API_DB_PASSWORD`, `WORKER_DB_PASSWORD`, `MIGRATOR_DB_PASSWORD`, `DEMO_KEY_A`, `DEMO_KEY_B`, `CHECKOUT_SECRET`, `PROVIDER_SECRET`, `WEBHOOK_SECRET`. Настройки подключения задаются процессам через `ConnectionStrings__Migration`, `ConnectionStrings__Api`, `ConnectionStrings__Worker`; `Sandbox__Enabled` включает локальные маршруты. API валидирует критичные секреты и строку подключения при старте. В `appsettings.json` только несекретные настройки.

```bash
docker compose ps
docker compose logs setup api worker
docker compose restart api worker
docker compose down
docker compose down -v  # удаляет только локальный том PostgreSQL
```

Миграции применяются **до** старта API и Worker зависимостью Compose. Первичная локальная инициализация выполняется bootstrap-подключением `postgres`; после неё создаётся отдельная роль `dobropay_migrator` с правом DDL, а таблицы принадлежат `dobropay_owner`. Runtime-роли этими правами не обладают. Повторно применить миграции и обновить демо-данные: `docker compose run --rm setup`. Для новой модели: `dotnet ef migrations add <Name> --project src/Dobropay.Infrastructure --startup-project src/Dobropay.Infrastructure --output-dir Migrations`; миграция безопасности хранит SQL политик как встроенный ресурс `db/security.sql`.

## Проверка RLS под runtime-ролью

Пример ниже выполняется после создания платежа первого магазина; подставьте его ID. Команды выполняются именно под `dobropay_api`, без `BYPASSRLS`:

```bash
docker compose exec db psql -U dobropay_api -d dobropay
BEGIN;
SELECT set_config('app.shop_id','11111111-1111-4111-8111-111111111111',true);
SELECT id,shop_id FROM payments WHERE id='<payment-id>';
COMMIT;
SELECT count(*) FROM payments; -- 0: локальный контекст завершился
```

Для второго магазина установите `22222222-2222-4222-8222-222222222222`: тот же ID не виден. В транзакции с контекстом A `INSERT INTO payments(id,shop_id,amount,currency,status) VALUES (gen_random_uuid(),'22222222-2222-4222-8222-222222222222',100,'RUB','pending')` отклоняется политикой `WITH CHECK`; после ошибки выполните `ROLLBACK`.
