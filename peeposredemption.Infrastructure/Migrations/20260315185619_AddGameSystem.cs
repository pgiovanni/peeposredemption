using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace peeposredemption.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGameSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "game_channel_configs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    channel_id = table.Column<Guid>(type: "uuid", nullable: false),
                    game_bot_muted = table.Column<bool>(type: "boolean", nullable: false),
                    muted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    muted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_game_channel_configs", x => x.id);
                    table.ForeignKey(
                        name: "f_k_game_channel_configs_channels_channel_id",
                        column: x => x.channel_id,
                        principalTable: "channels",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "item_definitions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    sub_type = table.Column<int>(type: "integer", nullable: false),
                    equip_slot = table.Column<int>(type: "integer", nullable: true),
                    rarity = table.Column<int>(type: "integer", nullable: false),
                    icon = table.Column<string>(type: "text", nullable: false),
                    level_req = table.Column<int>(type: "integer", nullable: false),
                    class_req = table.Column<int>(type: "integer", nullable: true),
                    is_stackable = table.Column<bool>(type: "boolean", nullable: false),
                    buy_price = table.Column<long>(type: "bigint", nullable: false),
                    sell_price = table.Column<long>(type: "bigint", nullable: false),
                    bonus_s_t_r = table.Column<int>(type: "integer", nullable: false),
                    bonus_d_e_f = table.Column<int>(type: "integer", nullable: false),
                    bonus_i_n_t = table.Column<int>(type: "integer", nullable: false),
                    bonus_d_e_x = table.Column<int>(type: "integer", nullable: false),
                    bonus_v_i_t = table.Column<int>(type: "integer", nullable: false),
                    bonus_l_u_k = table.Column<int>(type: "integer", nullable: false),
                    bonus_h_p = table.Column<int>(type: "integer", nullable: false),
                    bonus_m_p = table.Column<int>(type: "integer", nullable: false),
                    min_damage = table.Column<int>(type: "integer", nullable: false),
                    max_damage = table.Column<int>(type: "integer", nullable: false),
                    element = table.Column<int>(type: "integer", nullable: false),
                    heal_amount = table.Column<int>(type: "integer", nullable: false),
                    mana_restore_amount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_item_definitions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "monster_definitions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    icon = table.Column<string>(type: "text", nullable: false),
                    level = table.Column<int>(type: "integer", nullable: false),
                    zone = table.Column<string>(type: "text", nullable: false),
                    max_hp = table.Column<int>(type: "integer", nullable: false),
                    s_t_r = table.Column<int>(type: "integer", nullable: false),
                    d_e_f = table.Column<int>(type: "integer", nullable: false),
                    i_n_t = table.Column<int>(type: "integer", nullable: false),
                    d_e_x = table.Column<int>(type: "integer", nullable: false),
                    min_damage = table.Column<int>(type: "integer", nullable: false),
                    max_damage = table.Column<int>(type: "integer", nullable: false),
                    element = table.Column<int>(type: "integer", nullable: false),
                    xp_reward = table.Column<long>(type: "bigint", nullable: false),
                    orb_reward_min = table.Column<long>(type: "bigint", nullable: false),
                    orb_reward_max = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_monster_definitions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "player_characters",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    character_name = table.Column<string>(type: "text", nullable: false),
                    @class = table.Column<int>(name: "class", type: "integer", nullable: false),
                    level = table.Column<int>(type: "integer", nullable: false),
                    x_p = table.Column<long>(type: "bigint", nullable: false),
                    s_t_r = table.Column<int>(type: "integer", nullable: false),
                    d_e_f = table.Column<int>(type: "integer", nullable: false),
                    i_n_t = table.Column<int>(type: "integer", nullable: false),
                    d_e_x = table.Column<int>(type: "integer", nullable: false),
                    v_i_t = table.Column<int>(type: "integer", nullable: false),
                    l_u_k = table.Column<int>(type: "integer", nullable: false),
                    current_hp = table.Column<int>(type: "integer", nullable: false),
                    max_hp = table.Column<int>(type: "integer", nullable: false),
                    current_mp = table.Column<int>(type: "integer", nullable: false),
                    max_mp = table.Column<int>(type: "integer", nullable: false),
                    total_monsters_killed = table.Column<int>(type: "integer", nullable: false),
                    total_deaths = table.Column<int>(type: "integer", nullable: false),
                    last_gather_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_player_characters", x => x.id);
                    table.ForeignKey(
                        name: "f_k_player_characters__users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "crafting_recipes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    output_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    output_quantity = table.Column<int>(type: "integer", nullable: false),
                    required_skill = table.Column<int>(type: "integer", nullable: false),
                    required_skill_level = table.Column<int>(type: "integer", nullable: false),
                    orb_cost = table.Column<long>(type: "bigint", nullable: false),
                    base_success_rate = table.Column<decimal>(type: "numeric", nullable: false),
                    xp_reward = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_crafting_recipes", x => x.id);
                    table.ForeignKey(
                        name: "f_k_crafting_recipes__item_definitions_output_item_id",
                        column: x => x.output_item_id,
                        principalTable: "item_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "monster_loot_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    monster_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    drop_chance = table.Column<decimal>(type: "numeric", nullable: false),
                    min_quantity = table.Column<int>(type: "integer", nullable: false),
                    max_quantity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_monster_loot_entries", x => x.id);
                    table.ForeignKey(
                        name: "f_k_monster_loot_entries_item_definitions_item_definition_id",
                        column: x => x.item_definition_id,
                        principalTable: "item_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "f_k_monster_loot_entries_monster_definitions_monster_definition~",
                        column: x => x.monster_definition_id,
                        principalTable: "monster_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "combat_sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    monster_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    channel_id = table.Column<Guid>(type: "uuid", nullable: false),
                    state = table.Column<int>(type: "integer", nullable: false),
                    turn_number = table.Column<int>(type: "integer", nullable: false),
                    is_player_turn = table.Column<bool>(type: "boolean", nullable: false),
                    monster_current_hp = table.Column<int>(type: "integer", nullable: false),
                    monster_max_hp = table.Column<int>(type: "integer", nullable: false),
                    player_hp_at_start = table.Column<int>(type: "integer", nullable: false),
                    player_defending = table.Column<bool>(type: "boolean", nullable: false),
                    combat_log = table.Column<string>(type: "text", nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_turn_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ended_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_combat_sessions", x => x.id);
                    table.ForeignKey(
                        name: "f_k_combat_sessions__monster_definitions_monster_definition_id",
                        column: x => x.monster_definition_id,
                        principalTable: "monster_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "f_k_combat_sessions__player_characters_player_id",
                        column: x => x.player_id,
                        principalTable: "player_characters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "marketplace_listings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    seller_id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    price_per_unit = table.Column<long>(type: "bigint", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_marketplace_listings", x => x.id);
                    table.ForeignKey(
                        name: "f_k_marketplace_listings__player_characters_buyer_id",
                        column: x => x.buyer_id,
                        principalTable: "player_characters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "f_k_marketplace_listings__player_characters_seller_id",
                        column: x => x.seller_id,
                        principalTable: "player_characters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "f_k_marketplace_listings_item_definitions_item_definition_id",
                        column: x => x.item_definition_id,
                        principalTable: "item_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "player_inventory_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    is_equipped = table.Column<bool>(type: "boolean", nullable: false),
                    equipped_slot = table.Column<int>(type: "integer", nullable: true),
                    enchant_element = table.Column<int>(type: "integer", nullable: true),
                    enchant_bonus = table.Column<int>(type: "integer", nullable: false),
                    enchant_name = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_player_inventory_items", x => x.id);
                    table.ForeignKey(
                        name: "f_k_player_inventory_items_item_definitions_item_definition_id",
                        column: x => x.item_definition_id,
                        principalTable: "item_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "f_k_player_inventory_items_player_characters_player_id",
                        column: x => x.player_id,
                        principalTable: "player_characters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "player_skills",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    skill_type = table.Column<int>(type: "integer", nullable: false),
                    level = table.Column<int>(type: "integer", nullable: false),
                    x_p = table.Column<long>(type: "bigint", nullable: false),
                    xp_to_next_level = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_player_skills", x => x.id);
                    table.ForeignKey(
                        name: "f_k_player_skills_player_characters_player_id",
                        column: x => x.player_id,
                        principalTable: "player_characters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "trade_offers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    initiator_id = table.Column<Guid>(type: "uuid", nullable: false),
                    recipient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    channel_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    initiator_items = table.Column<string>(type: "text", nullable: false),
                    initiator_orbs = table.Column<long>(type: "bigint", nullable: false),
                    recipient_items = table.Column<string>(type: "text", nullable: false),
                    recipient_orbs = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_trade_offers", x => x.id);
                    table.ForeignKey(
                        name: "f_k_trade_offers_player_characters_initiator_id",
                        column: x => x.initiator_id,
                        principalTable: "player_characters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "f_k_trade_offers_player_characters_recipient_id",
                        column: x => x.recipient_id,
                        principalTable: "player_characters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "crafting_recipe_ingredients",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    recipe_id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_crafting_recipe_ingredients", x => x.id);
                    table.ForeignKey(
                        name: "f_k_crafting_recipe_ingredients__item_definitions_item_definition~",
                        column: x => x.item_definition_id,
                        principalTable: "item_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "f_k_crafting_recipe_ingredients_crafting_recipes_recipe_id",
                        column: x => x.recipe_id,
                        principalTable: "crafting_recipes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_combat_sessions_monster_definition_id",
                table: "combat_sessions",
                column: "monster_definition_id");

            migrationBuilder.CreateIndex(
                name: "IX_combat_sessions_player_id",
                table: "combat_sessions",
                column: "player_id");

            migrationBuilder.CreateIndex(
                name: "IX_crafting_recipe_ingredients_item_definition_id",
                table: "crafting_recipe_ingredients",
                column: "item_definition_id");

            migrationBuilder.CreateIndex(
                name: "IX_crafting_recipe_ingredients_recipe_id",
                table: "crafting_recipe_ingredients",
                column: "recipe_id");

            migrationBuilder.CreateIndex(
                name: "IX_crafting_recipes_output_item_id",
                table: "crafting_recipes",
                column: "output_item_id");

            migrationBuilder.CreateIndex(
                name: "IX_game_channel_configs_channel_id",
                table: "game_channel_configs",
                column: "channel_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_item_definitions_name",
                table: "item_definitions",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_marketplace_listings_buyer_id",
                table: "marketplace_listings",
                column: "buyer_id");

            migrationBuilder.CreateIndex(
                name: "IX_marketplace_listings_item_definition_id",
                table: "marketplace_listings",
                column: "item_definition_id");

            migrationBuilder.CreateIndex(
                name: "IX_marketplace_listings_seller_id",
                table: "marketplace_listings",
                column: "seller_id");

            migrationBuilder.CreateIndex(
                name: "IX_marketplace_listings_status_expires_at",
                table: "marketplace_listings",
                columns: new[] { "status", "expires_at" });

            migrationBuilder.CreateIndex(
                name: "IX_monster_loot_entries_item_definition_id",
                table: "monster_loot_entries",
                column: "item_definition_id");

            migrationBuilder.CreateIndex(
                name: "IX_monster_loot_entries_monster_definition_id",
                table: "monster_loot_entries",
                column: "monster_definition_id");

            migrationBuilder.CreateIndex(
                name: "IX_player_characters_user_id",
                table: "player_characters",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_player_inventory_items_item_definition_id",
                table: "player_inventory_items",
                column: "item_definition_id");

            migrationBuilder.CreateIndex(
                name: "IX_player_inventory_items_player_id",
                table: "player_inventory_items",
                column: "player_id");

            migrationBuilder.CreateIndex(
                name: "IX_player_skills_player_id_skill_type",
                table: "player_skills",
                columns: new[] { "player_id", "skill_type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_trade_offers_initiator_id",
                table: "trade_offers",
                column: "initiator_id");

            migrationBuilder.CreateIndex(
                name: "IX_trade_offers_recipient_id",
                table: "trade_offers",
                column: "recipient_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "combat_sessions");

            migrationBuilder.DropTable(
                name: "crafting_recipe_ingredients");

            migrationBuilder.DropTable(
                name: "game_channel_configs");

            migrationBuilder.DropTable(
                name: "marketplace_listings");

            migrationBuilder.DropTable(
                name: "monster_loot_entries");

            migrationBuilder.DropTable(
                name: "player_inventory_items");

            migrationBuilder.DropTable(
                name: "player_skills");

            migrationBuilder.DropTable(
                name: "trade_offers");

            migrationBuilder.DropTable(
                name: "crafting_recipes");

            migrationBuilder.DropTable(
                name: "monster_definitions");

            migrationBuilder.DropTable(
                name: "player_characters");

            migrationBuilder.DropTable(
                name: "item_definitions");
        }
    }
}
