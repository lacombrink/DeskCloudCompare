namespace DeskCloudCompare.Models;

public enum ExclusionMatchType
{
    Contains = 0,
    StartsWith = 1,
    EndsWith = 2
}

public class PresetExclusion
{
    public int Id { get; set; }
    public int PresetId { get; set; }
    public FolderPreset Preset { get; set; } = null!;

    /// <summary>Matched against the canonical path (case-insensitive).</summary>
    public string Pattern { get; set; } = string.Empty;
    public ExclusionMatchType MatchType { get; set; } = ExclusionMatchType.Contains;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}
