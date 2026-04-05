using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeskCloudCompare.Migrations
{
    /// <inheritdoc />
    public partial class AddCountryManager : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CanonicalFrameworks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Category = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CanonicalFrameworks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CountryEntries",
                columns: table => new
                {
                    Code = table.Column<string>(type: "TEXT", nullable: false),
                    RawFolderName = table.Column<string>(type: "TEXT", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CountryEntries", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "CountryManagerSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RootFolderPath = table.Column<string>(type: "TEXT", nullable: false),
                    MasterCountryCode = table.Column<string>(type: "TEXT", nullable: true),
                    LastScanned = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CountryManagerSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CanonicalFiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CanonicalFrameworkId = table.Column<int>(type: "INTEGER", nullable: false),
                    RelativePath = table.Column<string>(type: "TEXT", nullable: false),
                    FileName = table.Column<string>(type: "TEXT", nullable: false),
                    IsDxdb = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsFinancialData = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CanonicalFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CanonicalFiles_CanonicalFrameworks_CanonicalFrameworkId",
                        column: x => x.CanonicalFrameworkId,
                        principalTable: "CanonicalFrameworks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CountryFrameworkPresences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CanonicalFrameworkId = table.Column<int>(type: "INTEGER", nullable: false),
                    CountryCode = table.Column<string>(type: "TEXT", nullable: false),
                    ActualFolderName = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CountryFrameworkPresences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CountryFrameworkPresences_CanonicalFrameworks_CanonicalFrameworkId",
                        column: x => x.CanonicalFrameworkId,
                        principalTable: "CanonicalFrameworks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CountryFilePresences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CanonicalFileId = table.Column<int>(type: "INTEGER", nullable: false),
                    CountryCode = table.Column<string>(type: "TEXT", nullable: false),
                    BinaryHash = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CountryFilePresences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CountryFilePresences_CanonicalFiles_CanonicalFileId",
                        column: x => x.CanonicalFileId,
                        principalTable: "CanonicalFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CanonicalFiles_CanonicalFrameworkId",
                table: "CanonicalFiles",
                column: "CanonicalFrameworkId");

            migrationBuilder.CreateIndex(
                name: "IX_CanonicalFrameworks_Name_Category",
                table: "CanonicalFrameworks",
                columns: new[] { "Name", "Category" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CountryFilePresences_CanonicalFileId",
                table: "CountryFilePresences",
                column: "CanonicalFileId");

            migrationBuilder.CreateIndex(
                name: "IX_CountryFrameworkPresences_CanonicalFrameworkId",
                table: "CountryFrameworkPresences",
                column: "CanonicalFrameworkId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CountryEntries");

            migrationBuilder.DropTable(
                name: "CountryFilePresences");

            migrationBuilder.DropTable(
                name: "CountryFrameworkPresences");

            migrationBuilder.DropTable(
                name: "CountryManagerSettings");

            migrationBuilder.DropTable(
                name: "CanonicalFiles");

            migrationBuilder.DropTable(
                name: "CanonicalFrameworks");
        }
    }
}
