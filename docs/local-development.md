# Локальная разработка и проверка изоляции (DB-per-Service & RLS)

Требуются .NET SDK 10, Docker Desktop / Docker Compose, `curl` и `jq`.

## 1. Подготовка окружения и запуск

1. Скопируйте шаблон переменных окружения:
   ```bash
   cp .env.example .env
   ```
   Файл `.env` содержит все локальные пароли, строки подключения, ключи и адреса сервисов и не коммитится в Git.
2. Соберите и запустите весь контур:
   ```bash
   docker compose up -d --build
   docker compose ps
   ```

Контур поднимает:
- 5 отдельных контейнеров PostgreSQL (`auth-db`, `merchant-dev-db`, `payments-db`, `notifications-db`, `receipts-db`);
- брокер `kafka` (KRaft) и `kafka-init` (создание топиков);
- `redis` и `mailpit` (`http://localhost:8025`);
- одноразовый сервис `setup` (настройка ролей, применение EF Core миграций во всех 5 БД и создание двух демо-магазинов и тестового сотрудника);
- 7 микросервисов (`auth`, `developer-api`, `payments`, `notifications`, `email`, `receipts`, `merchant-admin`) и `gateway` (`http://localhost:8080`).

## 2. Полезные команды

```bash
dotnet build Dobropay.slnx -warnaserror
dotnet test Dobropay.slnx --no-build
docker compose logs setup payments notifications receipts
docker compose run --rm setup
docker compose down
docker compose down -v
```

## 3. Проверка RLS и ролевой изоляции под рабочей ролью

После создания платежа первого магазина (`11111111-1111-4111-8111-111111111111`) проверьте RLS непосредственно в контейнере `payments-db` под ролью `payments_runtime` (не имеющей `SUPERUSER` и `BYPASSRLS`):

```bash
docker compose exec payments-db psql -U payments_runtime -d payments_db
BEGIN;
SELECT set_config('app.shop_id', '11111111-1111-4111-8111-111111111111', true);
SELECT id, shop_id, status FROM payments;
COMMIT;
SELECT count(*) FROM payments; -- 0: транзакция завершена, контекст магазина сброшен
```

Попытка прочитать платёж первого магазина под контекстом второго (`22222222-2222-4222-8222-222222222222`) возвращает 0 строк, а попытка вставить строку с чужим `shop_id` отклоняется политикой `WITH CHECK`.
