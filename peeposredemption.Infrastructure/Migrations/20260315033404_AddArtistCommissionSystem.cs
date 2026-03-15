using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace peeposredemption.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddArtistCommissionSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "artists",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    display_name = table.Column<string>(type: "text", nullable: false),
                    bio = table.Column<string>(type: "text", nullable: true),
                    payout_email = table.Column<string>(type: "text", nullable: false),
                    payout_method = table.Column<int>(type: "integer", nullable: false),
                    total_earned_cents = table.Column<long>(type: "bigint", nullable: false),
                    total_paid_cents = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_artists", x => x.id);
                    table.ForeignKey(
                        name: "f_k_artists__users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "art_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    artist_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    rarity = table.Column<int>(type: "integer", nullable: false),
                    item_type = table.Column<int>(type: "integer", nullable: false),
                    asset_url = table.Column<string>(type: "text", nullable: false),
                    r2_key = table.Column<string>(type: "text", nullable: false),
                    orb_value = table.Column<long>(type: "bigint", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_art_items", x => x.id);
                    table.ForeignKey(
                        name: "f_k_art_items__artists_artist_id",
                        column: x => x.artist_id,
                        principalTable: "artists",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "artist_payouts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    artist_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount_cents = table.Column<long>(type: "bigint", nullable: false),
                    payout_method = table.Column<int>(type: "integer", nullable: false),
                    reference = table.Column<string>(type: "text", nullable: true),
                    paid_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_artist_payouts", x => x.id);
                    table.ForeignKey(
                        name: "f_k_artist_payouts_artists_artist_id",
                        column: x => x.artist_id,
                        principalTable: "artists",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "artist_commissions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    artist_id = table.Column<Guid>(type: "uuid", nullable: false),
                    art_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    orb_amount = table.Column<long>(type: "bigint", nullable: false),
                    commission_orbs = table.Column<long>(type: "bigint", nullable: false),
                    commission_cents = table.Column<long>(type: "bigint", nullable: false),
                    source = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_artist_commissions", x => x.id);
                    table.ForeignKey(
                        name: "f_k_artist_commissions__users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "f_k_artist_commissions_art_items_art_item_id",
                        column: x => x.art_item_id,
                        principalTable: "art_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "f_k_artist_commissions_artists_artist_id",
                        column: x => x.artist_id,
                        principalTable: "artists",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_art_items_artist_id_rarity",
                table: "art_items",
                columns: new[] { "artist_id", "rarity" });

            migrationBuilder.CreateIndex(
                name: "IX_art_items_r2_key",
                table: "art_items",
                column: "r2_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_artist_commissions_art_item_id",
                table: "artist_commissions",
                column: "art_item_id");

            migrationBuilder.CreateIndex(
                name: "IX_artist_commissions_artist_id_created_at",
                table: "artist_commissions",
                columns: new[] { "artist_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_artist_commissions_user_id",
                table: "artist_commissions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_artist_payouts_artist_id",
                table: "artist_payouts",
                column: "artist_id");

            migrationBuilder.CreateIndex(
                name: "IX_artists_payout_email",
                table: "artists",
                column: "payout_email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_artists_user_id",
                table: "artists",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "artist_commissions");

            migrationBuilder.DropTable(
                name: "artist_payouts");

            migrationBuilder.DropTable(
                name: "art_items");

            migrationBuilder.DropTable(
                name: "artists");
        }
    }
}
