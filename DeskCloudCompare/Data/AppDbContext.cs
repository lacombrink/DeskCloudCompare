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

    // Framework Manager
    public DbSet<FrameworkManagerSettings> FrameworkManagerSettings => Set<FrameworkManagerSettings>();
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
            new MasterFileAlias { Id = 15, FolderPath = @"Documents\DraftworX Files", ActualFileName = "Member Minutes-Resolution2.xlsx",       CanonicalFileName = "Shareholder Minutes-Resolution.xlsx" }
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
