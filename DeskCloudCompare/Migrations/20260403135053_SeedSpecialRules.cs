using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeskCloudCompare.Migrations
{
    /// <inheritdoc />
    public partial class SeedSpecialRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DxdbCsvMappings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    DxdbFilePattern = table.Column<string>(type: "TEXT", nullable: false),
                    CsvFilePattern = table.Column<string>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DxdbCsvMappings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SpecialFileRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    FileNamePattern = table.Column<string>(type: "TEXT", nullable: false),
                    RuleType = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpecialFileRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FieldMappings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DxdbCsvMappingId = table.Column<int>(type: "INTEGER", nullable: false),
                    DxdbTableName = table.Column<string>(type: "TEXT", nullable: false),
                    DxdbColumnName = table.Column<string>(type: "TEXT", nullable: false),
                    CsvColumnName = table.Column<string>(type: "TEXT", nullable: false),
                    IsKeyField = table.Column<bool>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FieldMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FieldMappings_DxdbCsvMappings_DxdbCsvMappingId",
                        column: x => x.DxdbCsvMappingId,
                        principalTable: "DxdbCsvMappings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FieldMappings_DxdbCsvMappingId",
                table: "FieldMappings",
                column: "DxdbCsvMappingId");

            // Seed SpecialFileRules
            // RuleType: 0 = OneToMany, 1 = FormatConversion
            migrationBuilder.Sql(@"INSERT OR IGNORE INTO SpecialFileRules (Id, Description, FileNamePattern, RuleType, IsActive, Notes)
VALUES
(1, 'LeadTemp — broadcast template',   'LeadTemp.xlsx',      0, 1, 'Exists once per country in Desktop (NewUserDataUpdates). Copied to every framework #Updates folder in Cloud. Show summary row: 1 Desktop source vs N Cloud copies.'),
(2, 'FindReplace — settings database', 'FindReplace.dxdb',   1, 1, 'Encrypted SQLite in Desktop. Cloud equivalent is a CSV in country #Settings folder. Excluded from file compare; will be matched in Data Compare tab when decryption key is available.'),
(3, 'FindReplaceCS — CS settings db',  'FindReplaceCS.dxdb', 1, 1, 'Encrypted SQLite in Desktop. Cloud equivalent is a CSV in country #Settings folder. Excluded from file compare; will be matched in Data Compare tab when decryption key is available.');");

            // Seed DxdbCsvMappings (schema ready for future Data Compare)
            migrationBuilder.Sql(@"INSERT OR IGNORE INTO DxdbCsvMappings (Id, Description, DxdbFilePattern, CsvFilePattern, IsActive, Notes)
VALUES
(1, 'FindReplace settings',   '*\FindReplace.dxdb',   '*\#Settings\*.csv', 1, 'Awaiting decryption key. Map dxdb table/columns to CSV columns once schema is known.'),
(2, 'FindReplaceCS settings', '*\FindReplaceCS.dxdb', '*\#Settings\*.csv', 1, 'Awaiting decryption key. Map dxdb table/columns to CSV columns once schema is known.');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FieldMappings");

            migrationBuilder.DropTable(
                name: "SpecialFileRules");

            migrationBuilder.DropTable(
                name: "DxdbCsvMappings");
        }
    }
}
