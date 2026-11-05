using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dobropay.Notifications.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Security : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$ DECLARE t text; BEGIN
                  FOREACH t IN ARRAY ARRAY['webhook_subscriptions','notification_inbox','webhook_deliveries','webhook_delivery_attempts'] LOOP
                    EXECUTE format('ALTER TABLE %I ENABLE ROW LEVEL SECURITY', t);
                    EXECUTE format('ALTER TABLE %I FORCE ROW LEVEL SECURITY', t);
                    EXECUTE format('CREATE POLICY shop_isolation ON %I USING (shop_id = NULLIF(current_setting(''app.shop_id'', true), '''')::uuid) WITH CHECK (shop_id = NULLIF(current_setting(''app.shop_id'', true), '''')::uuid)', t);
                    EXECUTE format('GRANT SELECT,INSERT,UPDATE ON %I TO notifications_runtime', t);
                  END LOOP;
                END $$;
                GRANT SELECT,INSERT,UPDATE ON local_receiver_events TO notifications_runtime;
                CREATE FUNCTION next_delivery() RETURNS TABLE(id uuid,shop_id uuid)
                  LANGUAGE sql SECURITY DEFINER SET search_path=public AS $$
                  SELECT d.id,d.shop_id FROM webhook_deliveries d
                  WHERE (d.state='pending' AND d.next_at<=now()) OR (d.state='processing' AND d.locked_until<now())
                  ORDER BY d.next_at LIMIT 1
                  $$;
                REVOKE ALL ON FUNCTION next_delivery() FROM PUBLIC;
                GRANT EXECUTE ON FUNCTION next_delivery() TO notifications_runtime;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP FUNCTION next_delivery();
                DO $$ DECLARE t text; BEGIN
                  FOREACH t IN ARRAY ARRAY['webhook_subscriptions','notification_inbox','webhook_deliveries','webhook_delivery_attempts'] LOOP
                    EXECUTE format('DROP POLICY shop_isolation ON %I', t);
                  END LOOP;
                END $$;
                """);
        }
    }
}
