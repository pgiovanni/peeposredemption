using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace peeposredemption.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBadgeOrbReward : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "orb_reward",
                table: "badge_definitions",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            // Backfill orb rewards for existing seeded badges
            migrationBuilder.Sql(@"
                UPDATE badge_definitions SET orb_reward = CASE name
                    WHEN 'First Steps'       THEN 5
                    WHEN 'Chatterbox'        THEN 50
                    WHEN 'Wordsmith'         THEN 250
                    WHEN 'Legend'            THEN 1000
                    WHEN 'Dedicated'         THEN 25
                    WHEN 'Committed'         THEN 150
                    WHEN 'Devoted'           THEN 500
                    WHEN 'Generous'          THEN 25
                    WHEN 'Philanthropist'    THEN 500
                    WHEN 'Social Butterfly'  THEN 25
                    WHEN 'Networker'         THEN 100
                    WHEN 'First Orb'         THEN 10
                    WHEN 'Hoarder'           THEN 500
                    ELSE 0
                END;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "orb_reward",
                table: "badge_definitions");
        }
    }
}
