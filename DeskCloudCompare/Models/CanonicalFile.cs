namespace DeskCloudCompare.Models;

public class CanonicalFile
{
    public int Id { get; set; }
    public int CanonicalFrameworkId { get; set; }
    public CanonicalFramework CanonicalFramework { get; set; } = null!;

    /// <summary>
    /// Relative path from the framework folder root.
    /// Update-folder files are prefixed with "#Updates\", e.g. "#Updates\Detail.xlsx".
    /// </summary>
    public string RelativePath { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    /// <summary>True for .dxdb files — listed with a different colour, not binary-compared.</summary>
    public bool IsDxdb { get; set; }

    /// <summary>True for files under "Financial Data\" — listed but excluded from binary compare.</summary>
    public bool IsFinancialData { get; set; }

    public ICollection<CountryFilePresence> Presences { get; set; } = new List<CountryFilePresence>();
}
