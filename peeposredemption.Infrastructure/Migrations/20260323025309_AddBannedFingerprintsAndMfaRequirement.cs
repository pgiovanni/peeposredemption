using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace peeposredemption.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBannedFingerprintsAndMfaRequirement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "reported_message_id",
                table: "support_tickets",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "reported_user_id",
                table: "support_tickets",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "require_mfa_for_moderators",
                table: "servers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "banned_fingerprints",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    fingerprint_hash = table.Column<string>(type: "text", nullable: false),
                    banned_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_banned_fingerprints", x => x.id);
                    table.ForeignKey(
                        name: "f_k_banned_fingerprints__users_banned_by_id",
                        column: x => x.banned_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_banned_fingerprints_banned_by_user_id",
                table: "banned_fingerprints",
                column: "banned_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_banned_fingerprints_fingerprint_hash",
                table: "banned_fingerprints",
                column: "fingerprint_hash");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "banned_fingerprints");

            migrationBuilder.DropColumn(
                name: "reported_message_id",
                table: "support_tickets");

            migrationBuilder.DropColumn(
                name: "reported_user_id",
                table: "support_tickets");

            migrationBuilder.DropColumn(
                name: "require_mfa_for_moderators",
                table: "servers");
        }
    }
}
