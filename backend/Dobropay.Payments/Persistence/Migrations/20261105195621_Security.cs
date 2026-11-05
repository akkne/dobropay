using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dobropay.Payments.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Security : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$ DECLARE t text; BEGIN
                  FOREACH t IN ARRAY ARRAY['payments','checkout_links','payment_attempts','idempotency_records','provider_inbox','outbox_events'] LOOP
                    EXECUTE format('ALTER TABLE %I ENABLE ROW LEVEL SECURITY', t);
                    EXECUTE format('ALTER TABLE %I FORCE ROW LEVEL SECURITY', t);
                    EXECUTE format('CREATE POLICY shop_isolation ON %I USING (shop_id = NULLIF(current_setting(''app.shop_id'', true), '''')::uuid) WITH CHECK (shop_id = NULLIF(current_setting(''app.shop_id'', true), '''')::uuid)', t);
                    EXECUTE format('GRANT SELECT,INSERT,UPDATE ON %I TO payments_runtime', t);
                  END LOOP;
                END $$;
                CREATE FUNCTION lookup_checkout(p_hash text) RETURNS TABLE(shop_id uuid,payment_id uuid,expires_at timestamptz)
                  LANGUAGE sql SECURITY DEFINER SET search_path=public AS $$
                  SELECT c.shop_id,c.payment_id,c.expires_at FROM checkout_links c WHERE c.token_hash=p_hash
                  $$;
                CREATE FUNCTION lookup_attempt(p_external text) RETURNS TABLE(shop_id uuid,payment_id uuid,attempt_id uuid,amount bigint,currency text)
                  LANGUAGE sql SECURITY DEFINER SET search_path=public AS $$
                  SELECT a.shop_id,a.payment_id,a.id,p.amount,p.currency::text FROM payment_attempts a
                  JOIN payments p ON p.shop_id=a.shop_id AND p.id=a.payment_id WHERE a.external_id=p_external
                  $$;
                CREATE FUNCTION lookup_attempt_id(p_id uuid) RETURNS TABLE(shop_id uuid,payment_id uuid)
                  LANGUAGE sql SECURITY DEFINER SET search_path=public AS $$
                  SELECT a.shop_id,a.payment_id FROM payment_attempts a WHERE a.id=p_id
                  $$;
                CREATE FUNCTION next_outbox() RETURNS TABLE(id uuid,shop_id uuid)
                  LANGUAGE sql SECURITY DEFINER SET search_path=public AS $$
                  SELECT e.id,e.shop_id FROM outbox_events e WHERE NOT e.published ORDER BY e.occurred_at LIMIT 1
                  $$;
                REVOKE ALL ON FUNCTION lookup_checkout(text),lookup_attempt(text),lookup_attempt_id(uuid),next_outbox() FROM PUBLIC;
                GRANT EXECUTE ON FUNCTION lookup_checkout(text),lookup_attempt(text),lookup_attempt_id(uuid),next_outbox() TO payments_runtime;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP FUNCTION lookup_checkout(text),lookup_attempt(text),lookup_attempt_id(uuid),next_outbox();
                DO $$ DECLARE t text; BEGIN
                  FOREACH t IN ARRAY ARRAY['payments','checkout_links','payment_attempts','idempotency_records','provider_inbox','outbox_events'] LOOP
                    EXECUTE format('DROP POLICY shop_isolation ON %I', t);
                  END LOOP;
                END $$;
                """);
        }
    }
}
