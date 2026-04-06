namespace DeskCloudCompare.Models;

/// <summary>
/// Maps an actual file name (as it appears in a specific framework) to the
/// canonical file name (from IFRS+ ZZ). E.g. "Partner Certificates.xlsx" → "Director Certificates.xlsx".
/// </summary>
public class MasterFileAlias
{
    public int Id { get; set; }

    /// <summary>Folder containing the file, relative to framework root (e.g. "Documents\DraftworX Files").</summary>
    public string FolderPath { get; set; } = string.Empty;

    /// <summary>Actual file name in this framework.</summary>
    public string ActualFileName { get; set; } = string.Empty;

    /// <summary>Canonical file name (from IFRS+ ZZ or agreed standard).</summary>
    public string CanonicalFileName { get; set; } = string.Empty;
}
