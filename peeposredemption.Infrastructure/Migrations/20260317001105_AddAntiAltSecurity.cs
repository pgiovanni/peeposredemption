using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace peeposredemption.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAntiAltSecurity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_suspicious",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "ip_bans",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    ip_address = table.Column<string>(type: "text", nullable: false),
                    reason = table.Column<string>(type: "text", nullable: true),
                    banned_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_ip_bans", x => x.id);
                    table.ForeignKey(
                        name: "f_k_ip_bans__users_banned_by_id",
                        column: x => x.banned_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_devices",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    device_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    first_seen_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_seen_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_banned = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_user_devices", x => x.id);
                    table.ForeignKey(
                        name: "f_k_user_devices_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_fingerprints",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fingerprint_hash = table.Column<string>(type: "text", nullable: false),
                    raw_components = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_user_fingerprints", x => x.id);
                    table.ForeignKey(
                        name: "f_k_user_fingerprints_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_ip_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ip_address = table.Column<string>(type: "text", nullable: false),
                    is_vpn = table.Column<bool>(type: "boolean", nullable: false),
                    is_tor = table.Column<bool>(type: "boolean", nullable: false),
                    seen_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_user_ip_logs", x => x.id);
                    table.ForeignKey(
                        name: "f_k_user_ip_logs_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ip_bans_banned_by_user_id",
                table: "ip_bans",
                column: "banned_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_ip_bans_ip_address",
                table: "ip_bans",
                column: "ip_address",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_devices_device_id",
                table: "user_devices",
                column: "device_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_devices_device_id_user_id",
                table: "user_devices",
                columns: new[] { "device_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_devices_user_id",
                table: "user_devices",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_fingerprints_fingerprint_hash",
                table: "user_fingerprints",
                column: "fingerprint_hash");

            migrationBuilder.CreateIndex(
                name: "IX_user_fingerprints_user_id",
                table: "user_fingerprints",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_ip_logs_ip_address",
                table: "user_ip_logs",
                column: "ip_address");

            migrationBuilder.CreateIndex(
                name: "IX_user_ip_logs_user_id_seen_at",
                table: "user_ip_logs",
                columns: new[] { "user_id", "seen_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ip_bans");

            migrationBuilder.DropTable(
                name: "user_devices");

            migrationBuilder.DropTable(
                name: "user_fingerprints");

            migrationBuilder.DropTable(
                name: "user_ip_logs");

            migrationBuilder.DropColumn(
                name: "is_suspicious",
                table: "users");
        }
    }
}
