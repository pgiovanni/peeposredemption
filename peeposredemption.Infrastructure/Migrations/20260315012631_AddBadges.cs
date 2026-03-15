using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace peeposredemption.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBadges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "date_of_birth",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "badge_definitions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    icon = table.Column<string>(type: "text", nullable: false),
                    category = table.Column<int>(type: "integer", nullable: false),
                    stat_key = table.Column<string>(type: "text", nullable: false),
                    threshold = table.Column<long>(type: "bigint", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_badge_definitions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "parental_links",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    parent_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    child_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    link_code = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    account_frozen = table.Column<bool>(type: "boolean", nullable: false),
                    dm_friends_only = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_parental_links", x => x.id);
                    table.ForeignKey(
                        name: "FK_parental_links_users_child_user_id",
                        column: x => x.child_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_parental_links_users_parent_user_id",
                        column: x => x.parent_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_activity_stats",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    total_messages = table.Column<long>(type: "bigint", nullable: false),
                    longest_streak = table.Column<int>(type: "integer", nullable: false),
                    total_orbs_gifted = table.Column<long>(type: "bigint", nullable: false),
                    servers_joined = table.Column<int>(type: "integer", nullable: false),
                    peak_orb_balance = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_user_activity_stats", x => x.id);
                    table.ForeignKey(
                        name: "f_k_user_activity_stats_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_badges",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    badge_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    earned_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_displayed = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_user_badges", x => x.id);
                    table.ForeignKey(
                        name: "f_k_user_badges_badge_definitions_badge_definition_id",
                        column: x => x.badge_definition_id,
                        principalTable: "badge_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "f_k_user_badges_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_badge_definitions_stat_key_threshold",
                table: "badge_definitions",
                columns: new[] { "stat_key", "threshold" });

            migrationBuilder.CreateIndex(
                name: "IX_parental_links_child_user_id",
                table: "parental_links",
                column: "child_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_parental_links_link_code",
                table: "parental_links",
                column: "link_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_parental_links_parent_user_id",
                table: "parental_links",
                column: "parent_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_activity_stats_user_id",
                table: "user_activity_stats",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_badges_badge_definition_id",
                table: "user_badges",
                column: "badge_definition_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_badges_user_id_badge_definition_id",
                table: "user_badges",
                columns: new[] { "user_id", "badge_definition_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "parental_links");

            migrationBuilder.DropTable(
                name: "user_activity_stats");

            migrationBuilder.DropTable(
                name: "user_badges");

            migrationBuilder.DropTable(
                name: "badge_definitions");

            migrationBuilder.DropColumn(
                name: "date_of_birth",
                table: "users");
        }
    }
}
