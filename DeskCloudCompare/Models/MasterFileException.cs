namespace DeskCloudCompare.Models;

/// <summary>
/// Records that a file's absence in a specific framework is intentional.
/// Keyed by stable names so it survives a rescan.
/// </summary>
public class MasterFileException
{
    public int Id { get; set; }
    public FrameworkTypeGroup TypeGroup { get; set; }
    public string RelativePath { get; set; } = string.Empty;
    public string FrameworkCanonicalName { get; set; } = string.Empty;
}
