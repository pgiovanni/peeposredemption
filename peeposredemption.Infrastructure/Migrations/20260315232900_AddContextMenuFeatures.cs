using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace peeposredemption.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddContextMenuFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Update existing Owner role values: 2 (old Owner) → 3 (new Owner)
            // Must happen before any Admin=2 values are used
            migrationBuilder.Sql("UPDATE server_members SET role = 3 WHERE role = 2;");

            migrationBuilder.AddColumn<bool>(
                name: "is_muted",
                table: "server_members",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "muted_until",
                table: "server_members",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert Owner role values: 3 → 2, and remove any Admin=2 rows
            migrationBuilder.Sql("UPDATE server_members SET role = 2 WHERE role = 3;");

            migrationBuilder.DropColumn(
                name: "is_muted",
                table: "server_members");

            migrationBuilder.DropColumn(
                name: "muted_until",
                table: "server_members");
        }
    }
}
