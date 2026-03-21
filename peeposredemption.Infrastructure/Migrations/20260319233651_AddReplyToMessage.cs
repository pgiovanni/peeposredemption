using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace peeposredemption.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReplyToMessage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "reply_to_message_id",
                table: "messages",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_messages_reply_to_message_id",
                table: "messages",
                column: "reply_to_message_id");

            migrationBuilder.AddForeignKey(
                name: "f_k_messages_messages_reply_to_message_id",
                table: "messages",
                column: "reply_to_message_id",
                principalTable: "messages",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "f_k_messages_messages_reply_to_message_id",
                table: "messages");

            migrationBuilder.DropIndex(
                name: "IX_messages_reply_to_message_id",
                table: "messages");

            migrationBuilder.DropColumn(
                name: "reply_to_message_id",
                table: "messages");
        }
    }
}
