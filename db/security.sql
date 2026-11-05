-- DDL adjunct for the EF Core code-first migration: roles, grants, RLS, and narrow lookups.
DO $$ BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname='dobropay_owner') THEN CREATE ROLE dobropay_owner NOLOGIN; END IF;
  IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname='dobropay_api') THEN CREATE ROLE dobropay_api LOGIN NOSUPERUSER NOBYPASSRLS; END IF;
  IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname='dobropay_worker') THEN CREATE ROLE dobropay_worker LOGIN NOSUPERUSER NOBYPASSRLS; END IF;
  IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname='dobropay_migrator') THEN CREATE ROLE dobropay_migrator LOGIN NOSUPERUSER NOBYPASSRLS; END IF;
END $$;
GRANT dobropay_owner TO dobropay_migrator;
GRANT CONNECT ON DATABASE dobropay TO dobropay_api,dobropay_worker;
GRANT USAGE ON SCHEMA public TO dobropay_api,dobropay_worker;

-- Created as bootstrap postgres and intentionally limited to exact hashes.
CREATE OR REPLACE FUNCTION lookup_shop(key_digest text) RETURNS uuid LANGUAGE sql SECURITY DEFINER SET search_path=public AS $$
  SELECT shop_id FROM api_credentials WHERE key_hash=key_digest LIMIT 1
$$;
CREATE OR REPLACE FUNCTION lookup_checkout(token_digest text) RETURNS TABLE(shop_id uuid,payment_id uuid,expires_at timestamptz)
LANGUAGE sql SECURITY DEFINER SET search_path=public AS $$
  SELECT c.shop_id,c.payment_id,c.expires_at FROM checkout_links c WHERE c.token_hash=token_digest LIMIT 1
$$;
REVOKE ALL ON FUNCTION lookup_shop(text),lookup_checkout(text) FROM PUBLIC;
GRANT EXECUTE ON FUNCTION lookup_shop(text),lookup_checkout(text) TO dobropay_api;

DO $$ DECLARE t text; BEGIN
  FOREACH t IN ARRAY ARRAY['shops','api_credentials','payments','checkout_links','idempotency_records','payment_attempts','provider_inbox','outbox_events','webhook_subscriptions','webhook_deliveries','webhook_delivery_attempts'] LOOP
    EXECUTE format('ALTER TABLE %I OWNER TO dobropay_owner',t);
    EXECUTE format('ALTER TABLE %I ENABLE ROW LEVEL SECURITY',t);
    EXECUTE format('ALTER TABLE %I FORCE ROW LEVEL SECURITY',t);
    EXECUTE format('CREATE POLICY tenant_access ON %I FOR ALL TO dobropay_api,dobropay_worker USING (%s) WITH CHECK (%s)',t,
      CASE WHEN t='shops' THEN 'id = NULLIF(current_setting(''app.shop_id'',true),'''')::uuid'
      ELSE 'shop_id = NULLIF(current_setting(''app.shop_id'',true),'''')::uuid' END,
      CASE WHEN t='shops' THEN 'id = NULLIF(current_setting(''app.shop_id'',true),'''')::uuid'
      ELSE 'shop_id = NULLIF(current_setting(''app.shop_id'',true),'''')::uuid' END);
  END LOOP;
END $$;
CREATE POLICY worker_queue_inbox ON provider_inbox FOR SELECT TO dobropay_worker USING (true);
CREATE POLICY worker_queue_outbox ON outbox_events FOR SELECT TO dobropay_worker USING (true);
CREATE POLICY worker_queue_deliveries ON webhook_deliveries FOR SELECT TO dobropay_worker USING (true);

GRANT SELECT ON shops,payments,payment_attempts,idempotency_records TO dobropay_api;
GRANT INSERT ON payments,payment_attempts,idempotency_records,checkout_links,provider_inbox TO dobropay_api;
GRANT UPDATE ON payment_attempts TO dobropay_api;
GRANT SELECT ON payments,payment_attempts,provider_inbox,outbox_events,webhook_subscriptions,webhook_deliveries TO dobropay_worker;
GRANT UPDATE ON payments,payment_attempts,provider_inbox,outbox_events,webhook_deliveries TO dobropay_worker;
GRANT INSERT ON outbox_events,webhook_deliveries,webhook_delivery_attempts TO dobropay_worker;
GRANT SELECT,INSERT,UPDATE ON local_receiver_events TO dobropay_api;
