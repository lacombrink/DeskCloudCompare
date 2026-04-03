namespace DeskCloudCompare.Models;

public enum SpecialRuleType
{
    OneToMany = 0,
    FormatConversion = 1
}

public class SpecialFileRule
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;

    /// <summary>Exact filename to match (case-insensitive). E.g. "LeadTemp.xlsx".</summary>
    public string FileNamePattern { get; set; } = string.Empty;

    public SpecialRuleType RuleType { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }
}
