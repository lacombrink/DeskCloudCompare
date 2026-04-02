namespace DeskCloudCompare.Models;

public class FolderPresetSlot
{
    public int Id { get; set; }

    public int PresetId { get; set; }
    public FolderPreset Preset { get; set; } = null!;

    public string SlotLabel { get; set; } = string.Empty; // "A", "B", "C", "D"
    public string? FolderPath { get; set; }

    public int? FolderTypeId { get; set; }
    public FolderType? FolderType { get; set; }
}
