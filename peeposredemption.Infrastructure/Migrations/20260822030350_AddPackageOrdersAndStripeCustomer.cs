using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace peeposredemption.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPackageOrdersAndStripeCustomer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "stripe_customer_id",
                table: "users",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "package_orders",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    package_slug = table.Column<string>(type: "text", nullable: false),
                    package_name = table.Column<string>(type: "text", nullable: false),
                    price_cents = table.Column<long>(type: "bigint", nullable: false),
                    discord_guild_id = table.Column<string>(type: "text", nullable: true),
                    discord_guild_name = table.Column<string>(type: "text", nullable: true),
                    credit_usd = table.Column<decimal>(type: "numeric", nullable: true),
                    stripe_session_id = table.Column<string>(type: "text", nullable: false),
                    stripe_payment_intent_id = table.Column<string>(type: "text", nullable: true),
                    stripe_invoice_id = table.Column<string>(type: "text", nullable: true),
                    invoice_number = table.Column<string>(type: "text", nullable: true),
                    invoice_url = table.Column<string>(type: "text", nullable: true),
                    invoice_pdf_url = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    paid_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    fulfilled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_package_orders", x => x.id);
                    table.ForeignKey(
                        name: "f_k_package_orders__users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_package_orders_package_slug_status_fulfilled_at",
                table: "package_orders",
                columns: new[] { "package_slug", "status", "fulfilled_at" });

            migrationBuilder.CreateIndex(
                name: "IX_package_orders_stripe_session_id",
                table: "package_orders",
                column: "stripe_session_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_package_orders_user_id_created_at",
                table: "package_orders",
                columns: new[] { "user_id", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "package_orders");

            migrationBuilder.DropColumn(
                name: "stripe_customer_id",
                table: "users");
        }
    }
}
