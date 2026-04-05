namespace DeskCloudCompare.Models;

public class CountryFilePresence
{
    public int Id { get; set; }
    public int CanonicalFileId { get; set; }
    public CanonicalFile CanonicalFile { get; set; } = null!;
    public string CountryCode { get; set; } = string.Empty;

    /// <summary>
    /// SHA-256 hash of the file, populated when binary compare is run.
    /// Null means not yet compared (or excluded from compare).
    /// </summary>
    public string? BinaryHash { get; set; }
}
