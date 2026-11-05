using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dobropay.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ProviderInboxConflictGrant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("GRANT SELECT(provider_event_id) ON provider_inbox TO dobropay_api");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("REVOKE SELECT(provider_event_id) ON provider_inbox FROM dobropay_api");
        }
    }
}
