# Локальная разработка

Для сборки нужны .NET 10 SDK и Node.js. Запуск Compose также требует Docker и заполненный `.env` по [.env.example](../.env.example). Шаблон не содержит готовых паролей или тестовых аккаунтов. `setup` применяет миграции пяти баз, включая удаление устаревшей таблицы `demo_receipts`.

```bash
dotnet build Dobropay.slnx -warnaserror
dotnet test Dobropay.slnx --no-build
npm run build --prefix frontend
docker compose config --quiet
docker compose up -d --build
```

Перед миграцией существующей `receipts_db` сделайте резервную копию: `RemoveDemoReceipts` удаляет демонстрационные квитанции. Регистрация первого сотрудника доступна через кабинет или `POST /v1/auth/register`; магазин создаётся после входа. Платежи не принимаются при `SANDBOX_ENABLED=false`, пока не подключён реальный провайдер. Состояние подготовки описано в [production-readiness.md](production-readiness.md).
