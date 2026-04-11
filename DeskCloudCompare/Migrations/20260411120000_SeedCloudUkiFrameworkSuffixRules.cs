using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeskCloudCompare.Migrations
{
    /// <inheritdoc />
    public partial class SeedCloudUkiFrameworkSuffixRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Cloud framework folders for Crown Dependencies and GB use underscore (or space)
            // country suffixes — identical naming to the Desktop side — but the existing
            // suffix-strip rules are all Desktop-only (FromTypeId=3).  Without matching
            // Cloud rules (FromTypeId=2) the canonical paths diverge:
            //
            //   Cloud:   GG\Frameworks\FRS102_Trust_GG\...  → canonical keeps "_GG"
            //   Desktop: 831.\NewUserData\Frameworks\FRS102_Trust_GG\...
            //            → 831. stripped → GG\..., NewUserData stripped, _GG\ stripped
            //            → canonical: GG\Frameworks\FRS102_Trust\...
            //
            // The two rows never merge, so Copy Between Slots has no destination path
            // ("Skipped — path unknown in one slot").
            //
            // _EW\, _NI\, _SC\ are intentionally NOT stripped (same policy as Desktop)
            // so that England/Wales, Northern Ireland, and Scotland remain distinguishable
            // within the shared GB country folder.
            //
            // SortOrders 20-25 are chosen to follow the existing Cloud rule at SortOrder 10
            // (\#Updates\ → \) without conflicting with any Desktop SortOrders.

            migrationBuilder.Sql(@"INSERT OR IGNORE INTO PathTranslationRules
                (FromTypeId, ToTypeId, FindText, ReplaceText, SortOrder)
            VALUES
                (2, 1, '_JE\', '\', 20),
                (2, 1, '_GG\', '\', 21),
                (2, 1, '_GB\', '\', 22),
                (2, 1, '_IE\', '\', 23),
                (2, 1, ' GG\', '\', 24),
                (2, 1, ' JE\', '\', 25);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DELETE FROM PathTranslationRules
            WHERE FromTypeId = 2 AND ToTypeId = 1
              AND FindText IN ('_JE\', '_GG\', '_GB\', '_IE\', ' GG\', ' JE\');");
        }
    }
}
