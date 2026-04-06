using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeskCloudCompare.Migrations
{
    /// <inheritdoc />
    public partial class AddSubFrameworkManager : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SubGroup",
                table: "MasterFrameworkEntries",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SubFrameworkFileExceptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SubGroup = table.Column<int>(type: "INTEGER", nullable: false),
                    RelativePath = table.Column<string>(type: "TEXT", nullable: false),
                    FrameworkCanonicalName = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubFrameworkFileExceptions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SubFrameworkFileExceptions_SubGroup_RelativePath_FrameworkCanonicalName",
                table: "SubFrameworkFileExceptions",
                columns: new[] { "SubGroup", "RelativePath", "FrameworkCanonicalName" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SubFrameworkFileExceptions");

            migrationBuilder.DropColumn(
                name: "SubGroup",
                table: "MasterFrameworkEntries");
        }
    }
}
