namespace DeskCloudCompare.Models;

public class CountryEntry
{
    /// <summary>Two-letter country code, e.g. "ZA", "BW", "EW".</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Raw folder name as it appears on disk, e.g. "710.ZA", "826.EW".</summary>
    public string RawFolderName { get; set; } = string.Empty;

    /// <summary>Controls display order in matrix columns.</summary>
    public int SortOrder { get; set; }
}
