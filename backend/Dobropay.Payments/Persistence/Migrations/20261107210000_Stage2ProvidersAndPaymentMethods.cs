using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dobropay.Payments.Persistence.Migrations
{
    public partial class Stage2ProvidersAndPaymentMethods : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "payment_method_type",
                table: "payments",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "sandbox");

            migrationBuilder.AddColumn<string>(
                name: "confirmation_type",
                table: "payments",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "redirect");

            migrationBuilder.AddColumn<string>(
                name: "confirmation_data",
                table: "payments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "save_payment_method",
                table: "payments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "saved_payment_method_id",
                table: "payments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "payment_method_type",
                table: "payment_attempts",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "sandbox");

            migrationBuilder.AddColumn<string>(
                name: "confirmation_payload",
                table: "payment_attempts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "sbp_qrc_id",
                table: "payment_attempts",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "sbp_operation_id",
                table: "payment_attempts",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "payer_bank_id",
                table: "payment_attempts",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "crypto_currency",
                table: "payment_attempts",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "crypto_network",
                table: "payment_attempts",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "exchange_rate",
                table: "payment_attempts",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "crypto_amount",
                table: "payment_attempts",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "deposit_address",
                table: "payment_attempts",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "quote_expires_at",
                table: "payment_attempts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "tx_hash",
                table: "payment_attempts",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "confirmations",
                table: "payment_attempts",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "required_confirmations",
                table: "payment_attempts",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "saved_payment_methods",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    shop_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    card_first6 = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: false),
                    card_last4 = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    expiry_month = table.Column<int>(type: "integer", nullable: false),
                    expiry_year = table.Column<int>(type: "integer", nullable: false),
                    card_brand = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    provider_token = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    saved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_saved_payment_methods", x => x.id);
                    table.UniqueConstraint("AK_saved_payment_methods_shop_id_id", x => new { x.shop_id, x.id });
                });

            migrationBuilder.CreateIndex(
                name: "IX_saved_payment_methods_provider_token",
                table: "saved_payment_methods",
                column: "provider_token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_saved_payment_methods_shop_id_saved_at",
                table: "saved_payment_methods",
                columns: new[] { "shop_id", "saved_at" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "saved_payment_methods");

            migrationBuilder.DropColumn(
                name: "confirmation_data",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "confirmation_type",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "payment_method_type",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "save_payment_method",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "saved_payment_method_id",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "confirmation_payload",
                table: "payment_attempts");

            migrationBuilder.DropColumn(
                name: "confirmations",
                table: "payment_attempts");

            migrationBuilder.DropColumn(
                name: "crypto_amount",
                table: "payment_attempts");

            migrationBuilder.DropColumn(
                name: "crypto_currency",
                table: "payment_attempts");

            migrationBuilder.DropColumn(
                name: "crypto_network",
                table: "payment_attempts");

            migrationBuilder.DropColumn(
                name: "deposit_address",
                table: "payment_attempts");

            migrationBuilder.DropColumn(
                name: "exchange_rate",
                table: "payment_attempts");

            migrationBuilder.DropColumn(
                name: "payer_bank_id",
                table: "payment_attempts");

            migrationBuilder.DropColumn(
                name: "payment_method_type",
                table: "payment_attempts");

            migrationBuilder.DropColumn(
                name: "quote_expires_at",
                table: "payment_attempts");

            migrationBuilder.DropColumn(
                name: "required_confirmations",
                table: "payment_attempts");

            migrationBuilder.DropColumn(
                name: "sbp_operation_id",
                table: "payment_attempts");

            migrationBuilder.DropColumn(
                name: "sbp_qrc_id",
                table: "payment_attempts");

            migrationBuilder.DropColumn(
                name: "tx_hash",
                table: "payment_attempts");
        }
    }
}
