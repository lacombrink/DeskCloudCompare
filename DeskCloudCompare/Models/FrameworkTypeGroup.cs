namespace DeskCloudCompare.Models;

public enum FrameworkTypeGroup
{
    IFRS   = 0,   // IFRS+, IFRS SME+, IFRS_Company, IFRS Consoli_Company, FRS101, ASPE
    FRS    = 1,   // FRS102, FRS102(1A), FRS105, Charity SORP, LLP SORP
    Legacy = 2,   // Body Corp, CC, Trust, Partnership, Sole Prop, NPC, NPO, School, ManPack, CET etc.
    Arabic = 3    // IFRS Full, IFRS SME English, Arabic-name templates
}
