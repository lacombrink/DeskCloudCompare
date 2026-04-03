using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeskCloudCompare.Migrations
{
    /// <inheritdoc />
    public partial class FixRacExclusions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Remove the incorrect seed from AddPresetExclusions:
            //   Wrong path:    Frameworks\RAC  (actual path is Methodologies\RAC)
            //   Wrong country: ZA excluded     (ZA is one of the TWO valid pairs)
            migrationBuilder.Sql(@"DELETE FROM PresetExclusions
            WHERE PresetId = 1 AND Pattern = 'ZA\Frameworks\RAC';");

            // Correct exclusions — MatchType 0 = Contains
            //
            // Desktop (CleanInstallSet) has Methodologies\RAC in 15 countries:
            //   AA, BW, GH, KE, LR, MU, NA, NG, RW, SA, ZA, SZ, UG, TZ, ZM
            //
            // Cloud (CloudProduction) has Methodologies\RAC in 2 countries only:
            //   LR, ZA
            //
            // Exclude the 13 countries where RAC exists in Desktop but NOT Cloud.
            // LR and ZA are left in — both sides have RAC so comparison is meaningful.
            migrationBuilder.Sql(@"INSERT OR IGNORE INTO PresetExclusions
                (PresetId, Pattern, MatchType, Description, IsActive)
            VALUES
                (1, 'ZZ\Methodologies\RAC', 0, 'RAC — Desktop only (no Cloud equivalent)', 1),
                (1, 'BW\Methodologies\RAC', 0, 'RAC — Desktop only (no Cloud equivalent)', 1),
                (1, 'GH\Methodologies\RAC', 0, 'RAC — Desktop only (no Cloud equivalent)', 1),
                (1, 'KE\Methodologies\RAC', 0, 'RAC — Desktop only (no Cloud equivalent)', 1),
                (1, 'MU\Methodologies\RAC', 0, 'RAC — Desktop only (no Cloud equivalent)', 1),
                (1, 'NA\Methodologies\RAC', 0, 'RAC — Desktop only (no Cloud equivalent)', 1),
                (1, 'NG\Methodologies\RAC', 0, 'RAC — Desktop only (no Cloud equivalent)', 1),
                (1, 'RW\Methodologies\RAC', 0, 'RAC — Desktop only (no Cloud equivalent)', 1),
                (1, 'SA\Methodologies\RAC', 0, 'RAC — Desktop only (no Cloud equivalent)', 1),
                (1, 'SZ\Methodologies\RAC', 0, 'RAC — Desktop only (no Cloud equivalent)', 1),
                (1, 'UG\Methodologies\RAC', 0, 'RAC — Desktop only (no Cloud equivalent)', 1),
                (1, 'TZ\Methodologies\RAC', 0, 'RAC — Desktop only (no Cloud equivalent)', 1),
                (1, 'ZM\Methodologies\RAC', 0, 'RAC — Desktop only (no Cloud equivalent)', 1);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DELETE FROM PresetExclusions
            WHERE PresetId = 1 AND Pattern LIKE '%\Methodologies\RAC';");

            // Restore the (incorrect) original seed
            migrationBuilder.Sql(@"INSERT OR IGNORE INTO PresetExclusions
                (PresetId, Pattern, MatchType, Description, IsActive)
            VALUES (1, 'ZA\Frameworks\RAC', 0, 'RAC framework — Cloud ZA only, no Desktop equivalent', 1);");
        }
    }
}
