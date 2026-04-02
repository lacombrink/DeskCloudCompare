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

        // Seed a default "Canonical" folder type as the normalization pivot
        modelBuilder.Entity<FolderType>().HasData(
            new FolderType { Id = 1, Name = "Canonical" }
        );
    }
}
