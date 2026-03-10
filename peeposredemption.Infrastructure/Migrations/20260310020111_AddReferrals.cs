using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace peeposredemption.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReferrals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "referred_by_code_id",
                table: "users",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "referral_codes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "text", nullable: false),
                    label = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_referral_codes", x => x.id);
                    table.ForeignKey(
                        name: "f_k_referral_codes__users_owner_id",
                        column: x => x.owner_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "referral_purchases",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    referral_code_id = table.Column<Guid>(type: "uuid", nullable: false),
                    purchaser_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount_cents = table.Column<long>(type: "bigint", nullable: false),
                    stripe_session_id = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_referral_purchases", x => x.id);
                    table.ForeignKey(
                        name: "f_k_referral_purchases__users_purchaser_id",
                        column: x => x.purchaser_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "f_k_referral_purchases_referral_codes_referral_code_id",
                        column: x => x.referral_code_id,
                        principalTable: "referral_codes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_referral_codes_owner_id",
                table: "referral_codes",
                column: "owner_id");

            migrationBuilder.CreateIndex(
                name: "IX_referral_purchases_purchaser_id",
                table: "referral_purchases",
                column: "purchaser_id");

            migrationBuilder.CreateIndex(
                name: "IX_referral_purchases_referral_code_id",
                table: "referral_purchases",
                column: "referral_code_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "referral_purchases");

            migrationBuilder.DropTable(
                name: "referral_codes");

            migrationBuilder.DropColumn(
                name: "referred_by_code_id",
                table: "users");
        }
    }
}
