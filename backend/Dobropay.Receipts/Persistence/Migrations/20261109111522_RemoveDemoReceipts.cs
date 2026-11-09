using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dobropay.Receipts.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDemoReceipts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "demo_receipts");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "demo_receipts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    currency = table.Column<string>(type: "text", nullable: false),
                    email_state = table.Column<string>(type: "text", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    shop_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_demo_receipts", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_demo_receipts_event_id",
                table: "demo_receipts",
                column: "event_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_demo_receipts_shop_id_payment_id",
                table: "demo_receipts",
                columns: new[] { "shop_id", "payment_id" });
        }
    }
}
