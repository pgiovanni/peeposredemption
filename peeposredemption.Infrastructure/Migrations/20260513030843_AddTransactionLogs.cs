using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace peeposredemption.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTransactionLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "coin_transactions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<long>(type: "bigint", nullable: false),
                    source = table.Column<int>(type: "integer", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_coin_transactions", x => x.id);
                    table.ForeignKey(
                        name: "f_k_coin_transactions__player_characters_player_id",
                        column: x => x.player_id,
                        principalTable: "player_characters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "item_transactions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    source = table.Column<int>(type: "integer", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_item_transactions", x => x.id);
                    table.ForeignKey(
                        name: "f_k_item_transactions__player_characters_player_id",
                        column: x => x.player_id,
                        principalTable: "player_characters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "f_k_item_transactions_item_definitions_item_definition_id",
                        column: x => x.item_definition_id,
                        principalTable: "item_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_coin_transactions_player_id",
                table: "coin_transactions",
                column: "player_id");

            migrationBuilder.CreateIndex(
                name: "IX_item_transactions_item_definition_id",
                table: "item_transactions",
                column: "item_definition_id");

            migrationBuilder.CreateIndex(
                name: "IX_item_transactions_player_id",
                table: "item_transactions",
                column: "player_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "coin_transactions");

            migrationBuilder.DropTable(
                name: "item_transactions");
        }
    }
}
