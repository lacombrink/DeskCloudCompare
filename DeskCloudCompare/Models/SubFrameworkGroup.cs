namespace DeskCloudCompare.Models;

public enum SubFrameworkGroup
{
    FRS102      = 0,   // FRS102_Company, FRS102 Consoli, FRS102_Partnership, FRS102_Sole Prop
    FRS102_1A   = 1,   // FRS102(1A)_Company, FRS102(1A) Consoli, FRS102(1A)_Partnership, FRS102(1A)_Sole Prop
    FRS105      = 2,   // FRS105_Company, FRS105_Partnership, FRS105_Sole Prop
    IFRS_Plus   = 3,   // IFRS+, IFRS+ Consoli, IFRS_Company, IFRS Consoli_Company, FRS101
    IFRS_SME    = 4,   // IFRS SME+, IFRS SME+ Consoli, CC+, NPC+, NPO+, Partnership+, School+,
                       //   Sole Prop+, Trust+, Body Corp+, Monthly+ ManPack
    FRS_SORP    = 5,   // Charity SORP, LLP SORP variants
}
