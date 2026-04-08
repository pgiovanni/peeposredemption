using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace peeposredemption.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_notifications_user_id",
                table: "notifications");

            migrationBuilder.DropIndex(
                name: "IX_messages_channel_id",
                table: "messages");

            migrationBuilder.CreateIndex(
                name: "IX_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_notifications_user_id_created_at",
                table: "notifications",
                columns: new[] { "user_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_notifications_user_id_is_read",
                table: "notifications",
                columns: new[] { "user_id", "is_read" });

            migrationBuilder.CreateIndex(
                name: "IX_messages_channel_id_sent_at",
                table: "messages",
                columns: new[] { "channel_id", "sent_at" });

            migrationBuilder.CreateIndex(
                name: "IX_direct_messages_recipient_id_is_read",
                table: "direct_messages",
                columns: new[] { "recipient_id", "is_read" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_users_email",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_notifications_user_id_created_at",
                table: "notifications");

            migrationBuilder.DropIndex(
                name: "IX_notifications_user_id_is_read",
                table: "notifications");

            migrationBuilder.DropIndex(
                name: "IX_messages_channel_id_sent_at",
                table: "messages");

            migrationBuilder.DropIndex(
                name: "IX_direct_messages_recipient_id_is_read",
                table: "direct_messages");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_user_id",
                table: "notifications",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_messages_channel_id",
                table: "messages",
                column: "channel_id");
        }
    }
}
