namespace DeskCloudCompare.Models;

/// <summary>
/// Records that a MasterCanonicalFile exists in a specific MasterFrameworkEntry,
/// optionally storing the actual (non-canonical) file name and binary hash.
/// </summary>
public class MasterFilePresence
{
    public int Id { get; set; }

    public int MasterCanonicalFileId { get; set; }
    public MasterCanonicalFile MasterCanonicalFile { get; set; } = null!;

    public int MasterFrameworkEntryId { get; set; }
    public MasterFrameworkEntry MasterFrameworkEntry { get; set; } = null!;

    /// <summary>Actual file name in this framework (may differ from canonical via alias).</summary>
    public string ActualFileName { get; set; } = string.Empty;

    public string? BinaryHash { get; set; }
}
