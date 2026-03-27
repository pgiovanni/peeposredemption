using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace peeposredemption.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddServerSortOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "sort_order",
                table: "server_members",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Backfill: assign sort_order based on join date per user
            migrationBuilder.Sql(@"
                UPDATE server_members sm
                SET sort_order = sub.rn
                FROM (
                    SELECT user_id, server_id,
                           ROW_NUMBER() OVER (PARTITION BY user_id ORDER BY joined_at) - 1 AS rn
                    FROM server_members
                ) sub
                WHERE sm.user_id = sub.user_id AND sm.server_id = sub.server_id;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "sort_order",
                table: "server_members");
        }
    }
}
