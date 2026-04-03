using DeskCloudCompare.Data;
using DeskCloudCompare.Models;
using Microsoft.EntityFrameworkCore;

namespace DeskCloudCompare.Services;

public class PresetExclusionService(AppDbContext db)
{
    public Task<List<PresetExclusion>> GetByPresetAsync(int presetId) =>
        db.PresetExclusions
          .Where(e => e.PresetId == presetId)
          .OrderBy(e => e.MatchType)
          .ThenBy(e => e.Pattern)
          .ToListAsync();

    public async Task AddAsync(PresetExclusion exclusion)
    {
        db.PresetExclusions.Add(exclusion);
        await db.SaveChangesAsync();
    }

    public Task UpdateAsync() => db.SaveChangesAsync();

    public async Task DeleteAsync(int id)
    {
        var rule = await db.PresetExclusions.FindAsync(id) ?? throw new KeyNotFoundException();
        db.PresetExclusions.Remove(rule);
        await db.SaveChangesAsync();
    }
}
