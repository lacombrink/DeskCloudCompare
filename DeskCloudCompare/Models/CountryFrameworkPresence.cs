namespace DeskCloudCompare.Models;

public class CountryFrameworkPresence
{
    public int Id { get; set; }
    public int CanonicalFrameworkId { get; set; }
    public CanonicalFramework CanonicalFramework { get; set; } = null!;
    public string CountryCode { get; set; } = string.Empty;

    /// <summary>Actual folder name on disk, e.g. "IFRS+ ZA" or "FRS102_Company_EW".</summary>
    public string ActualFolderName { get; set; } = string.Empty;
}
