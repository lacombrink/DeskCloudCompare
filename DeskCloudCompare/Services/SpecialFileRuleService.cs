using DeskCloudCompare.Data;
using DeskCloudCompare.Models;
using Microsoft.EntityFrameworkCore;

namespace DeskCloudCompare.Services;

public class SpecialFileRuleService(AppDbContext db)
{
    public Task<List<SpecialFileRule>> GetAllAsync() =>
        db.SpecialFileRules.OrderBy(r => r.RuleType).ThenBy(r => r.Description).ToListAsync();

    public async Task AddAsync(SpecialFileRule rule)
    {
        db.SpecialFileRules.Add(rule);
        await db.SaveChangesAsync();
    }

    public Task UpdateAsync() => db.SaveChangesAsync();

    public async Task DeleteAsync(int id)
    {
        var rule = await db.SpecialFileRules.FindAsync(id) ?? throw new KeyNotFoundException();
        db.SpecialFileRules.Remove(rule);
        await db.SaveChangesAsync();
    }
}
