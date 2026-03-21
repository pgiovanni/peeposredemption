using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace peeposredemption.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSupportTickets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "support_tickets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category = table.Column<int>(type: "integer", nullable: false),
                    subject = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_support_tickets", x => x.id);
                    table.ForeignKey(
                        name: "f_k_support_tickets__users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_support_tickets_user_id",
                table: "support_tickets",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "support_tickets");
        }
    }
}
