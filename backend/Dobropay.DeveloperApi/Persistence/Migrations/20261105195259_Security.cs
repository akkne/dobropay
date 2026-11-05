using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dobropay.DeveloperApi.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Security : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE shops ENABLE ROW LEVEL SECURITY;
                ALTER TABLE shops FORCE ROW LEVEL SECURITY;
                CREATE POLICY shops_isolation ON shops USING (id = NULLIF(current_setting('app.shop_id', true), '')::uuid)
                    WITH CHECK (id = NULLIF(current_setting('app.shop_id', true), '')::uuid);
                ALTER TABLE api_credentials ENABLE ROW LEVEL SECURITY;
                ALTER TABLE api_credentials FORCE ROW LEVEL SECURITY;
                CREATE POLICY credentials_isolation ON api_credentials USING (shop_id = NULLIF(current_setting('app.shop_id', true), '')::uuid)
                    WITH CHECK (shop_id = NULLIF(current_setting('app.shop_id', true), '')::uuid);
                ALTER TABLE shop_memberships ENABLE ROW LEVEL SECURITY;
                ALTER TABLE shop_memberships FORCE ROW LEVEL SECURITY;
                CREATE POLICY memberships_isolation ON shop_memberships USING (shop_id = NULLIF(current_setting('app.shop_id', true), '')::uuid)
                    WITH CHECK (shop_id = NULLIF(current_setting('app.shop_id', true), '')::uuid);
                CREATE FUNCTION resolve_api_key(p_hash text) RETURNS TABLE(shop_id uuid, shop_name text)
                    LANGUAGE sql SECURITY DEFINER SET search_path = public AS $$
                    SELECT c.shop_id, s.name::text FROM api_credentials c JOIN shops s ON s.id=c.shop_id WHERE c.key_hash=p_hash
                    $$;
                REVOKE ALL ON FUNCTION resolve_api_key(text) FROM PUBLIC;
                GRANT EXECUTE ON FUNCTION resolve_api_key(text) TO developer_api_runtime;
                GRANT SELECT, INSERT, UPDATE ON shops, api_credentials, shop_memberships TO developer_api_runtime;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP FUNCTION resolve_api_key(text);
                DROP POLICY memberships_isolation ON shop_memberships;
                DROP POLICY credentials_isolation ON api_credentials;
                DROP POLICY shops_isolation ON shops;
                """);
        }
    }
}
