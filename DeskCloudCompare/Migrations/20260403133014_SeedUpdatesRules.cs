using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeskCloudCompare.Migrations
{
    /// <inheritdoc />
    public partial class SeedUpdatesRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ---------------------------------------------------------------
            // Updates folder mapping
            //
            // Desktop: \072.BW\NewUserDataUpdates\Body Corp+ BW\Detail.xlsx
            // Cloud:   \BW\Frameworks\Body Corp+\#Updates\Detail.xlsx
            //
            // After existing Desktop rules strip the numeric prefix (072. → "")
            // and the framework country suffix (" BW\" → "\"), the canonical
            // form becomes: \BW\Frameworks\Body Corp+\Detail.xlsx
            //
            // Achieved by two new rules:
            //   1. Desktop → Canonical: replace \NewUserDataUpdates\ with \Frameworks\
            //      (SortOrder 29 — runs just before the \NewUserData\ rule at 30)
            //   2. Cloud → Canonical:   strip \#Updates\ from cloud paths
            //      (Cloud had no rules before; this is the first one)
            // ---------------------------------------------------------------

            // Desktop rule (FromTypeId=3 Desktop, ToTypeId=1 Canonical)
            migrationBuilder.Sql(@"INSERT OR IGNORE INTO PathTranslationRules (FromTypeId, ToTypeId, FindText, ReplaceText, SortOrder) VALUES (3, 1, '\NewUserDataUpdates\', '\Frameworks\', 29);");

            // Cloud rule (FromTypeId=2 Cloud, ToTypeId=1 Canonical)
            migrationBuilder.Sql(@"INSERT OR IGNORE INTO PathTranslationRules (FromTypeId, ToTypeId, FindText, ReplaceText, SortOrder) VALUES (2, 1, '\#Updates\', '\', 10);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DELETE FROM PathTranslationRules WHERE FindText = '\NewUserDataUpdates\' AND FromTypeId = 3;");
            migrationBuilder.Sql(@"DELETE FROM PathTranslationRules WHERE FindText = '\#Updates\' AND FromTypeId = 2;");
        }
    }
}
