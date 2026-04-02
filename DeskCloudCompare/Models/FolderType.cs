namespace DeskCloudCompare.Models;

public class FolderType
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public ICollection<PathTranslationRule> FromRules { get; set; } = new List<PathTranslationRule>();
    public ICollection<PathTranslationRule> ToRules { get; set; } = new List<PathTranslationRule>();
    public ICollection<FolderPresetSlot> Slots { get; set; } = new List<FolderPresetSlot>();
}
