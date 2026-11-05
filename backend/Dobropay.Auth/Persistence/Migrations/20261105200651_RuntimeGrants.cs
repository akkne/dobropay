using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dobropay.Auth.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RuntimeGrants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("GRANT SELECT ON accounts TO auth_runtime; GRANT SELECT,INSERT ON staff_sessions TO auth_runtime;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("REVOKE ALL ON accounts,staff_sessions FROM auth_runtime;");
        }
    }
}
