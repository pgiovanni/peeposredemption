using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace peeposredemption.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGuildConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "enchant_bonus",
                table: "player_inventory_items");

            migrationBuilder.DropColumn(
                name: "enchant_element",
                table: "player_inventory_items");

            migrationBuilder.RenameColumn(
                name: "enchant_name",
                table: "player_inventory_items",
                newName: "enchants_json");

            migrationBuilder.AddColumn<int>(
                name: "enchant_drop_element",
                table: "monster_definitions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "enchanted_drop_chance",
                table: "monster_definitions",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<int>(
                name: "enchant_tier",
                table: "item_definitions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "guild_configs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    guild_id = table.Column<string>(type: "text", nullable: false),
                    status_channel_id = table.Column<string>(type: "text", nullable: true),
                    loot_drop_channel_id = table.Column<string>(type: "text", nullable: true),
                    rpg_channel_id = table.Column<string>(type: "text", nullable: true),
                    suggestions_channel_id = table.Column<string>(type: "text", nullable: true),
                    welcome_channel_id = table.Column<string>(type: "text", nullable: true),
                    mod_log_channel_id = table.Column<string>(type: "text", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_guild_configs", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_guild_configs_guild_id",
                table: "guild_configs",
                column: "guild_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "guild_configs");

            migrationBuilder.DropColumn(
                name: "enchant_drop_element",
                table: "monster_definitions");

            migrationBuilder.DropColumn(
                name: "enchanted_drop_chance",
                table: "monster_definitions");

            migrationBuilder.DropColumn(
                name: "enchant_tier",
                table: "item_definitions");

            migrationBuilder.RenameColumn(
                name: "enchants_json",
                table: "player_inventory_items",
                newName: "enchant_name");

            migrationBuilder.AddColumn<int>(
                name: "enchant_bonus",
                table: "player_inventory_items",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "enchant_element",
                table: "player_inventory_items",
                type: "integer",
                nullable: true);
        }
    }
}
