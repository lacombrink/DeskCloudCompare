namespace DeskCloudCompare.Models;

public class FolderPreset
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public ICollection<FolderPresetSlot> Slots { get; set; } = new List<FolderPresetSlot>();
    public ICollection<PresetExclusion> Exclusions { get; set; } = new List<PresetExclusion>();
}
