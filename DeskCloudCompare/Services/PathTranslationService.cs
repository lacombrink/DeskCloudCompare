using DeskCloudCompare.Data;
using DeskCloudCompare.Models;
using Microsoft.EntityFrameworkCore;

namespace DeskCloudCompare.Services;

public class PathTranslationService(AppDbContext db)
{
    public Task<List<PathTranslationRule>> GetAllAsync() =>
        db.PathTranslationRules
          .Include(r => r.FromType)
          .Include(r => r.ToType)
          .OrderBy(r => r.FromTypeId)
          .ThenBy(r => r.SortOrder)
          .ToListAsync();

    public async Task AddAsync(PathTranslationRule rule)
    {
        db.PathTranslationRules.Add(rule);
        await db.SaveChangesAsync();
    }

    public Task UpdateAsync() => db.SaveChangesAsync();

    public async Task DeleteAsync(int id)
    {
        var rule = await db.PathTranslationRules.FindAsync(id)
            ?? throw new KeyNotFoundException();
        db.PathTranslationRules.Remove(rule);
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Normalizes a relative file path from the given folder type toward the canonical form.
    /// Applies all rules where FromTypeId matches, in SortOrder order.
    /// This is a pure method — pass the pre-loaded rules list to avoid DB calls per file.
    /// </summary>
    public static string Normalize(
        string relativeFilePath,
        int fromTypeId,
        IEnumerable<PathTranslationRule> allRules)
    {
        var applicable = allRules
            .Where(r => r.FromTypeId == fromTypeId)
            .OrderBy(r => r.SortOrder);

        var path = relativeFilePath;
        foreach (var rule in applicable)
            path = path.Replace(rule.FindText, rule.ReplaceText, StringComparison.OrdinalIgnoreCase);

        return path;
    }
}
