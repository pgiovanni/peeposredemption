using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace peeposredemption.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStatusEffects : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ability_json",
                table: "monster_definitions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "monster_status_json",
                table: "combat_sessions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "player_status_json",
                table: "combat_sessions",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ability_json",
                table: "monster_definitions");

            migrationBuilder.DropColumn(
                name: "monster_status_json",
                table: "combat_sessions");

            migrationBuilder.DropColumn(
                name: "player_status_json",
                table: "combat_sessions");
        }
    }
}
