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

    // Set by OneToMany post-processing
    public bool IsOneToMany { get; set; }
    public int OneToManyTotalCopies { get; set; }
    public int OneToManyMatchingCopies { get; set; }

    // Set by FormatConversion detection
    public bool IsFormatConversion { get; set; }

    // Set by ApplyUpdatesFilesMatch: one Desktop file matches many Cloud copies
    public bool IsUpdatesMatch { get; set; }
    public List<string> UpdatesCloudCopies { get; set; } = new();
    public int UpdatesTotalCopies { get; set; }
    public int UpdatesMatchingCopies { get; set; }
}

public class FolderScanService
{
    /// <summary>
    /// File extensions excluded from file comparison (handled separately in Data Compare).
    /// </summary>
    private static readonly HashSet<string> ExcludedExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".dxdb", ".csv" };

    /// <summary>
    /// Canonical folder names that are always excluded from comparison regardless of preset.
    /// Matched as whole path segments so "999.ZZ" won't accidentally match "1999.ZZ".
    /// </summary>
    private static readonly HashSet<string> ExcludedFolderSegments =
        new(StringComparer.OrdinalIgnoreCase) { "999.ZZ" };

    public async Task<List<ComparisonRow>> ScanAsync(
        IReadOnlyList<SlotConfig> slots,
        IEnumerable<PathTranslationRule> rules,
        IEnumerable<SpecialFileRule>? specialRules = null,
        IEnumerable<PresetExclusion>? exclusions = null,
        IProgress<string>? progress = null,
        CancellationToken ct = default)
    {
        var rulesList = rules.ToList();
        var specialList = (specialRules ?? Enumerable.Empty<SpecialFileRule>())
            .Where(r => r.IsActive).ToList();
        var exclusionList = (exclusions ?? Enumerable.Empty<PresetExclusion>())
            .Where(e => e.IsActive).ToList();

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

                    // Exclude .dxdb and .csv — handled in the Data Compare tab
                    if (ExcludedExtensions.Contains(Path.GetExtension(fullPath)))
                        continue;

                    var relPath = fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase)
                        ? fullPath[(root.Length - 1)..] // keep leading backslash
                        : "\\" + fullPath[slot.FolderPath.Length..].TrimStart('\\', '/');

                    var canonical = slot.FolderTypeId.HasValue
                        ? PathTranslationService.Normalize(relPath, slot.FolderTypeId.Value, rulesList)
                        : relPath;

                    if (IsExcluded(canonical, exclusionList) ||
                        HasExcludedSegment(canonical))
                        continue;

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

            // Post-process special rules
            ApplySpecialRules(dict, specialList);
            ApplyUpdatesFilesMatch(dict);

        }, ct);

        return dict.Values.OrderBy(r => r.CanonicalPath, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static void ApplySpecialRules(
        Dictionary<string, ComparisonRow> dict,
        List<SpecialFileRule> specialRules)
    {
        foreach (var rule in specialRules)
        {
            var matching = dict.Values
                .Where(r => r.FileName.Equals(rule.FileNamePattern, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (matching.Count == 0) continue;

            switch (rule.RuleType)
            {
                case SpecialRuleType.OneToMany:
                    ApplyOneToMany(dict, matching);
                    break;

                case SpecialRuleType.FormatConversion:
                    foreach (var row in matching)
                        row.IsFormatConversion = true;
                    break;
            }
        }
    }

    /// <summary>
    /// Collapses N matching rows into one summary row.
    /// Detects the "one" side (slot with a single copy) and the "many" side (slot with multiple copies).
    /// </summary>
    private static void ApplyOneToMany(
        Dictionary<string, ComparisonRow> dict,
        List<ComparisonRow> matching)
    {
        if (matching.Count <= 1) return;

        // Count how many copies exist in each slot
        int countA = matching.Count(r => r.SlotA != null);
        int countB = matching.Count(r => r.SlotB != null);
        int countC = matching.Count(r => r.SlotC != null);
        int countD = matching.Count(r => r.SlotD != null);

        // "One" side = slot with exactly 1 copy; "many" side = slot with > 1 copies
        // If both have 1 copy, no OneToMany consolidation applies
        var slots = new[] { ("A", countA), ("B", countB), ("C", countC), ("D", countD) };
        var oneSlot = slots.FirstOrDefault(s => s.Item2 == 1);
        var manySlot = slots.FirstOrDefault(s => s.Item2 > 1);

        if (oneSlot.Item1 == null || manySlot.Item1 == null) return;

        // Find the single "master" row and the "cloud copy" rows
        ComparisonRow? masterRow = null;
        var copyRows = new List<ComparisonRow>();

        foreach (var row in matching)
        {
            var hasOne = GetSlotInfo(row, oneSlot.Item1) != null;
            var hasMany = GetSlotInfo(row, manySlot.Item1) != null;

            if (hasOne && !hasMany)
                masterRow = row;
            else if (hasMany && !hasOne)
                copyRows.Add(row);
        }

        if (masterRow == null || copyRows.Count == 0) return;

        // Compare master file against each copy by size and date
        var masterInfo = GetSlotInfo(masterRow, oneSlot.Item1)!;
        int matchCount = copyRows.Count(r =>
        {
            var info = GetSlotInfo(r, manySlot.Item1);
            return info != null &&
                   info.Size == masterInfo.Size &&
                   info.LastWrite == masterInfo.LastWrite;
        });

        // Update the master row with summary data; remove the individual copy rows
        masterRow.IsOneToMany = true;
        masterRow.OneToManyTotalCopies = copyRows.Count;
        masterRow.OneToManyMatchingCopies = matchCount;

        foreach (var copyRow in copyRows)
            dict.Remove(copyRow.CanonicalPath);
    }

    private static SlotFileInfo? GetSlotInfo(ComparisonRow row, string slotLabel) =>
        slotLabel switch
        {
            "A" => row.SlotA,
            "B" => row.SlotB,
            "C" => row.SlotC,
            "D" => row.SlotD,
            _ => null
        };

    private static SlotFileInfo? GetAnySlotInfo(ComparisonRow row) =>
        row.SlotA ?? row.SlotB ?? row.SlotC ?? row.SlotD;

    // Files that live directly under NewUserDataUpdates\ on Desktop (no framework subfolder)
    // and under {framework}\#Updates\ on Cloud — one Desktop copy per country, many Cloud copies.
    private static readonly HashSet<string> _updatesFileNames =
        new(StringComparer.OrdinalIgnoreCase) { "Detail.xlsx", "Switch.xlsx" };

    /// <summary>
    /// Detects rows where a single Desktop file at canonical {country}\Frameworks\{file}
    /// should be matched against multiple Cloud files at {country}\Frameworks\{fw}\{file}.
    /// Consolidates them into a single "IsUpdatesMatch" summary row on the Desktop entry.
    /// </summary>
    private static void ApplyUpdatesFilesMatch(Dictionary<string, ComparisonRow> dict)
    {
        var updatesRows = dict.Values
            .Where(r => _updatesFileNames.Contains(r.FileName) && !r.IsOneToMany)
            .ToList();

        if (updatesRows.Count == 0) return;

        // Group by (country, filename) so we handle BW\Detail and ZA\Detail independently.
        var groups = updatesRows.GroupBy(r =>
        {
            var segs = r.CanonicalPath.Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
            return (Country: segs.Length > 0 ? segs[0] : string.Empty, r.FileName);
        });

        foreach (var grp in groups)
        {
            var rows = grp.ToList();

            // Shallow = {country}\Frameworks\{filename}  (3 segments — Desktop side)
            var shallowRows = rows.Where(r =>
            {
                var s = r.CanonicalPath.Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
                return s.Length == 3 &&
                       string.Equals(s[1], "Frameworks", StringComparison.OrdinalIgnoreCase) &&
                       string.Equals(s[2], r.FileName, StringComparison.OrdinalIgnoreCase);
            }).ToList();

            // Deep = {country}\Frameworks\{framework}\{filename}  (4 segments — Cloud side)
            var deepRows = rows.Where(r =>
            {
                var s = r.CanonicalPath.Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
                return s.Length == 4 &&
                       string.Equals(s[1], "Frameworks", StringComparison.OrdinalIgnoreCase) &&
                       string.Equals(s[3], r.FileName, StringComparison.OrdinalIgnoreCase);
            }).ToList();

            if (shallowRows.Count != 1 || deepRows.Count == 0) continue;

            var masterRow  = shallowRows[0];
            var masterInfo = GetAnySlotInfo(masterRow);
            if (masterInfo == null) continue;

            var cloudCopies = new List<string>();
            int matchCount  = 0;

            foreach (var deep in deepRows)
            {
                var info = GetAnySlotInfo(deep);
                if (info == null) continue;
                cloudCopies.Add(info.FullPath);
                if (info.Size == masterInfo.Size && info.LastWrite == masterInfo.LastWrite)
                    matchCount++;
            }

            if (cloudCopies.Count == 0) continue;

            masterRow.IsUpdatesMatch       = true;
            masterRow.UpdatesCloudCopies   = cloudCopies;
            masterRow.UpdatesTotalCopies   = cloudCopies.Count;
            masterRow.UpdatesMatchingCopies = matchCount;

            foreach (var deep in deepRows)
                dict.Remove(deep.CanonicalPath);
        }
    }

    private static bool IsExcluded(string canonicalPath, List<PresetExclusion> exclusions)
    {
        foreach (var ex in exclusions)
        {
            var matched = ex.MatchType switch
            {
                ExclusionMatchType.Contains    => canonicalPath.Contains(ex.Pattern, StringComparison.OrdinalIgnoreCase),
                ExclusionMatchType.StartsWith  => canonicalPath.StartsWith(ex.Pattern, StringComparison.OrdinalIgnoreCase),
                ExclusionMatchType.EndsWith    => canonicalPath.EndsWith(ex.Pattern, StringComparison.OrdinalIgnoreCase),
                _ => false
            };
            if (matched) return true;
        }
        return false;
    }

    /// <summary>
    /// Returns true if any folder segment of <paramref name="canonicalPath"/> is in
    /// <see cref="ExcludedFolderSegments"/>. Segments are split on both separators so
    /// the check works regardless of the normalised path format.
    /// </summary>
    private static bool HasExcludedSegment(string canonicalPath)
    {
        foreach (var seg in canonicalPath.Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries))
            if (ExcludedFolderSegments.Contains(seg))
                return true;
        return false;
    }
}
