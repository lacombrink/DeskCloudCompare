using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814

namespace DeskCloudCompare.Migrations
{
    /// <inheritdoc />
    public partial class SeedFinancialFileAliases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "MasterFileAliases",
                columns: new[] { "Id", "ActualFileName", "CanonicalFileName", "FolderPath" },
                values: new object[,]
                {
                    // IFRS type: consolidation → IFRS+ Consolidation.xlsx
                    { 16, "IFRS SME+ Consolidation.xlsx",  "IFRS+ Consolidation.xlsx",          @"Financial Data\DraftworX Files" },
                    { 17, "IFRS Consolidation.xlsx",        "IFRS+ Consolidation.xlsx",          @"Financial Data\DraftworX Files" },
                    // IFRS type: entity financials → IFRS+ Financials.xlsx
                    { 18, "ASPE+ Financials.xlsx",          "IFRS+ Financials.xlsx",             @"Financial Data\DraftworX Files" },
                    { 19, "IFRS SME+ Financials.xlsx",      "IFRS+ Financials.xlsx",             @"Financial Data\DraftworX Files" },
                    { 20, "IFRS Financials.xlsx",           "IFRS+ Financials.xlsx",             @"Financial Data\DraftworX Files" },
                    { 21, "Body Corp+ Financials.xlsx",     "IFRS+ Financials.xlsx",             @"Financial Data\DraftworX Files" },
                    { 22, "CC+ Financials.xlsx",            "IFRS+ Financials.xlsx",             @"Financial Data\DraftworX Files" },
                    { 23, "NPC+ Financials.xlsx",           "IFRS+ Financials.xlsx",             @"Financial Data\DraftworX Files" },
                    { 24, "NPO+ Financials.xlsx",           "IFRS+ Financials.xlsx",             @"Financial Data\DraftworX Files" },
                    { 25, "Partnership+ Financials.xlsx",   "IFRS+ Financials.xlsx",             @"Financial Data\DraftworX Files" },
                    { 26, "School+ Financials.xlsx",        "IFRS+ Financials.xlsx",             @"Financial Data\DraftworX Files" },
                    { 27, "Sole Prop+ Financials.xlsx",     "IFRS+ Financials.xlsx",             @"Financial Data\DraftworX Files" },
                    { 28, "Trust+ Financials.xlsx",         "IFRS+ Financials.xlsx",             @"Financial Data\DraftworX Files" },
                    // FRS type: company consolidation → FRS102 Consolidation.xlsx
                    { 29, "FRS102(1A) Consolidation.xlsx",  "FRS102 Consolidation.xlsx",         @"Financial Data\DraftworX Files" },
                    // FRS type: company financials → FRS102 Financials.xlsx
                    { 30, "FRS102(1A) Financials.xlsx",     "FRS102 Financials.xlsx",            @"Financial Data\DraftworX Files" },
                    { 31, "FRS105 Financials.xlsx",         "FRS102 Financials.xlsx",            @"Financial Data\DraftworX Files" },
                    { 32, "FRS101 Financials.xlsx",         "FRS102 Financials.xlsx",            @"Financial Data\DraftworX Files" },
                    // FRS type: partnership financials → Partnership Financials.xlsx
                    { 33, "Partnership(1A) Financials.xlsx","Partnership Financials.xlsx",       @"Financial Data\DraftworX Files" },
                    { 34, "Partnership(105) Financials.xlsx","Partnership Financials.xlsx",      @"Financial Data\DraftworX Files" },
                    // FRS type: sole prop financials → Sole Prop Financials.xlsx
                    { 35, "Sole Prop(1A) Financials.xlsx",  "Sole Prop Financials.xlsx",         @"Financial Data\DraftworX Files" },
                    { 36, "Sole Prop(105) Financials.xlsx", "Sole Prop Financials.xlsx",         @"Financial Data\DraftworX Files" },
                    // FRS type: LLP → LLP Financials.xlsx
                    { 37, "LLP(1A) Financials.xlsx",        "LLP Financials.xlsx",               @"Financial Data\DraftworX Files" },
                    // Legacy: Afrikaans variants → canonical English name
                    { 38, "Body Corporate_Afrikaans.xlsx",   "Body Corporate Financials.xlsx",   @"Financial Data\DraftworX Files" },
                    { 39, "Close Corporation_Afrikaans.xlsx","Close Corporation Financials.xlsx", @"Financial Data\DraftworX Files" },
                    { 40, "IFRS SME_Afrikaans.xlsx",         "IFRS SME Financials.xlsx",         @"Financial Data\DraftworX Files" },
                    { 41, "Partnership_Afrikaans.xlsx",      "Partnership Financials.xlsx",       @"Financial Data\DraftworX Files" },
                    { 42, "Sole Proprietor_Afrikaans.xlsx",  "Sole Proprietor Financials.xlsx",  @"Financial Data\DraftworX Files" },
                    { 43, "Trust Financials _Afrikaans.xlsx","Trust Financials.xlsx",             @"Financial Data\DraftworX Files" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "MasterFileAliases",
                keyColumn: "Id",
                keyValues: new object[] { 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28,
                                          29, 30, 31, 32, 33, 34, 35, 36, 37,
                                          38, 39, 40, 41, 42, 43 });
        }
    }
}
