namespace DeskCloudCompare.Models;

/// <summary>
/// Maps a column inside a dxdb SQLite table to the corresponding column in the Cloud CSV export.
/// Used by the Data Compare feature.
/// </summary>
public class FieldMapping
{
    public int Id { get; set; }

    public int DxdbCsvMappingId { get; set; }
    public DxdbCsvMapping DxdbCsvMapping { get; set; } = null!;

    public string DxdbTableName { get; set; } = string.Empty;
    public string DxdbColumnName { get; set; } = string.Empty;
    public string CsvColumnName { get; set; } = string.Empty;

    /// <summary>If true, this field is part of the join key used to match rows.</summary>
    public bool IsKeyField { get; set; }

    public string? Notes { get; set; }
}
