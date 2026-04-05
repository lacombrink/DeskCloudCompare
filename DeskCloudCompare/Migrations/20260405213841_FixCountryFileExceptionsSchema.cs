using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeskCloudCompare.Migrations
{
    /// <inheritdoc />
    public partial class FixCountryFileExceptionsSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The previous migration (RekeyCountryFileExceptionsByName) was generated empty
            // and recorded in __EFMigrationsHistory before it was fixed, so it never ran.
            //
            // Uses SQLite's temp-table rename pattern (no ALTER COLUMN support).
            // Safe regardless of which schema is currently present:
            //   old schema (CanonicalFileId): dropped and replaced
            //   new schema already in place:  dropped and recreated (no important data lost)

            migrationBuilder.Sql(@"
                CREATE TABLE CountryFileExceptions_new (
                    Id                INTEGER PRIMARY KEY AUTOINCREMENT,
                    FrameworkName     TEXT NOT NULL DEFAULT '',
                    FrameworkCategory INTEGER NOT NULL DEFAULT 0,
                    RelativePath      TEXT NOT NULL DEFAULT '',
                    CountryCode       TEXT NOT NULL DEFAULT ''
                );
            ");

            migrationBuilder.Sql("DROP TABLE IF EXISTS CountryFileExceptions;");

            migrationBuilder.Sql(
                "ALTER TABLE CountryFileExceptions_new RENAME TO CountryFileExceptions;");

            migrationBuilder.Sql(@"
                CREATE UNIQUE INDEX IF NOT EXISTS
                    IX_CountryFileExceptions_FrameworkName_FrameworkCategory_RelativePath_CountryCode
                ON CountryFileExceptions
                    (FrameworkName, FrameworkCategory, RelativePath, CountryCode);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CountryFileExceptions_FrameworkName_FrameworkCategory_RelativePath_CountryCode",
                table: "CountryFileExceptions");

            migrationBuilder.DropColumn(
                name: "FrameworkName",
                table: "CountryFileExceptions");

            migrationBuilder.DropColumn(
                name: "RelativePath",
                table: "CountryFileExceptions");

            migrationBuilder.RenameColumn(
                name: "FrameworkCategory",
                table: "CountryFileExceptions",
                newName: "CanonicalFileId");

            migrationBuilder.CreateIndex(
                name: "IX_CountryFileExceptions_CanonicalFileId_CountryCode",
                table: "CountryFileExceptions",
                columns: new[] { "CanonicalFileId", "CountryCode" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CountryFileExceptions_CanonicalFiles_CanonicalFileId",
                table: "CountryFileExceptions",
                column: "CanonicalFileId",
                principalTable: "CanonicalFiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
