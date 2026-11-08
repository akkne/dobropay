using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dobropay.Receipts.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Stage3FiscalReceipts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "fiscal_receipts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    shop_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    refund_id = table.Column<Guid>(type: "uuid", nullable: true),
                    receipt_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    total_amount = table.Column<long>(type: "bigint", nullable: false),
                    currency = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    tax_system_code = table.Column<int>(type: "integer", nullable: false),
                    customer_email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    customer_phone = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    fiscal_document_number = table.Column<long>(type: "bigint", nullable: true),
                    fiscal_storage_number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    fiscal_attribute = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    fiscal_sign = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    qr_payload = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    registered_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fiscal_receipts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "fiscal_receipt_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    receipt_id = table.Column<Guid>(type: "uuid", nullable: false),
                    description = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    quantity = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    amount = table.Column<long>(type: "bigint", nullable: false),
                    vat_code = table.Column<int>(type: "integer", nullable: false),
                    payment_mode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    payment_subject = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fiscal_receipt_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_fiscal_receipt_items_fiscal_receipts_receipt_id",
                        column: x => x.receipt_id,
                        principalTable: "fiscal_receipts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_fiscal_receipts_created_at",
                table: "fiscal_receipts",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_fiscal_receipts_fiscal_document_number",
                table: "fiscal_receipts",
                column: "fiscal_document_number");

            migrationBuilder.CreateIndex(
                name: "IX_fiscal_receipts_payment_id",
                table: "fiscal_receipts",
                column: "payment_id");

            migrationBuilder.CreateIndex(
                name: "IX_fiscal_receipts_payment_id_refund_id",
                table: "fiscal_receipts",
                columns: new[] { "payment_id", "refund_id" });

            migrationBuilder.CreateIndex(
                name: "IX_fiscal_receipts_shop_id",
                table: "fiscal_receipts",
                column: "shop_id");

            migrationBuilder.CreateIndex(
                name: "IX_fiscal_receipts_shop_id_created_at",
                table: "fiscal_receipts",
                columns: new[] { "shop_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_fiscal_receipt_items_receipt_id",
                table: "fiscal_receipt_items",
                column: "receipt_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "fiscal_receipt_items");

            migrationBuilder.DropTable(
                name: "fiscal_receipts");
        }
    }
}
