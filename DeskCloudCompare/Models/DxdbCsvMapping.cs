namespace DeskCloudCompare.Models;

/// <summary>
/// Links a .dxdb file pattern (Desktop) to its equivalent .csv file pattern (Cloud)
/// for future data-level comparison once the dxdb decryption key is available.
/// </summary>
public class DxdbCsvMapping
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public string DxdbFilePattern { get; set; } = string.Empty;
    public string CsvFilePattern { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }

    public ICollection<FieldMapping> FieldMappings { get; set; } = new List<FieldMapping>();
}
