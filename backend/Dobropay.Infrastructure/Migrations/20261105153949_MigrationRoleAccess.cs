using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dobropay.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MigrationRoleAccess : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("GRANT CONNECT ON DATABASE dobropay TO dobropay_migrator");
            migrationBuilder.Sql("GRANT USAGE,CREATE ON SCHEMA public TO dobropay_migrator");
            migrationBuilder.Sql("ALTER TABLE \"__EFMigrationsHistory\" OWNER TO dobropay_owner");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE \"__EFMigrationsHistory\" OWNER TO postgres");
            migrationBuilder.Sql("REVOKE CREATE ON SCHEMA public FROM dobropay_migrator");
        }
    }
}
