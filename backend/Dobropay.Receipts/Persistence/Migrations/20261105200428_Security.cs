using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dobropay.Receipts.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Security : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$ DECLARE t text; BEGIN
                  FOREACH t IN ARRAY ARRAY['receipt_inbox','demo_receipts'] LOOP
                    EXECUTE format('ALTER TABLE %I ENABLE ROW LEVEL SECURITY', t);
                    EXECUTE format('ALTER TABLE %I FORCE ROW LEVEL SECURITY', t);
                    EXECUTE format('CREATE POLICY shop_isolation ON %I USING (shop_id = NULLIF(current_setting(''app.shop_id'', true), '''')::uuid) WITH CHECK (shop_id = NULLIF(current_setting(''app.shop_id'', true), '''')::uuid)', t);
                    EXECUTE format('GRANT SELECT,INSERT,UPDATE ON %I TO receipts_runtime', t);
                  END LOOP;
                END $$;
                CREATE FUNCTION next_receipt() RETURNS TABLE(id uuid,shop_id uuid)
                  LANGUAGE sql SECURITY DEFINER SET search_path=public AS $$
                  SELECT r.id,r.shop_id FROM demo_receipts r WHERE r.email_state='pending' ORDER BY r.created_at LIMIT 1
                  $$;
                REVOKE ALL ON FUNCTION next_receipt() FROM PUBLIC;
                GRANT EXECUTE ON FUNCTION next_receipt() TO receipts_runtime;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP FUNCTION next_receipt();
                DROP POLICY shop_isolation ON receipt_inbox;
                DROP POLICY shop_isolation ON demo_receipts;
                """);
        }
    }
}
