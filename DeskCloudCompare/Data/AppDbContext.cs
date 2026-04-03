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

        // Seed a default "Canonical" folder type as the normalization pivot
        modelBuilder.Entity<FolderType>().HasData(
            new FolderType { Id = 1, Name = "Canonical" }
        );
    }
}
