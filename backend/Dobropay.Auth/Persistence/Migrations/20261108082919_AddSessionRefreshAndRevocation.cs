using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dobropay.Auth.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionRefreshAndRevocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_revoked",
                table: "staff_sessions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "refresh_expires_at",
                table: "staff_sessions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "refresh_token_hash",
                table: "staff_sessions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_staff_sessions_refresh_token_hash",
                table: "staff_sessions",
                column: "refresh_token_hash");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_staff_sessions_refresh_token_hash",
                table: "staff_sessions");

            migrationBuilder.DropColumn(
                name: "is_revoked",
                table: "staff_sessions");

            migrationBuilder.DropColumn(
                name: "refresh_expires_at",
                table: "staff_sessions");

            migrationBuilder.DropColumn(
                name: "refresh_token_hash",
                table: "staff_sessions");
        }
    }
}
