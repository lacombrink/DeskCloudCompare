namespace DeskCloudCompare.Models;

public class FrameworkManagerSettings
{
    public int Id { get; set; }
    public string MasterFolderPath { get; set; } = string.Empty;
    public DateTime? LastScanned { get; set; }
}
