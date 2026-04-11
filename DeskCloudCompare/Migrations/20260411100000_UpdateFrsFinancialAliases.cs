using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814

namespace DeskCloudCompare.Migrations
{
    /// <inheritdoc />
    public partial class UpdateFrsFinancialAliases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Update IDs 35-39, 43: change canonical from intermediate Partnership/SoleProp/LLP
            // Financials.xlsx → directly to FRS102 Financials.xlsx (aliases are single-level).
            // Also add IDs 46-49: map the entity financial file names used in Charity/LLP/
            // Partnership/Sole Prop sub-frameworks directly to FRS102 Financials.xlsx.

            migrationBuilder.DeleteData(
                table: "MasterFileAliases",
                keyColumn: "Id",
                keyValues: new object[] { 35, 36, 37, 38, 39, 43 });

            migrationBuilder.InsertData(
                table: "MasterFileAliases",
                columns: new[] { "Id", "ActualFileName", "CanonicalFileName", "FolderPath" },
                values: new object[,]
                {
                    { 35, "Partnership(1A) Financials.xlsx",  "FRS102 Financials.xlsx", @"Financial Data\DraftworX Files" },
                    { 36, "Partnership(105) Financials.xlsx", "FRS102 Financials.xlsx", @"Financial Data\DraftworX Files" },
                    { 37, "Sole Prop(1A) Financials.xlsx",    "FRS102 Financials.xlsx", @"Financial Data\DraftworX Files" },
                    { 38, "Sole Prop(105) Financials.xlsx",   "FRS102 Financials.xlsx", @"Financial Data\DraftworX Files" },
                    { 39, "LLP(1A) Financials.xlsx",          "FRS102 Financials.xlsx", @"Financial Data\DraftworX Files" },
                    { 43, "Partnership_Afrikaans.xlsx",        "FRS102 Financials.xlsx", @"Financial Data\DraftworX Files" },
                    { 46, "Charity Financials.xlsx",           "FRS102 Financials.xlsx", @"Financial Data\DraftworX Files" },
                    { 47, "LLP Financials.xlsx",               "FRS102 Financials.xlsx", @"Financial Data\DraftworX Files" },
                    { 48, "Partnership Financials.xlsx",       "FRS102 Financials.xlsx", @"Financial Data\DraftworX Files" },
                    { 49, "Sole Prop Financials.xlsx",         "FRS102 Financials.xlsx", @"Financial Data\DraftworX Files" },
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "MasterFileAliases",
                keyColumn: "Id",
                keyValues: new object[] { 35, 36, 37, 38, 39, 43, 46, 47, 48, 49 });

            migrationBuilder.InsertData(
                table: "MasterFileAliases",
                columns: new[] { "Id", "ActualFileName", "CanonicalFileName", "FolderPath" },
                values: new object[,]
                {
                    { 35, "Partnership(1A) Financials.xlsx",  "Partnership Financials.xlsx", @"Financial Data\DraftworX Files" },
                    { 36, "Partnership(105) Financials.xlsx", "Partnership Financials.xlsx", @"Financial Data\DraftworX Files" },
                    { 37, "Sole Prop(1A) Financials.xlsx",    "Sole Prop Financials.xlsx",   @"Financial Data\DraftworX Files" },
                    { 38, "Sole Prop(105) Financials.xlsx",   "Sole Prop Financials.xlsx",   @"Financial Data\DraftworX Files" },
                    { 39, "LLP(1A) Financials.xlsx",          "LLP Financials.xlsx",         @"Financial Data\DraftworX Files" },
                    { 43, "Partnership_Afrikaans.xlsx",        "Partnership Financials.xlsx", @"Financial Data\DraftworX Files" },
                });
        }
    }
}
