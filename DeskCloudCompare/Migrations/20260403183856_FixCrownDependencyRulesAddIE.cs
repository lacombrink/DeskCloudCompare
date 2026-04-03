using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeskCloudCompare.Migrations
{
    /// <inheritdoc />
    public partial class FixCrownDependencyRulesAddIE : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // _EW\, _NI\, _SC\ must NOT be stripped. All three Desktop nation sub-folders
            // (England/Wales, Northern Ireland, Scotland) live under the same GB\ country
            // folder in Cloud. Their framework suffix (_EW, _NI, _SC) is the only thing
            // that distinguishes them — removing it would collapse all three to the same
            // canonical path and prevent matching. Keep these suffixes intact.
            migrationBuilder.Sql(@"DELETE FROM PathTranslationRules
            WHERE FromTypeId = 3 AND ToTypeId = 1
              AND FindText IN ('_EW\', '_NI\', '_SC\');");

            // Add _IE\ → \ for Ireland (its own country folder, suffix can be dropped).
            migrationBuilder.Sql(@"INSERT OR IGNORE INTO PathTranslationRules
                (FromTypeId, ToTypeId, FindText, ReplaceText, SortOrder)
            VALUES (3, 1, '_IE\', '\', 64);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DELETE FROM PathTranslationRules
            WHERE FromTypeId = 3 AND ToTypeId = 1 AND FindText = '_IE\';");

            migrationBuilder.Sql(@"INSERT OR IGNORE INTO PathTranslationRules
                (FromTypeId, ToTypeId, FindText, ReplaceText, SortOrder)
            VALUES
                (3, 1, '_EW\', '\', 61),
                (3, 1, '_NI\', '\', 62),
                (3, 1, '_SC\', '\', 63);");
        }
    }
}
