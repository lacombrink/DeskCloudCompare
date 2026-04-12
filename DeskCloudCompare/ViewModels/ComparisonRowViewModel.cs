using CommunityToolkit.Mvvm.ComponentModel;
using DeskCloudCompare.Services;

namespace DeskCloudCompare.ViewModels;

public partial class ComparisonRowViewModel : ObservableObject
{
    public string CanonicalPath { get; }
    public string FileName { get; }

    // Paths
    public string? PathA { get; }
    public string? PathB { get; }
    public string? PathC { get; }
    public string? PathD { get; }

    // Sizes (bytes)
    public long? SizeA { get; }
    public long? SizeB { get; }
    public long? SizeC { get; }
    public long? SizeD { get; }

    // Dates
    public DateTime? DateA { get; }
    public DateTime? DateB { get; }
    public DateTime? DateC { get; }
    public DateTime? DateD { get; }

    public string Result { get; }

    [ObservableProperty]
    private string? _binaryResult;

    [ObservableProperty]
    private bool _isSelectedForBinaryCompare;

    /// <summary>True when this file exists once in one slot and N times in another slot.</summary>
    public bool IsOneToMany { get; }

    /// <summary>Total number of copies on the "many" side.</summary>
    public int OneToManyTotal { get; }

    /// <summary>Number of copies on the "many" side that match the master by size and date.</summary>
    public int OneToManyMatching { get; }

    /// <summary>
    /// True when a single Desktop file (at canonical {country}\Frameworks\{file}) matches
    /// multiple Cloud files (at {country}\Frameworks\{fw}\{file}).  Row is highlighted green.
    /// </summary>
    public bool IsUpdatesMatch { get; }

    /// <summary>Full paths of each Cloud copy for binary compare.</summary>
    public IReadOnlyList<string> UpdatesCloudCopies { get; }

    public bool NeedsCompare => IsUpdatesMatch || (Result != "All identical" && !IsOneToMany);

    // Raw paths for binary compare service
    public IReadOnlyDictionary<string, string?> SlotPaths => new Dictionary<string, string?>
    {
        { "A", PathA },
        { "B", PathB },
        { "C", PathC },
        { "D", PathD }
    };

    public ComparisonRowViewModel(ComparisonRow row, IReadOnlyList<string> activeSlotLabels)
    {
        CanonicalPath = row.CanonicalPath;
        FileName = row.FileName;

        PathA = row.SlotA?.FullPath;
        PathB = row.SlotB?.FullPath;
        PathC = row.SlotC?.FullPath;
        PathD = row.SlotD?.FullPath;

        SizeA = row.SlotA?.Size;
        SizeB = row.SlotB?.Size;
        SizeC = row.SlotC?.Size;
        SizeD = row.SlotD?.Size;

        DateA = row.SlotA?.LastWrite;
        DateB = row.SlotB?.LastWrite;
        DateC = row.SlotC?.LastWrite;
        DateD = row.SlotD?.LastWrite;

        IsOneToMany = row.IsOneToMany;
        OneToManyTotal = row.OneToManyTotalCopies;
        OneToManyMatching = row.OneToManyMatchingCopies;

        IsUpdatesMatch   = row.IsUpdatesMatch;
        UpdatesCloudCopies = row.UpdatesCloudCopies;

        Result = row.IsUpdatesMatch
            ? $"1→{row.UpdatesTotalCopies} cloud copies ({row.UpdatesMatchingCopies}/{row.UpdatesTotalCopies} match)"
            : row.IsOneToMany
                ? $"1→{row.OneToManyTotalCopies} copies ({row.OneToManyMatchingCopies}/{row.OneToManyTotalCopies} match)"
                : ComputeResult(row, activeSlotLabels);
    }

    private static string ComputeResult(ComparisonRow row, IReadOnlyList<string> activeSlots)
    {
        var infos = new Dictionary<string, SlotFileInfo?>
        {
            { "A", row.SlotA },
            { "B", row.SlotB },
            { "C", row.SlotC },
            { "D", row.SlotD }
        };

        var missing = activeSlots.Where(l => infos[l] == null).ToList();
        if (missing.Count == activeSlots.Count) return "All missing";
        if (missing.Count > 0) return $"Missing in {string.Join(", ", missing)}";

        var present = activeSlots.Select(l => infos[l]!).ToList();

        // Check sizes
        var distinctSizes = present.Select(x => x.Size).Distinct().ToList();
        if (distinctSizes.Count > 1)
        {
            var diffLabels = activeSlots.Where(l =>
                infos[l]!.Size != present[0].Size).ToList();
            return $"Size differs: {string.Join(", ", diffLabels)}";
        }

        // Check dates
        var distinctDates = present.Select(x => x.LastWrite).Distinct().ToList();
        if (distinctDates.Count > 1)
        {
            var diffLabels = activeSlots.Where(l =>
                infos[l]!.LastWrite != present[0].LastWrite).ToList();
            return $"Date differs: {string.Join(", ", diffLabels)}";
        }

        return "All identical";
    }
}
