using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeskCloudCompare.Migrations
{
    /// <inheritdoc />
    public partial class SeedJEFrameworkSuffixRule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Desktop framework folders that are Jersey-specific use an underscore suffix:
            //   FRS102(1A)_Company_JE\
            // The existing rule at SortOrder 55 strips " JE\" (space + JE) which handles
            // framework names like "IFRS+ JE\" but does NOT match the underscore variant.
            //
            // This rule strips "_JE\" so that:
            //   JE\Frameworks\FRS102(1A)_Company_JE\Lead Schedules\
            //   → JE\Frameworks\FRS102(1A)_Company\Lead Schedules\
            //
            // which then matches Cloud:
            //   JE\Frameworks\FRS102(1A)_Company\Lead Schedules\
            //
            // SortOrder 56 runs immediately after the space-based " JE\" rule (55),
            // and after DraftworX Files is already stripped (31), so the path is clean.

            migrationBuilder.Sql(@"INSERT OR IGNORE INTO PathTranslationRules
                (FromTypeId, ToTypeId, FindText, ReplaceText, SortOrder)
            VALUES (3, 1, '_JE\', '\', 56);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DELETE FROM PathTranslationRules
            WHERE FromTypeId = 3 AND ToTypeId = 1 AND FindText = '_JE\' AND SortOrder = 56;");
        }
    }
}
