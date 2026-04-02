using DeskCloudCompare.Data;
using DeskCloudCompare.Models;
using Microsoft.EntityFrameworkCore;

namespace DeskCloudCompare.Services;

public class PresetService(AppDbContext db)
{
    public Task<List<FolderPreset>> GetAllAsync() =>
        db.FolderPresets
          .Include(p => p.Slots)
          .ThenInclude(s => s.FolderType)
          .OrderBy(p => p.Name)
          .ToListAsync();

    public async Task<FolderPreset> AddAsync(string name)
    {
        var preset = new FolderPreset { Name = name };
        db.FolderPresets.Add(preset);
        await db.SaveChangesAsync();

        // Create the four default slots
        foreach (var label in new[] { "A", "B", "C", "D" })
            db.FolderPresetSlots.Add(new FolderPresetSlot { PresetId = preset.Id, SlotLabel = label });

        await db.SaveChangesAsync();
        return preset;
    }

    public Task UpdateAsync() => db.SaveChangesAsync();

    public async Task DeleteAsync(int id)
    {
        var preset = await db.FolderPresets.FindAsync(id)
            ?? throw new KeyNotFoundException();
        db.FolderPresets.Remove(preset);
        await db.SaveChangesAsync();
    }
}
