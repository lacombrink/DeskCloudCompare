using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeskCloudCompare.Migrations
{
    /// <inheritdoc />
    public partial class SeedInitialData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Folder Types (Id=1 "Canonical" already seeded by InitialCreate)
            migrationBuilder.Sql("INSERT OR IGNORE INTO FolderTypes (Id, Name) VALUES (2, 'Cloud');");
            migrationBuilder.Sql("INSERT OR IGNORE INTO FolderTypes (Id, Name) VALUES (3, 'Desktop');");

            // ---------------------------------------------------------------
            // Path Translation Rules: Desktop (Id=3) → Canonical (Id=1)
            // Applied in SortOrder sequence to each relative file path.
            // ---------------------------------------------------------------

            // --- Step 1-4: Special first-level folder mappings (826.xx → GB, 000.AA → ZZ) ---
            // Must run before the generic numeric-prefix strip rules below.
            migrationBuilder.Sql("INSERT OR IGNORE INTO PathTranslationRules (FromTypeId, ToTypeId, FindText, ReplaceText, SortOrder) VALUES (3, 1, '000.AA', 'ZZ', 1);");
            migrationBuilder.Sql("INSERT OR IGNORE INTO PathTranslationRules (FromTypeId, ToTypeId, FindText, ReplaceText, SortOrder) VALUES (3, 1, '826.EW', 'GB', 2);");
            migrationBuilder.Sql("INSERT OR IGNORE INTO PathTranslationRules (FromTypeId, ToTypeId, FindText, ReplaceText, SortOrder) VALUES (3, 1, '826.NI', 'GB', 3);");
            migrationBuilder.Sql("INSERT OR IGNORE INTO PathTranslationRules (FromTypeId, ToTypeId, FindText, ReplaceText, SortOrder) VALUES (3, 1, '826.SC', 'GB', 4);");

            // --- Step 10-27: Strip numeric country-code prefixes (e.g. "072." → "") ---
            // Turns "072.BW" → "BW", "124.CA" → "CA", etc.
            migrationBuilder.Sql("INSERT OR IGNORE INTO PathTranslationRules (FromTypeId, ToTypeId, FindText, ReplaceText, SortOrder) VALUES (3, 1, '072.', '', 10);");
            migrationBuilder.Sql("INSERT OR IGNORE INTO PathTranslationRules (FromTypeId, ToTypeId, FindText, ReplaceText, SortOrder) VALUES (3, 1, '124.', '', 11);");
            migrationBuilder.Sql("INSERT OR IGNORE INTO PathTranslationRules (FromTypeId, ToTypeId, FindText, ReplaceText, SortOrder) VALUES (3, 1, '288.', '', 12);");
            migrationBuilder.Sql("INSERT OR IGNORE INTO PathTranslationRules (FromTypeId, ToTypeId, FindText, ReplaceText, SortOrder) VALUES (3, 1, '372.', '', 13);");
            migrationBuilder.Sql("INSERT OR IGNORE INTO PathTranslationRules (FromTypeId, ToTypeId, FindText, ReplaceText, SortOrder) VALUES (3, 1, '404.', '', 14);");
            migrationBuilder.Sql("INSERT OR IGNORE INTO PathTranslationRules (FromTypeId, ToTypeId, FindText, ReplaceText, SortOrder) VALUES (3, 1, '430.', '', 15);");
            migrationBuilder.Sql("INSERT OR IGNORE INTO PathTranslationRules (FromTypeId, ToTypeId, FindText, ReplaceText, SortOrder) VALUES (3, 1, '480.', '', 16);");
            migrationBuilder.Sql("INSERT OR IGNORE INTO PathTranslationRules (FromTypeId, ToTypeId, FindText, ReplaceText, SortOrder) VALUES (3, 1, '516.', '', 17);");
            migrationBuilder.Sql("INSERT OR IGNORE INTO PathTranslationRules (FromTypeId, ToTypeId, FindText, ReplaceText, SortOrder) VALUES (3, 1, '566.', '', 18);");
            migrationBuilder.Sql("INSERT OR IGNORE INTO PathTranslationRules (FromTypeId, ToTypeId, FindText, ReplaceText, SortOrder) VALUES (3, 1, '646.', '', 19);");
            migrationBuilder.Sql("INSERT OR IGNORE INTO PathTranslationRules (FromTypeId, ToTypeId, FindText, ReplaceText, SortOrder) VALUES (3, 1, '682.', '', 20);");
            migrationBuilder.Sql("INSERT OR IGNORE INTO PathTranslationRules (FromTypeId, ToTypeId, FindText, ReplaceText, SortOrder) VALUES (3, 1, '710.', '', 21);");
            migrationBuilder.Sql("INSERT OR IGNORE INTO PathTranslationRules (FromTypeId, ToTypeId, FindText, ReplaceText, SortOrder) VALUES (3, 1, '748.', '', 22);");
            migrationBuilder.Sql("INSERT OR IGNORE INTO PathTranslationRules (FromTypeId, ToTypeId, FindText, ReplaceText, SortOrder) VALUES (3, 1, '800.', '', 23);");
            migrationBuilder.Sql("INSERT OR IGNORE INTO PathTranslationRules (FromTypeId, ToTypeId, FindText, ReplaceText, SortOrder) VALUES (3, 1, '831.', '', 24);");
            migrationBuilder.Sql("INSERT OR IGNORE INTO PathTranslationRules (FromTypeId, ToTypeId, FindText, ReplaceText, SortOrder) VALUES (3, 1, '832.', '', 25);");
            migrationBuilder.Sql("INSERT OR IGNORE INTO PathTranslationRules (FromTypeId, ToTypeId, FindText, ReplaceText, SortOrder) VALUES (3, 1, '834.', '', 26);");
            migrationBuilder.Sql("INSERT OR IGNORE INTO PathTranslationRules (FromTypeId, ToTypeId, FindText, ReplaceText, SortOrder) VALUES (3, 1, '894.', '', 27);");

            // --- Step 30: Strip the "NewUserData" extra folder level ---
            // Desktop: \BW\NewUserData\Frameworks\... → \BW\Frameworks\...
            migrationBuilder.Sql(@"INSERT OR IGNORE INTO PathTranslationRules (FromTypeId, ToTypeId, FindText, ReplaceText, SortOrder) VALUES (3, 1, '\NewUserData\', '\', 30);");

            // --- Step 31: Strip "DraftworX Files" subfolder ---
            // Desktop: \Financial Data\DraftworX Files\file.xlsx → \Financial Data\file.xlsx
            // Also covers Audit Programs\DraftworX Files\ in methodologies.
            migrationBuilder.Sql(@"INSERT OR IGNORE INTO PathTranslationRules (FromTypeId, ToTypeId, FindText, ReplaceText, SortOrder) VALUES (3, 1, '\DraftworX Files', '', 31);");

            // --- Step 32: Rename methodology template folder ---
            // Desktop: \TemplateData\ → Cloud: \#Templates\
            migrationBuilder.Sql(@"INSERT OR IGNORE INTO PathTranslationRules (FromTypeId, ToTypeId, FindText, ReplaceText, SortOrder) VALUES (3, 1, '\TemplateData\', '\#Templates\', 32);");

            // --- Steps 40-58: Strip country-code suffix from framework folder names ---
            // Desktop: "Body Corp+ BW\" → Cloud: "Body Corp+\"
            // The leading space prevents matching the country code in the path root (e.g. \BW\ is safe).
            migrationBuilder.Sql(@"INSERT OR IGNORE INTO PathTranslationRules (FromTypeId, ToTypeId, FindText, ReplaceText, SortOrder) VALUES (3, 1, ' BW\', '\', 40);");
            migrationBuilder.Sql(@"INSERT OR IGNORE INTO PathTranslationRules (FromTypeId, ToTypeId, FindText, ReplaceText, SortOrder) VALUES (3, 1, ' CA\', '\', 41);");
            migrationBuilder.Sql(@"INSERT OR IGNORE INTO PathTranslationRules (FromTypeId, ToTypeId, FindText, ReplaceText, SortOrder) VALUES (3, 1, ' GH\', '\', 42);");
            migrationBuilder.Sql(@"INSERT OR IGNORE INTO PathTranslationRules (FromTypeId, ToTypeId, FindText, ReplaceText, SortOrder) VALUES (3, 1, ' IE\', '\', 43);");
            migrationBuilder.Sql(@"INSERT OR IGNORE INTO PathTranslationRules (FromTypeId, ToTypeId, FindText, ReplaceText, SortOrder) VALUES (3, 1, ' KE\', '\', 44);");
            migrationBuilder.Sql(@"INSERT OR IGNORE INTO PathTranslationRules (FromTypeId, ToTypeId, FindText, ReplaceText, SortOrder) VALUES (3, 1, ' LR\', '\', 45);");
            migrationBuilder.Sql(@"INSERT OR IGNORE INTO PathTranslationRules (FromTypeId, ToTypeId, FindText, ReplaceText, SortOrder) VALUES (3, 1, ' MU\', '\', 46);");
            migrationBuilder.Sql(@"INSERT OR IGNORE INTO PathTranslationRules (FromTypeId, ToTypeId, FindText, ReplaceText, SortOrder) VALUES (3, 1, ' NA\', '\', 47);");
            migrationBuilder.Sql(@"INSERT OR IGNORE INTO PathTranslationRules (FromTypeId, ToTypeId, FindText, ReplaceText, SortOrder) VALUES (3, 1, ' NG\', '\', 48);");
            migrationBuilder.Sql(@"INSERT OR IGNORE INTO PathTranslationRules (FromTypeId, ToTypeId, FindText, ReplaceText, SortOrder) VALUES (3, 1, ' RW\', '\', 49);");
            migrationBuilder.Sql(@"INSERT OR IGNORE INTO PathTranslationRules (FromTypeId, ToTypeId, FindText, ReplaceText, SortOrder) VALUES (3, 1, ' SA\', '\', 50);");
            migrationBuilder.Sql(@"INSERT OR IGNORE INTO PathTranslationRules (FromTypeId, ToTypeId, FindText, ReplaceText, SortOrder) VALUES (3, 1, ' ZA\', '\', 51);");
            migrationBuilder.Sql(@"INSERT OR IGNORE INTO PathTranslationRules (FromTypeId, ToTypeId, FindText, ReplaceText, SortOrder) VALUES (3, 1, ' SZ\', '\', 52);");
            migrationBuilder.Sql(@"INSERT OR IGNORE INTO PathTranslationRules (FromTypeId, ToTypeId, FindText, ReplaceText, SortOrder) VALUES (3, 1, ' UG\', '\', 53);");
            migrationBuilder.Sql(@"INSERT OR IGNORE INTO PathTranslationRules (FromTypeId, ToTypeId, FindText, ReplaceText, SortOrder) VALUES (3, 1, ' GG\', '\', 54);");
            migrationBuilder.Sql(@"INSERT OR IGNORE INTO PathTranslationRules (FromTypeId, ToTypeId, FindText, ReplaceText, SortOrder) VALUES (3, 1, ' JE\', '\', 55);");
            migrationBuilder.Sql(@"INSERT OR IGNORE INTO PathTranslationRules (FromTypeId, ToTypeId, FindText, ReplaceText, SortOrder) VALUES (3, 1, ' TZ\', '\', 56);");
            migrationBuilder.Sql(@"INSERT OR IGNORE INTO PathTranslationRules (FromTypeId, ToTypeId, FindText, ReplaceText, SortOrder) VALUES (3, 1, ' ZM\', '\', 57);");
            migrationBuilder.Sql(@"INSERT OR IGNORE INTO PathTranslationRules (FromTypeId, ToTypeId, FindText, ReplaceText, SortOrder) VALUES (3, 1, ' AA\', '\', 58);");

            // ---------------------------------------------------------------
            // Default Preset: "All Countries — Cloud vs Desktop"
            // Slot A = CloudProduction root, Slot B = CleanInstallSet root
            // ---------------------------------------------------------------
            migrationBuilder.Sql("INSERT OR IGNORE INTO FolderPresets (Id, Name) VALUES (1, 'All Countries — Cloud vs Desktop');");
            migrationBuilder.Sql(@"INSERT OR IGNORE INTO FolderPresetSlots (PresetId, SlotLabel, FolderPath, FolderTypeId) VALUES (1, 'A', 'C:\Users\Public\CloudProduction', 2);");
            migrationBuilder.Sql(@"INSERT OR IGNORE INTO FolderPresetSlots (PresetId, SlotLabel, FolderPath, FolderTypeId) VALUES (1, 'B', 'C:\Users\Public\CleanInstallSet', 3);");
            migrationBuilder.Sql("INSERT OR IGNORE INTO FolderPresetSlots (PresetId, SlotLabel, FolderPath, FolderTypeId) VALUES (1, 'C', NULL, NULL);");
            migrationBuilder.Sql("INSERT OR IGNORE INTO FolderPresetSlots (PresetId, SlotLabel, FolderPath, FolderTypeId) VALUES (1, 'D', NULL, NULL);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM FolderPresetSlots WHERE PresetId = 1;");
            migrationBuilder.Sql("DELETE FROM FolderPresets WHERE Id = 1;");
            migrationBuilder.Sql("DELETE FROM PathTranslationRules WHERE FromTypeId = 3 AND ToTypeId = 1;");
            migrationBuilder.Sql("DELETE FROM FolderTypes WHERE Id IN (2, 3);");
        }
    }
}
