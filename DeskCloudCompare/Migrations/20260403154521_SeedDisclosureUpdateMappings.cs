using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeskCloudCompare.Migrations
{
    /// <inheritdoc />
    public partial class SeedDisclosureUpdateMappings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // -----------------------------------------------------------------------
            // DxdbCsvMappings — DetailUpdates.dxdb and StandardUpdates.dxdb
            //
            // These .dxdb files are SQLite databases populated from SQL text files
            // in the DatabaseUpdates folder. The Cloud side exposes the same data as
            // six DisclosureUpdate*.csv files per framework #Settings folder.
            //
            // Id 1 & 2 are reserved for FindReplace / FindReplaceCS (seeded earlier).
            // -----------------------------------------------------------------------

            migrationBuilder.Sql(@"INSERT OR IGNORE INTO DxdbCsvMappings
                (Id, Description, DxdbFilePattern, CsvFilePattern, IsActive, Notes)
            VALUES
            -- StandardSeq table (master registry of disclosure updates)
            (3,  'Detail updates — master registry',
                 '*DetailUpdates.dxdb',
                 '*\#Settings\DisclosureUpdates.csv',
                 1,
                 'StandardSeq table in dxdb matches rows where Type=Details in DisclosureUpdates.csv. Join key: Seq <-> Number and Standard <-> Name.'),

            (4,  'Standard updates — master registry',
                 '*StandardUpdates.dxdb',
                 '*\#Settings\DisclosureUpdates.csv',
                 1,
                 'StandardSeq table in dxdb matches rows where Type=Standard in DisclosureUpdates.csv. Join key: Seq <-> Number and Standard <-> Name.'),

            -- FileInsert table (file copy instructions)
            (5,  'Detail updates — insert files',
                 '*DetailUpdates.dxdb',
                 '*\#Settings\DisclosureUpdateInsertFiles.csv',
                 1,
                 'FileInsert table in dxdb maps to DisclosureUpdateInsertFiles.csv. Join key: StandardSeq <-> DisclosureUpdateNumber + SourceFilePath <-> Source.'),

            (6,  'Standard updates — insert files',
                 '*StandardUpdates.dxdb',
                 '*\#Settings\DisclosureUpdateInsertFiles.csv',
                 1,
                 'FileInsert table in dxdb maps to DisclosureUpdateInsertFiles.csv. Join key: StandardSeq <-> DisclosureUpdateNumber + SourceFilePath <-> Source.'),

            -- GUIDInsert table (template row positioning)
            (7,  'Detail updates — template row GUIDs',
                 '*DetailUpdates.dxdb',
                 '*\#Settings\DisclosureUpdateTemplates.csv',
                 1,
                 'GUIDInsert table in dxdb maps to DisclosureUpdateTemplates.csv. Join key: StandardSeq <-> DisclosureUpdateNumber + InsertGUID <-> RowGuid. Note: dxdb uses InsertAfterSeq (int); CSV uses InsertAfterGuid1/2/3 (GUID strings) — indirect match.'),

            (8,  'Standard updates — template row GUIDs',
                 '*StandardUpdates.dxdb',
                 '*\#Settings\DisclosureUpdateTemplates.csv',
                 1,
                 'GUIDInsert table in dxdb maps to DisclosureUpdateTemplates.csv. Join key: StandardSeq <-> DisclosureUpdateNumber + InsertGUID <-> RowGuid. Note: dxdb uses InsertAfterSeq (int); CSV uses InsertAfterGuid1/2/3 (GUID strings) — indirect match.'),

            -- LeadScheduleMappingInsert table (account code mappings)
            (9,  'Detail updates — account mappings',
                 '*DetailUpdates.dxdb',
                 '*\#Settings\DisclosureUpdateAccountMappings.csv',
                 1,
                 'LeadScheduleMappingInsert table in dxdb maps to DisclosureUpdateAccountMappings.csv. Join key: StandardSeq <-> DisclosureUpdateNumber + Keyword <-> Map (account code). CSV has richer data (AccountType, LeadA-D debit/credit flags) with no direct dxdb counterpart.'),

            -- ReplaceSheets (sheet replacement specs — no direct dxdb table found; tracked for completeness)
            (10, 'Detail updates — replace sheets',
                 '*DetailUpdates.dxdb',
                 '*\#Settings\DisclosureUpdateReplaceSheets.csv',
                 1,
                 'No direct table found in dxdb for sheet replacements. CSV columns: DisclosureUpdateNumber, Type, Version, Sheet. Likely derived from dxdb metadata at generation time.'),

            (11, 'Standard updates — replace sheets',
                 '*StandardUpdates.dxdb',
                 '*\#Settings\DisclosureUpdateReplaceSheets.csv',
                 1,
                 'No direct table found in dxdb for sheet replacements. CSV columns: DisclosureUpdateNumber, Type, Version, Sheet. Likely derived from dxdb metadata at generation time.'),

            -- DatabaseUpdates (framework metadata patches — no direct dxdb table found)
            (12, 'Detail updates — database patches',
                 '*DetailUpdates.dxdb',
                 '*\#Settings\DisclosureUpdateDatabaseUpdates.csv',
                 1,
                 'No direct table found in dxdb. CSV columns: DisclosureUpdateNumber, Type, Column, OldValue, UpdateValue. Used to patch framework metadata strings (e.g. rename FrameworkDescription).'),

            (13, 'Standard updates — database patches',
                 '*StandardUpdates.dxdb',
                 '*\#Settings\DisclosureUpdateDatabaseUpdates.csv',
                 1,
                 'No direct table found in dxdb. CSV columns: DisclosureUpdateNumber, Type, Column, OldValue, UpdateValue. Used to patch framework metadata strings (e.g. rename FrameworkDescription).');");

            // -----------------------------------------------------------------------
            // FieldMappings — column-level mappings for each DxdbCsvMapping above
            // -----------------------------------------------------------------------

            migrationBuilder.Sql(@"INSERT OR IGNORE INTO FieldMappings
                (Id, DxdbCsvMappingId, DxdbTableName, DxdbColumnName, CsvColumnName, IsKeyField, Notes)
            VALUES
            -- Mapping 3: DetailUpdates StandardSeq -> DisclosureUpdates.csv (Details rows)
            (1,  3, 'StandardSeq', 'Seq',         'Number',      1, 'Primary join key — sequence number'),
            (2,  3, 'StandardSeq', 'Standard',    'Name',        1, 'Secondary join key — standard code name'),
            (3,  3, 'StandardSeq', 'Description', 'Description', 0, NULL),
            (4,  3, 'StandardSeq', 'Source',      'Source',      0, 'External or Internal'),
            (5,  3, 'StandardSeq', 'Path',        'Path',        0, 'Source file name e.g. Detail.xlsx'),
            (6,  3, 'StandardSeq', 'Forced',      'Forced',      0, '0 or 1'),

            -- Mapping 4: StandardUpdates StandardSeq -> DisclosureUpdates.csv (Standard rows)
            (7,  4, 'StandardSeq', 'Seq',         'Number',      1, 'Primary join key — sequence number'),
            (8,  4, 'StandardSeq', 'Standard',    'Name',        1, 'Secondary join key — standard code name'),
            (9,  4, 'StandardSeq', 'Description', 'Description', 0, NULL),
            (10, 4, 'StandardSeq', 'Source',      'Source',      0, 'External or Internal'),
            (11, 4, 'StandardSeq', 'Path',        'Path',        0, 'Typically empty for Standard updates'),
            (12, 4, 'StandardSeq', 'Forced',      'Forced',      0, '0 or 1'),

            -- Mapping 5: DetailUpdates FileInsert -> DisclosureUpdateInsertFiles.csv
            (13, 5, 'FileInsert', 'StandardSeq',        'DisclosureUpdateNumber', 1, 'FK join to StandardSeq.Seq'),
            (14, 5, 'FileInsert', 'SourceFilePath',     'Source',                 0, NULL),
            (15, 5, 'FileInsert', 'DestinationFilePath','Destination',            0, NULL),

            -- Mapping 6: StandardUpdates FileInsert -> DisclosureUpdateInsertFiles.csv
            (16, 6, 'FileInsert', 'StandardSeq',        'DisclosureUpdateNumber', 1, 'FK join to StandardSeq.Seq'),
            (17, 6, 'FileInsert', 'SourceFilePath',     'Source',                 0, NULL),
            (18, 6, 'FileInsert', 'DestinationFilePath','Destination',            0, NULL),

            -- Mapping 7: DetailUpdates GUIDInsert -> DisclosureUpdateTemplates.csv
            (19, 7, 'GUIDInsert', 'StandardSeq',   'DisclosureUpdateNumber', 1, 'FK join to StandardSeq.Seq'),
            (20, 7, 'GUIDInsert', 'InsertGUID',    'RowGuid',                1, 'GUID identifying the template row to insert'),
            (21, 7, 'GUIDInsert', 'InsertAfterSeq','InsertAfterGuid1',       0, 'dxdb stores a seq int; CSV stores GUIDs — indirect relationship, needs resolution during compare'),

            -- Mapping 8: StandardUpdates GUIDInsert -> DisclosureUpdateTemplates.csv
            (22, 8, 'GUIDInsert', 'StandardSeq',   'DisclosureUpdateNumber', 1, 'FK join to StandardSeq.Seq'),
            (23, 8, 'GUIDInsert', 'InsertGUID',    'RowGuid',                1, 'GUID identifying the template row to insert'),
            (24, 8, 'GUIDInsert', 'InsertAfterSeq','InsertAfterGuid1',       0, 'dxdb stores a seq int; CSV stores GUIDs — indirect relationship, needs resolution during compare'),

            -- Mapping 9: DetailUpdates LeadScheduleMappingInsert -> DisclosureUpdateAccountMappings.csv
            (25, 9, 'LeadScheduleMappingInsert', 'StandardSeq', 'DisclosureUpdateNumber', 1, 'FK join to StandardSeq.Seq'),
            (26, 9, 'LeadScheduleMappingInsert', 'Keyword',     'Map',                    1, 'Account code e.g. c.745.302 — secondary join key'),
            (27, 9, 'LeadScheduleMappingInsert', 'FilePath',    '',                       0, 'No direct CSV column identified — likely embedded in Map or Details field'),
            (28, 9, 'LeadScheduleMappingInsert', 'Section',     '',                       0, 'No direct CSV column identified — may map to AccountCategory or LeadA/B/C/D');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM FieldMappings WHERE Id BETWEEN 1 AND 28;");
            migrationBuilder.Sql("DELETE FROM DxdbCsvMappings WHERE Id BETWEEN 3 AND 13;");
        }
    }
}
