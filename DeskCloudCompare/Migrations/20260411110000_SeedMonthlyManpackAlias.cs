using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814

namespace DeskCloudCompare.Migrations
{
    /// <inheritdoc />
    public partial class SeedMonthlyManpackAlias : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Guard against re-insertion if the row already exists (e.g. leftover from a
            // failed previous migration run that wasn't recorded in __EFMigrationsHistory).
            migrationBuilder.Sql("DELETE FROM MasterFileAliases WHERE Id = 50");

            migrationBuilder.InsertData(
                table: "MasterFileAliases",
                columns: new[] { "Id", "ActualFileName", "CanonicalFileName", "FolderPath" },
                values: new object[,]
                {
                    { 50, "Monthly+ Manpack.xlsx", "IFRS+ Financials.xlsx", @"Financial Data\DraftworX Files" },
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "MasterFileAliases",
                keyColumn: "Id",
                keyValue: 50);
        }
    }
}
