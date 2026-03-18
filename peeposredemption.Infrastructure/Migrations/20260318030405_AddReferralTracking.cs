using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace peeposredemption.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReferralTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "user_id",
                table: "storage_upgrade_purchases",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "link_clicks",
                table: "referral_codes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "link_copies",
                table: "referral_codes",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "user_id",
                table: "storage_upgrade_purchases");

            migrationBuilder.DropColumn(
                name: "link_clicks",
                table: "referral_codes");

            migrationBuilder.DropColumn(
                name: "link_copies",
                table: "referral_codes");
        }
    }
}
