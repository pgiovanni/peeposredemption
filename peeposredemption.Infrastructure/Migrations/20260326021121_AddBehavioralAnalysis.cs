using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace peeposredemption.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBehavioralAnalysis : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_messages_author_id",
                table: "messages");

            migrationBuilder.DropIndex(
                name: "IX_direct_messages_recipient_id",
                table: "direct_messages");

            migrationBuilder.DropIndex(
                name: "IX_direct_messages_sender_id",
                table: "direct_messages");

            migrationBuilder.DropIndex(
                name: "IX_banned_fingerprints_fingerprint_hash",
                table: "banned_fingerprints");

            migrationBuilder.CreateTable(
                name: "alt_suspicions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id1 = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id2 = table.Column<Guid>(type: "uuid", nullable: false),
                    score = table.Column<int>(type: "integer", nullable: false),
                    signals = table.Column<string>(type: "text", nullable: false),
                    detected_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    reviewed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_confirmed = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_alt_suspicions", x => x.id);
                    table.ForeignKey(
                        name: "f_k_alt_suspicions__users_user1_id",
                        column: x => x.user_id1,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "f_k_alt_suspicions__users_user2_id",
                        column: x => x.user_id2,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_messages_author_id_sent_at",
                table: "messages",
                columns: new[] { "author_id", "sent_at" });

            migrationBuilder.CreateIndex(
                name: "IX_direct_messages_recipient_id_sent_at",
                table: "direct_messages",
                columns: new[] { "recipient_id", "sent_at" });

            migrationBuilder.CreateIndex(
                name: "IX_direct_messages_sender_id_sent_at",
                table: "direct_messages",
                columns: new[] { "sender_id", "sent_at" });

            migrationBuilder.CreateIndex(
                name: "IX_banned_fingerprints_fingerprint_hash",
                table: "banned_fingerprints",
                column: "fingerprint_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_alt_suspicions_user_id1_user_id2",
                table: "alt_suspicions",
                columns: new[] { "user_id1", "user_id2" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_alt_suspicions_user_id2",
                table: "alt_suspicions",
                column: "user_id2");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "alt_suspicions");

            migrationBuilder.DropIndex(
                name: "IX_messages_author_id_sent_at",
                table: "messages");

            migrationBuilder.DropIndex(
                name: "IX_direct_messages_recipient_id_sent_at",
                table: "direct_messages");

            migrationBuilder.DropIndex(
                name: "IX_direct_messages_sender_id_sent_at",
                table: "direct_messages");

            migrationBuilder.DropIndex(
                name: "IX_banned_fingerprints_fingerprint_hash",
                table: "banned_fingerprints");

            migrationBuilder.CreateIndex(
                name: "IX_messages_author_id",
                table: "messages",
                column: "author_id");

            migrationBuilder.CreateIndex(
                name: "IX_direct_messages_recipient_id",
                table: "direct_messages",
                column: "recipient_id");

            migrationBuilder.CreateIndex(
                name: "IX_direct_messages_sender_id",
                table: "direct_messages",
                column: "sender_id");

            migrationBuilder.CreateIndex(
                name: "IX_banned_fingerprints_fingerprint_hash",
                table: "banned_fingerprints",
                column: "fingerprint_hash");
        }
    }
}
