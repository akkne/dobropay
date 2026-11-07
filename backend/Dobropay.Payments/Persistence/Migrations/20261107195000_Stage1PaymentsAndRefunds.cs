using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dobropay.Payments.Persistence.Migrations
{
    public partial class Stage1PaymentsAndRefunds : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "payments",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(16)",
                oldMaxLength: 16);

            migrationBuilder.AddColumn<bool>(
                name: "capture",
                table: "payments",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<long>(
                name: "captured_amount",
                table: "payments",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "captured_at",
                table: "payments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "refunded_amount",
                table: "payments",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "expires_at",
                table: "payments",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()");

            migrationBuilder.AddColumn<string>(
                name: "cancellation_party",
                table: "payments",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cancellation_reason",
                table: "payments",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "resource_id",
                table: "idempotency_records",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "refunds",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    shop_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<long>(type: "bigint", nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    external_refund_id = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refunds", x => x.id);
                    table.ForeignKey(
                        name: "FK_refunds_payments_shop_id_payment_id",
                        columns: x => new { x.shop_id, x.payment_id },
                        principalTable: "payments",
                        principalColumns: new[] { "shop_id", "id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_payments_status_expires_at",
                table: "payments",
                columns: new[] { "status", "expires_at" });

            migrationBuilder.CreateIndex(
                name: "IX_refunds_shop_id_created_at",
                table: "refunds",
                columns: new[] { "shop_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_refunds_shop_id_payment_id",
                table: "refunds",
                columns: new[] { "shop_id", "payment_id" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "refunds");

            migrationBuilder.DropIndex(
                name: "IX_payments_status_expires_at",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "resource_id",
                table: "idempotency_records");

            migrationBuilder.DropColumn(
                name: "cancellation_party",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "cancellation_reason",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "capture",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "captured_amount",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "captured_at",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "expires_at",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "refunded_amount",
                table: "payments");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "payments",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32);
        }
    }
}
