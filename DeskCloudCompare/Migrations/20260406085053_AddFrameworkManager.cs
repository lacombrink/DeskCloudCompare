using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DeskCloudCompare.Migrations
{
    /// <inheritdoc />
    public partial class AddFrameworkManager : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FrameworkManagerSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MasterFolderPath = table.Column<string>(type: "TEXT", nullable: false),
                    LastScanned = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FrameworkManagerSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MasterCanonicalFiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TypeGroup = table.Column<int>(type: "INTEGER", nullable: false),
                    RelativePath = table.Column<string>(type: "TEXT", nullable: false),
                    FileName = table.Column<string>(type: "TEXT", nullable: false),
                    IsDxdb = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsFinancialData = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MasterCanonicalFiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MasterFileAliases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FolderPath = table.Column<string>(type: "TEXT", nullable: false),
                    ActualFileName = table.Column<string>(type: "TEXT", nullable: false),
                    CanonicalFileName = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MasterFileAliases", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MasterFileExceptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TypeGroup = table.Column<int>(type: "INTEGER", nullable: false),
                    RelativePath = table.Column<string>(type: "TEXT", nullable: false),
                    FrameworkCanonicalName = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MasterFileExceptions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MasterFrameworkEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CanonicalName = table.Column<string>(type: "TEXT", nullable: false),
                    FolderName = table.Column<string>(type: "TEXT", nullable: false),
                    TypeGroup = table.Column<int>(type: "INTEGER", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MasterFrameworkEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MasterFilePresences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MasterCanonicalFileId = table.Column<int>(type: "INTEGER", nullable: false),
                    MasterFrameworkEntryId = table.Column<int>(type: "INTEGER", nullable: false),
                    ActualFileName = table.Column<string>(type: "TEXT", nullable: false),
                    BinaryHash = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MasterFilePresences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MasterFilePresences_MasterCanonicalFiles_MasterCanonicalFileId",
                        column: x => x.MasterCanonicalFileId,
                        principalTable: "MasterCanonicalFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MasterFilePresences_MasterFrameworkEntries_MasterFrameworkEntryId",
                        column: x => x.MasterFrameworkEntryId,
                        principalTable: "MasterFrameworkEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "MasterFileAliases",
                columns: new[] { "Id", "ActualFileName", "CanonicalFileName", "FolderPath" },
                values: new object[,]
                {
                    { 1, "Director Certificates.xlsx", "Director Certificates.xlsx", "Documents\\DraftworX Files" },
                    { 2, "Partner Certificates.xlsx", "Director Certificates.xlsx", "Documents\\DraftworX Files" },
                    { 3, "Proprietor Certificates.xlsx", "Director Certificates.xlsx", "Documents\\DraftworX Files" },
                    { 4, "Trustee Certificates.xlsx", "Director Certificates.xlsx", "Documents\\DraftworX Files" },
                    { 5, "Member Certificates.xlsx", "Director Certificates.xlsx", "Documents\\DraftworX Files" },
                    { 6, "Directors Minutes-Resolution.xlsx", "Directors Minutes-Resolution.xlsx", "Documents\\DraftworX Files" },
                    { 7, "Partner Minutes-Resolution.xlsx", "Directors Minutes-Resolution.xlsx", "Documents\\DraftworX Files" },
                    { 8, "Proprietor Minutes-Resolution.xlsx", "Directors Minutes-Resolution.xlsx", "Documents\\DraftworX Files" },
                    { 9, "Trustee Minutes-Resolution.xlsx", "Directors Minutes-Resolution.xlsx", "Documents\\DraftworX Files" },
                    { 10, "Member Minutes-Resolution.xlsx", "Directors Minutes-Resolution.xlsx", "Documents\\DraftworX Files" },
                    { 11, "Shareholder Minutes-Resolution.xlsx", "Shareholder Minutes-Resolution.xlsx", "Documents\\DraftworX Files" },
                    { 12, "Partner Minutes-Resolution2.xlsx", "Shareholder Minutes-Resolution.xlsx", "Documents\\DraftworX Files" },
                    { 13, "Proprietor Minutes-Resolution2.xlsx", "Shareholder Minutes-Resolution.xlsx", "Documents\\DraftworX Files" },
                    { 14, "Trustee Minutes-Resolution2.xlsx", "Shareholder Minutes-Resolution.xlsx", "Documents\\DraftworX Files" },
                    { 15, "Member Minutes-Resolution2.xlsx", "Shareholder Minutes-Resolution.xlsx", "Documents\\DraftworX Files" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_MasterCanonicalFiles_TypeGroup_RelativePath",
                table: "MasterCanonicalFiles",
                columns: new[] { "TypeGroup", "RelativePath" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MasterFileAliases_FolderPath_ActualFileName",
                table: "MasterFileAliases",
                columns: new[] { "FolderPath", "ActualFileName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MasterFileExceptions_TypeGroup_RelativePath_FrameworkCanonicalName",
                table: "MasterFileExceptions",
                columns: new[] { "TypeGroup", "RelativePath", "FrameworkCanonicalName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MasterFilePresences_MasterCanonicalFileId_MasterFrameworkEntryId",
                table: "MasterFilePresences",
                columns: new[] { "MasterCanonicalFileId", "MasterFrameworkEntryId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MasterFilePresences_MasterFrameworkEntryId",
                table: "MasterFilePresences",
                column: "MasterFrameworkEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_MasterFrameworkEntries_CanonicalName",
                table: "MasterFrameworkEntries",
                column: "CanonicalName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FrameworkManagerSettings");

            migrationBuilder.DropTable(
                name: "MasterFileAliases");

            migrationBuilder.DropTable(
                name: "MasterFileExceptions");

            migrationBuilder.DropTable(
                name: "MasterFilePresences");

            migrationBuilder.DropTable(
                name: "MasterCanonicalFiles");

            migrationBuilder.DropTable(
                name: "MasterFrameworkEntries");
        }
    }
}
