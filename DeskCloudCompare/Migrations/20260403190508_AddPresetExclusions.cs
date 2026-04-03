using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeskCloudCompare.Migrations
{
    /// <inheritdoc />
    public partial class AddPresetExclusions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PresetExclusions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PresetId = table.Column<int>(type: "INTEGER", nullable: false),
                    Pattern = table.Column<string>(type: "TEXT", nullable: false),
                    MatchType = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PresetExclusions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PresetExclusions_FolderPresets_PresetId",
                        column: x => x.PresetId,
                        principalTable: "FolderPresets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PresetExclusions_PresetId",
                table: "PresetExclusions",
                column: "PresetId");

            // Seed: RAC exists only in Cloud ZA — exclude from the default preset
            // MatchType: 0=Contains, 1=StartsWith, 2=EndsWith
            // PresetId 1 = "All Countries — Cloud vs Desktop" (seeded in SeedInitialData)
            migrationBuilder.Sql(@"INSERT OR IGNORE INTO PresetExclusions
                (PresetId, Pattern, MatchType, Description, IsActive)
            VALUES
                (1, 'ZA\Frameworks\RAC', 0, 'RAC framework — Cloud ZA only, no Desktop equivalent', 1);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PresetExclusions");
        }
    }
}
