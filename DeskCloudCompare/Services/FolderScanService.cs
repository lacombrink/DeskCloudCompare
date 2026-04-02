using DeskCloudCompare.Models;
using System.IO;

namespace DeskCloudCompare.Services;

public record SlotConfig(string Label, string FolderPath, int? FolderTypeId);

public record SlotFileInfo(string FullPath, long Size, DateTime LastWrite);

public class ComparisonRow
{
    public string CanonicalPath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public SlotFileInfo? SlotA { get; set; }
    public SlotFileInfo? SlotB { get; set; }
    public SlotFileInfo? SlotC { get; set; }
    public SlotFileInfo? SlotD { get; set; }
}

public class FolderScanService
{
    public async Task<List<ComparisonRow>> ScanAsync(
        IReadOnlyList<SlotConfig> slots,
        IEnumerable<PathTranslationRule> rules,
        IProgress<string>? progress = null,
        CancellationToken ct = default)
    {
        var rulesList = rules.ToList();

        var dict = new Dictionary<string, ComparisonRow>(StringComparer.OrdinalIgnoreCase);

        await Task.Run(() =>
        {
            foreach (var slot in slots)
            {
                if (string.IsNullOrWhiteSpace(slot.FolderPath) ||
                    !Directory.Exists(slot.FolderPath))
                    continue;

                progress?.Report($"Scanning slot {slot.Label}: {slot.FolderPath}");

                var root = slot.FolderPath.TrimEnd('\\', '/') + '\\';

                foreach (var fullPath in Directory.EnumerateFiles(slot.FolderPath, "*", SearchOption.AllDirectories))
                {
                    ct.ThrowIfCancellationRequested();

                    var relPath = fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase)
                        ? fullPath[(root.Length - 1)..] // keep leading backslash
                        : "\\" + fullPath[slot.FolderPath.Length..].TrimStart('\\', '/');

                    var canonical = slot.FolderTypeId.HasValue
                        ? PathTranslationService.Normalize(relPath, slot.FolderTypeId.Value, rulesList)
                        : relPath;

                    var info = new FileInfo(fullPath);
                    var fileInfo = new SlotFileInfo(fullPath, info.Length, info.LastWriteTime);

                    if (!dict.TryGetValue(canonical, out var row))
                    {
                        row = new ComparisonRow
                        {
                            CanonicalPath = canonical,
                            FileName = Path.GetFileName(fullPath)
                        };
                        dict[canonical] = row;
                    }

                    switch (slot.Label)
                    {
                        case "A": row.SlotA = fileInfo; break;
                        case "B": row.SlotB = fileInfo; break;
                        case "C": row.SlotC = fileInfo; break;
                        case "D": row.SlotD = fileInfo; break;
                    }
                }
            }
        }, ct);

        return dict.Values.OrderBy(r => r.CanonicalPath, StringComparer.OrdinalIgnoreCase).ToList();
    }
}
