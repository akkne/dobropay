using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dobropay.Payments.Persistence.Migrations
{
    public partial class Stage3PaymentReceiptPayloads : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "payment_receipt_payloads",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    shop_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    refund_id = table.Column<Guid>(type: "uuid", nullable: true),
                    receipt_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    tax_system_code = table.Column<int>(type: "integer", nullable: false),
                    customer_email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    customer_phone = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    items_json = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_receipt_payloads", x => x.id);
                    table.ForeignKey(
                        name: "FK_payment_receipt_payloads_payments_shop_id_payment_id",
                        columns: x => new { x.shop_id, x.payment_id },
                        principalTable: "payments",
                        principalColumns: new[] { "shop_id", "id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_payment_receipt_payloads_shop_id_payment_id_refund_id",
                table: "payment_receipt_payloads",
                columns: new[] { "shop_id", "payment_id", "refund_id" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "payment_receipt_payloads");
        }
    }
}
