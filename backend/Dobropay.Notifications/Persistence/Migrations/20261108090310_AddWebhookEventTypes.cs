using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dobropay.Notifications.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWebhookEventTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "event_types",
                table: "webhook_subscriptions",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "event_types",
                table: "webhook_subscriptions");
        }
    }
}
