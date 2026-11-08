# Документация Dobropay

Перед любой разработкой или правкой репозитория агент обязан прочитать [AGENTS.md](../AGENTS.md), [корневой README](../README.md) и [стандарты кода (Code Style)](code-style.md). Весь серверный код располагается в каталоге `backend/`.

| Документ | Назначение |
| --- | --- |
| [Правила для агентов (AGENTS.md)](../AGENTS.md) | Точка входа для каждого агента: ссылки на документацию, Code Style, Git-процесс и архитектуру DB-per-Service. |
| [Code Style](code-style.md) | Строгие правила чистого кода: ООП, сначала абстракция потом реализация, один класс — один файл, запрет магических строк, конфигурация в `.env`. |
| [Git-процесс](git-process.md) | Обязательные правила веток, коммитов, push, проверок и чистоты репозитория. |
| [Архитектура и анализ ЮKassa](architecture.md) | Семь микросервисов, паттерн Database-per-Service, диаграммы связей и проектирование целевых фичей по модели ЮKassa (Hold/Capture, возвраты, СБП/Карты/Крипто, 54-ФЗ, Webhooks). |
| [Архитектурное задание](architecture/architecture.md) | Техническое задание на микросервисы, пять изолированных БД, Kafka, Redis и критерии приёмки. |
| [Схема работы API, Kafka и Webhook](api-kafka-webhooks-flow.md) | Полная сквозная схема взаимодействия API (публичных и внутренних), топиков Kafka, Outbox/Inbox, идемпотентности, Redis rate-limiting и подписей Webhook. |
| [Инфраструктура](infrastructure.md) | Контейнеры Docker Compose (5 PostgreSQL БД, Kafka, Redis, Mailpit, Gateway), порты, тома и переменные `.env`. |
| [Данные и RLS](data.md) | Пять отдельных баз данных, владельцы, таблицы, миграции, роли и многоарендная изоляция PostgreSQL RLS. |
| [События и Kafka](events.md) | Топики, consumer groups, схема конверта `EventEnvelope`, Transactional Outbox/Inbox и DLQ. |
| [Контракт API](api.md) | Публичные и внутренние маршруты Auth, Developer API, Payments, Checkout, Merchant Admin и исходящих webhook. |
| [Локальная разработка](local-development.md) | Запуск контура, миграции и проверка RLS под рабочими ролями в изолированных контейнерах БД. |
| [Runbook](runbook.md) | Практическая диагностика платежей, Kafka lag, сбоев Redis, повторов webhook и писем в Mailpit. |
| [Журнал промптов](prompt-log.md) | Хронология запросов по проекту по файлу `ai-requests.json`; записи сокращены и частично реконструированы, это не дословная стенограмма. |
| [Общий Roadmap](roadmap.md) | Сводная дорожная карта проекта и синхронизация этапов Backend и Frontend. |
| [Roadmap Backend](roadmap-backend.md) | Детальная декомпозиция задач бэкенда по микросервисам, БД (EF Core), абстракциям и контрактам. |
| [Roadmap Frontend](roadmap-frontend.md) | Детальная декомпозиция задач фронтенда по макету `frontend/docs/DESIGN.md`, Checkout и личному кабинету. |
| [ADR](adr/) | Архитектурные решения (DB-per-Service, RLS, симулятор провайдера, Kafka и доставка webhook). |
