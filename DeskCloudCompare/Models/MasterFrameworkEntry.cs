namespace DeskCloudCompare.Models;

/// <summary>
/// A framework folder inside the 999.ZZ master folder.
/// </summary>
public class MasterFrameworkEntry
{
    public int Id { get; set; }
    public string CanonicalName { get; set; } = string.Empty;  // "IFRS+"
    public string FolderName { get; set; } = string.Empty;     // "IFRS+ ZZ"
    public FrameworkTypeGroup TypeGroup { get; set; }
    public int SortOrder { get; set; }

    public ICollection<MasterFilePresence> FilePresences { get; set; } = new List<MasterFilePresence>();
}
