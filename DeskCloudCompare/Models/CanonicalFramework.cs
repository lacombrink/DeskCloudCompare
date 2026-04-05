namespace DeskCloudCompare.Models;

public enum FrameworkCategory { Framework = 0, Methodology = 1 }

public class CanonicalFramework
{
    public int Id { get; set; }

    /// <summary>Canonical name with country suffix stripped, e.g. "IFRS+", "FRS102_Company".</summary>
    public string Name { get; set; } = string.Empty;

    public FrameworkCategory Category { get; set; }

    public ICollection<CountryFrameworkPresence> Presences { get; set; } = new List<CountryFrameworkPresence>();
    public ICollection<CanonicalFile> Files { get; set; } = new List<CanonicalFile>();
}
