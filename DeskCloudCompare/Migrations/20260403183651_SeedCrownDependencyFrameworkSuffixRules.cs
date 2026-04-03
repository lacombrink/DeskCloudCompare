using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeskCloudCompare.Migrations
{
    /// <inheritdoc />
    public partial class SeedCrownDependencyFrameworkSuffixRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Framework folder names for Crown Dependencies and GB nations use an underscore
            // suffix that must be stripped to produce the canonical path.
            //
            // Cloud uses _GB\ for Great Britain frameworks:
            //   GB\Frameworks\FRS102(1A)_Company_GB\  →  GB\Frameworks\FRS102(1A)_Company\
            //
            // Cloud uses _GG\ for Guernsey frameworks:
            //   GG\Frameworks\IFRS+_GG\  →  GG\Frameworks\IFRS+\
            //
            // Desktop uses _EW\, _NI\, _SC\ (country code is already converted to GB
            // by rules at SortOrder 2-4 before these rules run):
            //   GB\Frameworks\FRS102(1A)_Company_EW\  →  GB\Frameworks\FRS102(1A)_Company\
            //   GB\Frameworks\FRS102(1A)_Company_NI\  →  GB\Frameworks\FRS102(1A)_Company\
            //   GB\Frameworks\FRS102(1A)_Company_SC\  →  GB\Frameworks\FRS102(1A)_Company\
            //
            // _JE\ is handled in the previous migration (SortOrder 56).
            // SortOrders 59-63 are used here to avoid conflicts with existing rules.

            migrationBuilder.Sql(@"INSERT OR IGNORE INTO PathTranslationRules
                (FromTypeId, ToTypeId, FindText, ReplaceText, SortOrder)
            VALUES
                (3, 1, '_GG\', '\', 59),
                (3, 1, '_GB\', '\', 60),
                (3, 1, '_EW\', '\', 61),
                (3, 1, '_NI\', '\', 62),
                (3, 1, '_SC\', '\', 63);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DELETE FROM PathTranslationRules
            WHERE FromTypeId = 3 AND ToTypeId = 1
              AND FindText IN ('_GG\', '_GB\', '_EW\', '_NI\', '_SC\');");
        }
    }
}
