namespace DeskCloudCompare.Models;

public class PathTranslationRule
{
    public int Id { get; set; }

    public int FromTypeId { get; set; }
    public FolderType FromType { get; set; } = null!;

    public int ToTypeId { get; set; }
    public FolderType ToType { get; set; } = null!;

    public string FindText { get; set; } = string.Empty;
    public string ReplaceText { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}
