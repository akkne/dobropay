# Эксплуатационный runbook

Сначала проверьте `docker compose ps` и `docker compose logs setup payments notifications receipts email`. Готовность `/health/ready` возвращает `503`, пока реальный платёжный адаптер отсутствует и `SANDBOX_ENABLED=false`.

Пять PostgreSQL-контейнеров принадлежат разным сервисам. Проверяйте логи и метрики каждого сервиса отдельно; не подключайте один сервис к БД другого. В `payments-db` важны `payments`, `payment_attempts`, `provider_inbox` и `outbox_events`; в `notifications-db` — `webhook_deliveries` и `webhook_delivery_attempts`; в `receipts-db` — `receipt_inbox` и `fiscal_receipts`.

При задержке доставки событий проверьте Kafka и группы потребителей:

```bash
docker compose exec kafka /opt/kafka/bin/kafka-consumer-groups.sh --bootstrap-server kafka:19092 --describe --all-groups
```

При ошибках webhook проверьте доступность Redis, состояние подписки и `last_result` в доставке. При ошибках чеков проверьте ответ внешней кассы, `fiscal_receipts.status` и логи `email`/внешнего SMTP. Повторную фискализацию и рассылку следует выполнять только после проверки исхода у внешнего оператора, чтобы не создать дубликат документа.
