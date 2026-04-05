using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeskCloudCompare.Migrations
{
    /// <inheritdoc />
    public partial class RekeyCountryFileExceptionsByName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop the old FK-based table (CanonicalFileId + CountryCode)
            migrationBuilder.DropTable(name: "CountryFileExceptions");

            // Recreate with stable name-based identity — survives rescans
            migrationBuilder.CreateTable(
                name: "CountryFileExceptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FrameworkName     = table.Column<string>(type: "TEXT", nullable: false),
                    FrameworkCategory = table.Column<int>(type: "INTEGER", nullable: false),
                    RelativePath      = table.Column<string>(type: "TEXT", nullable: false),
                    CountryCode       = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CountryFileExceptions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CountryFileExceptions_FrameworkName_FrameworkCategory_RelativePath_CountryCode",
                table: "CountryFileExceptions",
                columns: new[] { "FrameworkName", "FrameworkCategory", "RelativePath", "CountryCode" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "CountryFileExceptions");

            migrationBuilder.CreateTable(
                name: "CountryFileExceptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CanonicalFileId = table.Column<int>(type: "INTEGER", nullable: false),
                    CountryCode     = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CountryFileExceptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CountryFileExceptions_CanonicalFiles_CanonicalFileId",
                        column: x => x.CanonicalFileId,
                        principalTable: "CanonicalFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CountryFileExceptions_CanonicalFileId_CountryCode",
                table: "CountryFileExceptions",
                columns: new[] { "CanonicalFileId", "CountryCode" },
                unique: true);
        }
    }
}
