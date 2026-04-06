namespace DeskCloudCompare.Models;

/// <summary>
/// A unique file path (after alias resolution) within a framework type group.
/// The canonical name comes from IFRS+ ZZ where the file exists there;
/// otherwise the first framework that contains it.
/// </summary>
public class MasterCanonicalFile
{
    public int Id { get; set; }
    public FrameworkTypeGroup TypeGroup { get; set; }

    /// <summary>Canonical relative path (may differ from actual paths via FileAliases).</summary>
    public string RelativePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public bool IsDxdb { get; set; }
    public bool IsFinancialData { get; set; }

    public ICollection<MasterFilePresence> Presences { get; set; } = new List<MasterFilePresence>();
}
