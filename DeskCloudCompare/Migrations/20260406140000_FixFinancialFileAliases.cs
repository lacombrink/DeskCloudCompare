using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814

namespace DeskCloudCompare.Migrations
{
    /// <inheritdoc />
    public partial class FixFinancialFileAliases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Delete all previously seeded financial aliases (IDs 16-43) and reinsert
            // with the corrected mappings:
            //   - FRS101 now maps to IFRS+ Financials (was FRS102 Financials)
            //   - IFRS+ Consolidation and all other consolidation variants map directly
            //     to the entity Financials canonical (no intermediate consolidation canonical)
            //   - IDs 40-45 for Afrikaans/Legacy (previously 38-43, renumbered)

            migrationBuilder.DeleteData(
                table: "MasterFileAliases",
                keyColumn: "Id",
                keyValues: new object[] { 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28,
                                          29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43 });

            migrationBuilder.InsertData(
                table: "MasterFileAliases",
                columns: new[] { "Id", "ActualFileName", "CanonicalFileName", "FolderPath" },
                values: new object[,]
                {
                    // IFRS type group: all entity + consolidation → IFRS+ Financials.xlsx
                    { 16, "IFRS+ Consolidation.xlsx",          "IFRS+ Financials.xlsx",            @"Financial Data\DraftworX Files" },
                    { 17, "IFRS SME+ Consolidation.xlsx",      "IFRS+ Financials.xlsx",            @"Financial Data\DraftworX Files" },
                    { 18, "IFRS Consolidation.xlsx",           "IFRS+ Financials.xlsx",            @"Financial Data\DraftworX Files" },
                    { 19, "ASPE+ Financials.xlsx",             "IFRS+ Financials.xlsx",            @"Financial Data\DraftworX Files" },
                    { 20, "IFRS SME+ Financials.xlsx",         "IFRS+ Financials.xlsx",            @"Financial Data\DraftworX Files" },
                    { 21, "IFRS Financials.xlsx",              "IFRS+ Financials.xlsx",            @"Financial Data\DraftworX Files" },
                    { 22, "FRS101 Financials.xlsx",            "IFRS+ Financials.xlsx",            @"Financial Data\DraftworX Files" },
                    { 23, "Body Corp+ Financials.xlsx",        "IFRS+ Financials.xlsx",            @"Financial Data\DraftworX Files" },
                    { 24, "CC+ Financials.xlsx",               "IFRS+ Financials.xlsx",            @"Financial Data\DraftworX Files" },
                    { 25, "NPC+ Financials.xlsx",              "IFRS+ Financials.xlsx",            @"Financial Data\DraftworX Files" },
                    { 26, "NPO+ Financials.xlsx",              "IFRS+ Financials.xlsx",            @"Financial Data\DraftworX Files" },
                    { 27, "Partnership+ Financials.xlsx",      "IFRS+ Financials.xlsx",            @"Financial Data\DraftworX Files" },
                    { 28, "School+ Financials.xlsx",           "IFRS+ Financials.xlsx",            @"Financial Data\DraftworX Files" },
                    { 29, "Sole Prop+ Financials.xlsx",        "IFRS+ Financials.xlsx",            @"Financial Data\DraftworX Files" },
                    { 30, "Trust+ Financials.xlsx",            "IFRS+ Financials.xlsx",            @"Financial Data\DraftworX Files" },
                    // FRS type group: company entity + consolidations → FRS102 Financials.xlsx
                    { 31, "FRS102 Consolidation.xlsx",         "FRS102 Financials.xlsx",           @"Financial Data\DraftworX Files" },
                    { 32, "FRS102(1A) Consolidation.xlsx",     "FRS102 Financials.xlsx",           @"Financial Data\DraftworX Files" },
                    { 33, "FRS102(1A) Financials.xlsx",        "FRS102 Financials.xlsx",           @"Financial Data\DraftworX Files" },
                    { 34, "FRS105 Financials.xlsx",            "FRS102 Financials.xlsx",           @"Financial Data\DraftworX Files" },
                    // FRS type group → Partnership / Sole Prop / LLP
                    { 35, "Partnership(1A) Financials.xlsx",   "Partnership Financials.xlsx",      @"Financial Data\DraftworX Files" },
                    { 36, "Partnership(105) Financials.xlsx",  "Partnership Financials.xlsx",      @"Financial Data\DraftworX Files" },
                    { 37, "Sole Prop(1A) Financials.xlsx",     "Sole Prop Financials.xlsx",        @"Financial Data\DraftworX Files" },
                    { 38, "Sole Prop(105) Financials.xlsx",    "Sole Prop Financials.xlsx",        @"Financial Data\DraftworX Files" },
                    { 39, "LLP(1A) Financials.xlsx",           "LLP Financials.xlsx",              @"Financial Data\DraftworX Files" },
                    // Legacy type group: Afrikaans variants → canonical English name
                    { 40, "Body Corporate_Afrikaans.xlsx",     "Body Corporate Financials.xlsx",   @"Financial Data\DraftworX Files" },
                    { 41, "Close Corporation_Afrikaans.xlsx",  "Close Corporation Financials.xlsx",@"Financial Data\DraftworX Files" },
                    { 42, "IFRS SME_Afrikaans.xlsx",           "IFRS SME Financials.xlsx",         @"Financial Data\DraftworX Files" },
                    { 43, "Partnership_Afrikaans.xlsx",        "Partnership Financials.xlsx",      @"Financial Data\DraftworX Files" },
                    { 44, "Sole Proprietor_Afrikaans.xlsx",    "Sole Proprietor Financials.xlsx",  @"Financial Data\DraftworX Files" },
                    { 45, "Trust Financials _Afrikaans.xlsx",  "Trust Financials.xlsx",            @"Financial Data\DraftworX Files" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "MasterFileAliases",
                keyColumn: "Id",
                keyValues: new object[] { 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28,
                                          29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39,
                                          40, 41, 42, 43, 44, 45 });
        }
    }
}
