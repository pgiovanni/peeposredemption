using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace peeposredemption.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEnchantingSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // PlayerInventoryItem — replace 3 single-enchant columns with stackable EnchantsJson
            migrationBuilder.DropColumn(name: "enchant_element", table: "player_inventory_items");
            migrationBuilder.DropColumn(name: "enchant_bonus",   table: "player_inventory_items");
            migrationBuilder.DropColumn(name: "enchant_name",    table: "player_inventory_items");

            migrationBuilder.AddColumn<string>(
                name: "enchants_json",
                table: "player_inventory_items",
                type: "text",
                nullable: true);

            // ItemDefinition — enchant book tier (0 = not an enchant book)
            migrationBuilder.AddColumn<int>(
                name: "enchant_tier",
                table: "item_definitions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // MonsterDefinition — enchanted drop fields
            migrationBuilder.AddColumn<float>(
                name: "enchanted_drop_chance",
                table: "monster_definitions",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<int>(
                name: "enchant_drop_element",
                table: "monster_definitions",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "enchants_json",         table: "player_inventory_items");
            migrationBuilder.DropColumn(name: "enchant_tier",          table: "item_definitions");
            migrationBuilder.DropColumn(name: "enchanted_drop_chance", table: "monster_definitions");
            migrationBuilder.DropColumn(name: "enchant_drop_element",  table: "monster_definitions");

            migrationBuilder.AddColumn<int>(
                name: "enchant_element",
                table: "player_inventory_items",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "enchant_bonus",
                table: "player_inventory_items",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "enchant_name",
                table: "player_inventory_items",
                type: "text",
                nullable: true);
        }
    }
}
