using DeskCloudCompare.Models;
using Microsoft.EntityFrameworkCore;

namespace DeskCloudCompare.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<FolderType> FolderTypes => Set<FolderType>();
    public DbSet<PathTranslationRule> PathTranslationRules => Set<PathTranslationRule>();
    public DbSet<FolderPreset> FolderPresets => Set<FolderPreset>();
    public DbSet<FolderPresetSlot> FolderPresetSlots => Set<FolderPresetSlot>();
    public DbSet<PresetExclusion> PresetExclusions => Set<PresetExclusion>();

    // Framework Manager + Sub Framework Manager
    public DbSet<FrameworkManagerSettings> FrameworkManagerSettings => Set<FrameworkManagerSettings>();
    public DbSet<SubFrameworkFileException> SubFrameworkFileExceptions => Set<SubFrameworkFileException>();
    public DbSet<MasterFrameworkEntry> MasterFrameworkEntries => Set<MasterFrameworkEntry>();
    public DbSet<MasterCanonicalFile> MasterCanonicalFiles => Set<MasterCanonicalFile>();
    public DbSet<MasterFilePresence> MasterFilePresences => Set<MasterFilePresence>();
    public DbSet<MasterFileAlias> MasterFileAliases => Set<MasterFileAlias>();
    public DbSet<MasterFileException> MasterFileExceptions => Set<MasterFileException>();

    // Country Manager
    public DbSet<CountryEntry> CountryEntries => Set<CountryEntry>();
    public DbSet<CanonicalFramework> CanonicalFrameworks => Set<CanonicalFramework>();
    public DbSet<CountryFrameworkPresence> CountryFrameworkPresences => Set<CountryFrameworkPresence>();
    public DbSet<CanonicalFile> CanonicalFiles => Set<CanonicalFile>();
    public DbSet<CountryFilePresence> CountryFilePresences => Set<CountryFilePresence>();
    public DbSet<CountryManagerSettings> CountryManagerSettings => Set<CountryManagerSettings>();
    public DbSet<CountryFileException> CountryFileExceptions => Set<CountryFileException>();
    public DbSet<SpecialFileRule> SpecialFileRules => Set<SpecialFileRule>();
    public DbSet<DxdbCsvMapping> DxdbCsvMappings => Set<DxdbCsvMapping>();
    public DbSet<FieldMapping> FieldMappings => Set<FieldMapping>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FolderType>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired();
            e.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<PathTranslationRule>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.FromType)
             .WithMany(x => x.FromRules)
             .HasForeignKey(x => x.FromTypeId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ToType)
             .WithMany(x => x.ToRules)
             .HasForeignKey(x => x.ToTypeId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<FolderPreset>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired();
        });

        modelBuilder.Entity<FolderPresetSlot>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Preset)
             .WithMany(x => x.Slots)
             .HasForeignKey(x => x.PresetId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.FolderType)
             .WithMany(x => x.Slots)
             .HasForeignKey(x => x.FolderTypeId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<PresetExclusion>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Preset)
             .WithMany(x => x.Exclusions)
             .HasForeignKey(x => x.PresetId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SpecialFileRule>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.FileNamePattern).IsRequired();
        });

        modelBuilder.Entity<DxdbCsvMapping>(e =>
        {
            e.HasKey(x => x.Id);
        });

        modelBuilder.Entity<FieldMapping>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.DxdbCsvMapping)
             .WithMany(x => x.FieldMappings)
             .HasForeignKey(x => x.DxdbCsvMappingId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CountryEntry>(e => e.HasKey(x => x.Code));

        modelBuilder.Entity<CanonicalFramework>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.Name, x.Category }).IsUnique();
        });

        modelBuilder.Entity<CountryFrameworkPresence>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.CanonicalFramework)
             .WithMany(x => x.Presences)
             .HasForeignKey(x => x.CanonicalFrameworkId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CanonicalFile>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.CanonicalFramework)
             .WithMany(x => x.Files)
             .HasForeignKey(x => x.CanonicalFrameworkId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CountryFilePresence>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.CanonicalFile)
             .WithMany(x => x.Presences)
             .HasForeignKey(x => x.CanonicalFileId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CountryManagerSettings>(e => e.HasKey(x => x.Id));

        // Framework Manager
        modelBuilder.Entity<FrameworkManagerSettings>(e => e.HasKey(x => x.Id));

        modelBuilder.Entity<MasterFrameworkEntry>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.CanonicalName).IsUnique();
        });

        modelBuilder.Entity<MasterCanonicalFile>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.TypeGroup, x.RelativePath }).IsUnique();
        });

        modelBuilder.Entity<MasterFilePresence>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.MasterCanonicalFileId, x.MasterFrameworkEntryId }).IsUnique();
            e.HasOne(x => x.MasterCanonicalFile)
             .WithMany(x => x.Presences)
             .HasForeignKey(x => x.MasterCanonicalFileId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.MasterFrameworkEntry)
             .WithMany(x => x.FilePresences)
             .HasForeignKey(x => x.MasterFrameworkEntryId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MasterFileAlias>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.FolderPath, x.ActualFileName }).IsUnique();
        });

        modelBuilder.Entity<MasterFileException>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.TypeGroup, x.RelativePath, x.FrameworkCanonicalName }).IsUnique();
        });

        modelBuilder.Entity<SubFrameworkFileException>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.SubGroup, x.RelativePath, x.FrameworkCanonicalName }).IsUnique();
        });

        // Seed known file aliases (Director/Partner/Proprietor/Trustee/Member variants)
        modelBuilder.Entity<MasterFileAlias>().HasData(
            // Certificates
            new MasterFileAlias { Id = 1,  FolderPath = @"Documents\DraftworX Files", ActualFileName = "Director Certificates.xlsx",   CanonicalFileName = "Director Certificates.xlsx" },
            new MasterFileAlias { Id = 2,  FolderPath = @"Documents\DraftworX Files", ActualFileName = "Partner Certificates.xlsx",    CanonicalFileName = "Director Certificates.xlsx" },
            new MasterFileAlias { Id = 3,  FolderPath = @"Documents\DraftworX Files", ActualFileName = "Proprietor Certificates.xlsx", CanonicalFileName = "Director Certificates.xlsx" },
            new MasterFileAlias { Id = 4,  FolderPath = @"Documents\DraftworX Files", ActualFileName = "Trustee Certificates.xlsx",    CanonicalFileName = "Director Certificates.xlsx" },
            new MasterFileAlias { Id = 5,  FolderPath = @"Documents\DraftworX Files", ActualFileName = "Member Certificates.xlsx",     CanonicalFileName = "Director Certificates.xlsx" },
            // Directors Minutes-Resolution (singular)
            new MasterFileAlias { Id = 6,  FolderPath = @"Documents\DraftworX Files", ActualFileName = "Directors Minutes-Resolution.xlsx", CanonicalFileName = "Directors Minutes-Resolution.xlsx" },
            new MasterFileAlias { Id = 7,  FolderPath = @"Documents\DraftworX Files", ActualFileName = "Partner Minutes-Resolution.xlsx",   CanonicalFileName = "Directors Minutes-Resolution.xlsx" },
            new MasterFileAlias { Id = 8,  FolderPath = @"Documents\DraftworX Files", ActualFileName = "Proprietor Minutes-Resolution.xlsx",CanonicalFileName = "Directors Minutes-Resolution.xlsx" },
            new MasterFileAlias { Id = 9,  FolderPath = @"Documents\DraftworX Files", ActualFileName = "Trustee Minutes-Resolution.xlsx",   CanonicalFileName = "Directors Minutes-Resolution.xlsx" },
            new MasterFileAlias { Id = 10, FolderPath = @"Documents\DraftworX Files", ActualFileName = "Member Minutes-Resolution.xlsx",    CanonicalFileName = "Directors Minutes-Resolution.xlsx" },
            // Shareholder Minutes-Resolution2 variants
            new MasterFileAlias { Id = 11, FolderPath = @"Documents\DraftworX Files", ActualFileName = "Shareholder Minutes-Resolution.xlsx",  CanonicalFileName = "Shareholder Minutes-Resolution.xlsx" },
            new MasterFileAlias { Id = 12, FolderPath = @"Documents\DraftworX Files", ActualFileName = "Partner Minutes-Resolution2.xlsx",      CanonicalFileName = "Shareholder Minutes-Resolution.xlsx" },
            new MasterFileAlias { Id = 13, FolderPath = @"Documents\DraftworX Files", ActualFileName = "Proprietor Minutes-Resolution2.xlsx",   CanonicalFileName = "Shareholder Minutes-Resolution.xlsx" },
            new MasterFileAlias { Id = 14, FolderPath = @"Documents\DraftworX Files", ActualFileName = "Trustee Minutes-Resolution2.xlsx",      CanonicalFileName = "Shareholder Minutes-Resolution.xlsx" },
            new MasterFileAlias { Id = 15, FolderPath = @"Documents\DraftworX Files", ActualFileName = "Member Minutes-Resolution2.xlsx",       CanonicalFileName = "Shareholder Minutes-Resolution.xlsx" },

            // ---------------------------------------------------------------
            // Financial statement files — Financial Data\DraftworX Files
            // Rule: consolidation variants collapse to the same canonical as
            //       the entity financials (aliases are single-level, no chaining).
            // IFRS type group → IFRS+ Financials.xlsx
            // ---------------------------------------------------------------
            new MasterFileAlias { Id = 16, FolderPath = @"Financial Data\DraftworX Files", ActualFileName = "IFRS+ Consolidation.xlsx",          CanonicalFileName = "IFRS+ Financials.xlsx" },
            new MasterFileAlias { Id = 17, FolderPath = @"Financial Data\DraftworX Files", ActualFileName = "IFRS SME+ Consolidation.xlsx",       CanonicalFileName = "IFRS+ Financials.xlsx" },
            new MasterFileAlias { Id = 18, FolderPath = @"Financial Data\DraftworX Files", ActualFileName = "IFRS Consolidation.xlsx",             CanonicalFileName = "IFRS+ Financials.xlsx" },
            new MasterFileAlias { Id = 19, FolderPath = @"Financial Data\DraftworX Files", ActualFileName = "ASPE+ Financials.xlsx",               CanonicalFileName = "IFRS+ Financials.xlsx" },
            new MasterFileAlias { Id = 20, FolderPath = @"Financial Data\DraftworX Files", ActualFileName = "IFRS SME+ Financials.xlsx",           CanonicalFileName = "IFRS+ Financials.xlsx" },
            new MasterFileAlias { Id = 21, FolderPath = @"Financial Data\DraftworX Files", ActualFileName = "IFRS Financials.xlsx",                CanonicalFileName = "IFRS+ Financials.xlsx" },
            new MasterFileAlias { Id = 22, FolderPath = @"Financial Data\DraftworX Files", ActualFileName = "FRS101 Financials.xlsx",              CanonicalFileName = "IFRS+ Financials.xlsx" },
            new MasterFileAlias { Id = 23, FolderPath = @"Financial Data\DraftworX Files", ActualFileName = "Body Corp+ Financials.xlsx",          CanonicalFileName = "IFRS+ Financials.xlsx" },
            new MasterFileAlias { Id = 24, FolderPath = @"Financial Data\DraftworX Files", ActualFileName = "CC+ Financials.xlsx",                 CanonicalFileName = "IFRS+ Financials.xlsx" },
            new MasterFileAlias { Id = 25, FolderPath = @"Financial Data\DraftworX Files", ActualFileName = "NPC+ Financials.xlsx",                CanonicalFileName = "IFRS+ Financials.xlsx" },
            new MasterFileAlias { Id = 26, FolderPath = @"Financial Data\DraftworX Files", ActualFileName = "NPO+ Financials.xlsx",                CanonicalFileName = "IFRS+ Financials.xlsx" },
            new MasterFileAlias { Id = 27, FolderPath = @"Financial Data\DraftworX Files", ActualFileName = "Partnership+ Financials.xlsx",        CanonicalFileName = "IFRS+ Financials.xlsx" },
            new MasterFileAlias { Id = 28, FolderPath = @"Financial Data\DraftworX Files", ActualFileName = "School+ Financials.xlsx",             CanonicalFileName = "IFRS+ Financials.xlsx" },
            new MasterFileAlias { Id = 29, FolderPath = @"Financial Data\DraftworX Files", ActualFileName = "Sole Prop+ Financials.xlsx",          CanonicalFileName = "IFRS+ Financials.xlsx" },
            new MasterFileAlias { Id = 30, FolderPath = @"Financial Data\DraftworX Files", ActualFileName = "Trust+ Financials.xlsx",              CanonicalFileName = "IFRS+ Financials.xlsx" },
            // FRS type group → FRS102 Financials.xlsx  (company entity + both consolidations)
            new MasterFileAlias { Id = 31, FolderPath = @"Financial Data\DraftworX Files", ActualFileName = "FRS102 Consolidation.xlsx",           CanonicalFileName = "FRS102 Financials.xlsx" },
            new MasterFileAlias { Id = 32, FolderPath = @"Financial Data\DraftworX Files", ActualFileName = "FRS102(1A) Consolidation.xlsx",       CanonicalFileName = "FRS102 Financials.xlsx" },
            new MasterFileAlias { Id = 33, FolderPath = @"Financial Data\DraftworX Files", ActualFileName = "FRS102(1A) Financials.xlsx",          CanonicalFileName = "FRS102 Financials.xlsx" },
            new MasterFileAlias { Id = 34, FolderPath = @"Financial Data\DraftworX Files", ActualFileName = "FRS105 Financials.xlsx",              CanonicalFileName = "FRS102 Financials.xlsx" },
            // FRS type group — variant names → FRS102 Financials.xlsx (all collapse to one canonical)
            new MasterFileAlias { Id = 35, FolderPath = @"Financial Data\DraftworX Files", ActualFileName = "Partnership(1A) Financials.xlsx",     CanonicalFileName = "FRS102 Financials.xlsx" },
            new MasterFileAlias { Id = 36, FolderPath = @"Financial Data\DraftworX Files", ActualFileName = "Partnership(105) Financials.xlsx",    CanonicalFileName = "FRS102 Financials.xlsx" },
            new MasterFileAlias { Id = 37, FolderPath = @"Financial Data\DraftworX Files", ActualFileName = "Sole Prop(1A) Financials.xlsx",       CanonicalFileName = "FRS102 Financials.xlsx" },
            new MasterFileAlias { Id = 38, FolderPath = @"Financial Data\DraftworX Files", ActualFileName = "Sole Prop(105) Financials.xlsx",      CanonicalFileName = "FRS102 Financials.xlsx" },
            new MasterFileAlias { Id = 39, FolderPath = @"Financial Data\DraftworX Files", ActualFileName = "LLP(1A) Financials.xlsx",             CanonicalFileName = "FRS102 Financials.xlsx" },
            // Legacy type group: Afrikaans variants → canonical English name
            new MasterFileAlias { Id = 40, FolderPath = @"Financial Data\DraftworX Files", ActualFileName = "Body Corporate_Afrikaans.xlsx",       CanonicalFileName = "Body Corporate Financials.xlsx" },
            new MasterFileAlias { Id = 41, FolderPath = @"Financial Data\DraftworX Files", ActualFileName = "Close Corporation_Afrikaans.xlsx",    CanonicalFileName = "Close Corporation Financials.xlsx" },
            new MasterFileAlias { Id = 42, FolderPath = @"Financial Data\DraftworX Files", ActualFileName = "IFRS SME_Afrikaans.xlsx",             CanonicalFileName = "IFRS SME Financials.xlsx" },
            new MasterFileAlias { Id = 43, FolderPath = @"Financial Data\DraftworX Files", ActualFileName = "Partnership_Afrikaans.xlsx",          CanonicalFileName = "FRS102 Financials.xlsx" },
            new MasterFileAlias { Id = 44, FolderPath = @"Financial Data\DraftworX Files", ActualFileName = "Sole Proprietor_Afrikaans.xlsx",      CanonicalFileName = "Sole Proprietor Financials.xlsx" },
            new MasterFileAlias { Id = 45, FolderPath = @"Financial Data\DraftworX Files", ActualFileName = "Trust Financials _Afrikaans.xlsx",    CanonicalFileName = "Trust Financials.xlsx" },
            // FRS type group — entity financial file names for each sub-group → FRS102 Financials.xlsx
            new MasterFileAlias { Id = 46, FolderPath = @"Financial Data\DraftworX Files", ActualFileName = "Charity Financials.xlsx",             CanonicalFileName = "FRS102 Financials.xlsx" },
            new MasterFileAlias { Id = 47, FolderPath = @"Financial Data\DraftworX Files", ActualFileName = "LLP Financials.xlsx",                 CanonicalFileName = "FRS102 Financials.xlsx" },
            new MasterFileAlias { Id = 48, FolderPath = @"Financial Data\DraftworX Files", ActualFileName = "Partnership Financials.xlsx",         CanonicalFileName = "FRS102 Financials.xlsx" },
            new MasterFileAlias { Id = 49, FolderPath = @"Financial Data\DraftworX Files", ActualFileName = "Sole Prop Financials.xlsx",           CanonicalFileName = "FRS102 Financials.xlsx" },
            new MasterFileAlias { Id = 50, FolderPath = @"Financial Data\DraftworX Files", ActualFileName = "Monthly+ Manpack.xlsx",               CanonicalFileName = "IFRS+ Financials.xlsx" }
        );

        modelBuilder.Entity<CountryFileException>(e =>
        {
            e.HasKey(x => x.Id);
            // Unique on the stable natural key — survives rescan (no FK to volatile CanonicalFile IDs)
            e.HasIndex(x => new { x.FrameworkName, x.FrameworkCategory, x.RelativePath, x.CountryCode })
             .IsUnique();
        });

        // Seed a default "Canonical" folder type as the normalization pivot
        modelBuilder.Entity<FolderType>().HasData(
            new FolderType { Id = 1, Name = "Canonical" }
        );
    }
}
