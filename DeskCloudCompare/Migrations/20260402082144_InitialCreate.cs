using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeskCloudCompare.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FolderPresets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FolderPresets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FolderTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FolderTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FolderPresetSlots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PresetId = table.Column<int>(type: "INTEGER", nullable: false),
                    SlotLabel = table.Column<string>(type: "TEXT", nullable: false),
                    FolderPath = table.Column<string>(type: "TEXT", nullable: true),
                    FolderTypeId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FolderPresetSlots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FolderPresetSlots_FolderPresets_PresetId",
                        column: x => x.PresetId,
                        principalTable: "FolderPresets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FolderPresetSlots_FolderTypes_FolderTypeId",
                        column: x => x.FolderTypeId,
                        principalTable: "FolderTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "PathTranslationRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FromTypeId = table.Column<int>(type: "INTEGER", nullable: false),
                    ToTypeId = table.Column<int>(type: "INTEGER", nullable: false),
                    FindText = table.Column<string>(type: "TEXT", nullable: false),
                    ReplaceText = table.Column<string>(type: "TEXT", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PathTranslationRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PathTranslationRules_FolderTypes_FromTypeId",
                        column: x => x.FromTypeId,
                        principalTable: "FolderTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PathTranslationRules_FolderTypes_ToTypeId",
                        column: x => x.ToTypeId,
                        principalTable: "FolderTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "FolderTypes",
                columns: new[] { "Id", "Name" },
                values: new object[] { 1, "Canonical" });

            migrationBuilder.CreateIndex(
                name: "IX_FolderPresetSlots_FolderTypeId",
                table: "FolderPresetSlots",
                column: "FolderTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_FolderPresetSlots_PresetId",
                table: "FolderPresetSlots",
                column: "PresetId");

            migrationBuilder.CreateIndex(
                name: "IX_FolderTypes_Name",
                table: "FolderTypes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PathTranslationRules_FromTypeId",
                table: "PathTranslationRules",
                column: "FromTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_PathTranslationRules_ToTypeId",
                table: "PathTranslationRules",
                column: "ToTypeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FolderPresetSlots");

            migrationBuilder.DropTable(
                name: "PathTranslationRules");

            migrationBuilder.DropTable(
                name: "FolderPresets");

            migrationBuilder.DropTable(
                name: "FolderTypes");
        }
    }
}
