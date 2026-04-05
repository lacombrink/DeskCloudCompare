namespace DeskCloudCompare.Models;

public class CountryManagerSettings
{
    public int Id { get; set; }
    public string RootFolderPath { get; set; } = string.Empty;
    public string? MasterCountryCode { get; set; }
    public DateTime? LastScanned { get; set; }
}
