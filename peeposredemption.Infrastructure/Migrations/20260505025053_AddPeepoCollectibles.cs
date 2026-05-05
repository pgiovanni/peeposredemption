using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace peeposredemption.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPeepoCollectibles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "initiator_coins",
                table: "trade_offers",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "recipient_coins",
                table: "trade_offers",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<int>(
                name: "currency_type",
                table: "marketplace_listings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "source_emoji_id",
                table: "item_definitions",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "initiator_coins",
                table: "trade_offers");

            migrationBuilder.DropColumn(
                name: "recipient_coins",
                table: "trade_offers");

            migrationBuilder.DropColumn(
                name: "currency_type",
                table: "marketplace_listings");

            migrationBuilder.DropColumn(
                name: "source_emoji_id",
                table: "item_definitions");
        }
    }
}
