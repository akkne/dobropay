using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dobropay.DeveloperApi.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddShopOnboardingAndCredentials : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "created_at",
                table: "api_credentials",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "key_prefix",
                table: "api_credentials",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "mode",
                table: "api_credentials",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "api_credentials",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "revoked_at",
                table: "api_credentials",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "shop_onboarding_profiles",
                columns: table => new
                {
                    shop_id = table.Column<Guid>(type: "uuid", nullable: false),
                    inn = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: true),
                    ogrn = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    legal_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    bank_bic = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: true),
                    bank_account = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    website_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    verification_status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shop_onboarding_profiles", x => x.shop_id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "shop_onboarding_profiles");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "api_credentials");

            migrationBuilder.DropColumn(
                name: "key_prefix",
                table: "api_credentials");

            migrationBuilder.DropColumn(
                name: "mode",
                table: "api_credentials");

            migrationBuilder.DropColumn(
                name: "name",
                table: "api_credentials");

            migrationBuilder.DropColumn(
                name: "revoked_at",
                table: "api_credentials");
        }
    }
}
