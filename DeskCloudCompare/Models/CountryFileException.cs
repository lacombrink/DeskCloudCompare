namespace DeskCloudCompare.Models;

/// <summary>
/// Records that a file's absence in a specific country is intentional/expected,
/// e.g. "Levies in arrears" only applies to BW and ZA.
/// Stored by canonical name + path so it survives a full rescan.
/// </summary>
public class CountryFileException
{
    public int Id { get; set; }
    public string FrameworkName { get; set; } = string.Empty;
    public FrameworkCategory FrameworkCategory { get; set; }
    public string RelativePath { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
}
