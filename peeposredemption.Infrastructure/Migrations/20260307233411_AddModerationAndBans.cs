using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace peeposredemption.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddModerationAndBans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "role",
                table: "server_members",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "messages",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "banned_members",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    server_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    banned_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    banned_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_banned_members", x => x.id);
                    table.ForeignKey(
                        name: "f_k_banned_members__servers_server_id",
                        column: x => x.server_id,
                        principalTable: "servers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "f_k_banned_members__users_user_id",
                        column: x => x.banned_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "f_k_banned_members__users_user_id1",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "moderation_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    server_id = table.Column<Guid>(type: "uuid", nullable: false),
                    moderator_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<int>(type: "integer", nullable: false),
                    target_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_message_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reason = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_moderation_logs", x => x.id);
                    table.ForeignKey(
                        name: "f_k_moderation_logs__servers_server_id",
                        column: x => x.server_id,
                        principalTable: "servers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "f_k_moderation_logs__users_moderator_id",
                        column: x => x.moderator_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "f_k_moderation_logs__users_target_user_id",
                        column: x => x.target_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_banned_members_banned_by_user_id",
                table: "banned_members",
                column: "banned_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_banned_members_server_id",
                table: "banned_members",
                column: "server_id");

            migrationBuilder.CreateIndex(
                name: "IX_banned_members_user_id",
                table: "banned_members",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_moderation_logs_moderator_id",
                table: "moderation_logs",
                column: "moderator_id");

            migrationBuilder.CreateIndex(
                name: "IX_moderation_logs_server_id",
                table: "moderation_logs",
                column: "server_id");

            migrationBuilder.CreateIndex(
                name: "IX_moderation_logs_target_user_id",
                table: "moderation_logs",
                column: "target_user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "banned_members");

            migrationBuilder.DropTable(
                name: "moderation_logs");

            migrationBuilder.DropColumn(
                name: "role",
                table: "server_members");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "messages");
        }
    }
}
