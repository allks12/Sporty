using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sporty.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTimeSlotOverlapConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE "TimeSlots"
                ADD CONSTRAINT "EX_TimeSlots_NoOverlapPerVenue"
                EXCLUDE USING gist (
                    "VenueId" WITH =,
                    tsrange("StartAt", "EndAt", '[)') WITH &&
                );
                """);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE "TimeSlots"
                DROP CONSTRAINT "EX_TimeSlots_NoOverlapPerVenue";
                """);
        }
    }
}
