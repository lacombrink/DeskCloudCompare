using DeskCloudCompare.Data;
using DeskCloudCompare.Models;
using Microsoft.EntityFrameworkCore;

namespace DeskCloudCompare.Services;

public class FolderTypeService(AppDbContext db)
{
    public Task<List<FolderType>> GetAllAsync() =>
        db.FolderTypes.OrderBy(x => x.Name).ToListAsync();

    public async Task<FolderType> AddAsync(string name)
    {
        var type = new FolderType { Name = name };
        db.FolderTypes.Add(type);
        await db.SaveChangesAsync();
        return type;
    }

    public Task UpdateAsync() => db.SaveChangesAsync();

    public async Task DeleteAsync(int id)
    {
        var inUseByRules = await db.PathTranslationRules
            .AnyAsync(r => r.FromTypeId == id || r.ToTypeId == id);
        var inUseBySlots = await db.FolderPresetSlots
            .AnyAsync(s => s.FolderTypeId == id);

        if (inUseByRules || inUseBySlots)
            throw new InvalidOperationException(
                "Cannot delete this folder type because it is referenced by translation rules or preset slots.");

        var type = await db.FolderTypes.FindAsync(id)
            ?? throw new KeyNotFoundException();
        db.FolderTypes.Remove(type);
        await db.SaveChangesAsync();
    }
}
