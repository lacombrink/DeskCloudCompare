using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeskCloudCompare.Models;
using DeskCloudCompare.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;

namespace DeskCloudCompare.ViewModels;

public partial class ComparisonViewModel : ObservableObject
{
    private readonly FolderScanService _scanService;
    private readonly BinaryCompareService _binaryService;
    private readonly PathTranslationService _pathTranslationService;

    private CancellationTokenSource? _cts;
    private ICollectionView? _filteredRows;

    public ObservableCollection<PresetSlotViewModel> Slots { get; } = new();
    public ObservableCollection<ComparisonRowViewModel> Rows { get; } = new();
    public ObservableCollection<FolderType> FolderTypeOptions { get; } = new();

    public ICollectionView FilteredRows
    {
        get
        {
            if (_filteredRows == null)
            {
                _filteredRows = CollectionViewSource.GetDefaultView(Rows);
                _filteredRows.Filter = ApplyFilter;
            }
            return _filteredRows;
        }
    }

    // --- Filter properties — each one triggers a view refresh ---

    [ObservableProperty] private string _filterFileName = string.Empty;
    [ObservableProperty] private string _filterCanonicalPath = string.Empty;
    [ObservableProperty] private string _filterPathA = string.Empty;
    [ObservableProperty] private string _filterPathB = string.Empty;
    [ObservableProperty] private string _filterPathC = string.Empty;
    [ObservableProperty] private string _filterPathD = string.Empty;
    [ObservableProperty] private string _filterSizeA = string.Empty;
    [ObservableProperty] private string _filterSizeB = string.Empty;
    [ObservableProperty] private string _filterSizeC = string.Empty;
    [ObservableProperty] private string _filterSizeD = string.Empty;
    [ObservableProperty] private string _filterDateA = string.Empty;
    [ObservableProperty] private string _filterDateB = string.Empty;
    [ObservableProperty] private string _filterDateC = string.Empty;
    [ObservableProperty] private string _filterDateD = string.Empty;
    [ObservableProperty] private string _filterResult = string.Empty;
    [ObservableProperty] private string _filterBinaryResult = string.Empty;

    partial void OnFilterFileNameChanged(string value) => RefreshFilters();
    partial void OnFilterCanonicalPathChanged(string value) => RefreshFilters();
    partial void OnFilterPathAChanged(string value) => RefreshFilters();
    partial void OnFilterPathBChanged(string value) => RefreshFilters();
    partial void OnFilterPathCChanged(string value) => RefreshFilters();
    partial void OnFilterPathDChanged(string value) => RefreshFilters();
    partial void OnFilterSizeAChanged(string value) => RefreshFilters();
    partial void OnFilterSizeBChanged(string value) => RefreshFilters();
    partial void OnFilterSizeCChanged(string value) => RefreshFilters();
    partial void OnFilterSizeDChanged(string value) => RefreshFilters();
    partial void OnFilterDateAChanged(string value) => RefreshFilters();
    partial void OnFilterDateBChanged(string value) => RefreshFilters();
    partial void OnFilterDateCChanged(string value) => RefreshFilters();
    partial void OnFilterDateDChanged(string value) => RefreshFilters();
    partial void OnFilterResultChanged(string value) => RefreshFilters();
    partial void OnFilterBinaryResultChanged(string value) => RefreshFilters();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private string _filterStatus = string.Empty;

    public bool IsNotBusy => !IsBusy;

    public ComparisonViewModel(
        FolderScanService scanService,
        BinaryCompareService binaryService,
        PathTranslationService pathTranslationService)
    {
        _scanService = scanService;
        _binaryService = binaryService;
        _pathTranslationService = pathTranslationService;
    }

    public void Initialize(IEnumerable<FolderType> folderTypes)
    {
        FolderTypeOptions.Clear();
        foreach (var t in folderTypes)
            FolderTypeOptions.Add(t);

        if (Slots.Count == 0)
        {
            foreach (var label in new[] { "A", "B", "C", "D" })
            {
                var slot = new FolderPresetSlot { SlotLabel = label };
                Slots.Add(new PresetSlotViewModel(slot, FolderTypeOptions));
            }
        }
    }

    public void LoadPreset(FolderPreset preset)
    {
        Slots.Clear();
        foreach (var slot in preset.Slots.OrderBy(s => s.SlotLabel))
            Slots.Add(new PresetSlotViewModel(slot, FolderTypeOptions));
    }

    private void RefreshFilters()
    {
        FilteredRows.Refresh();
        UpdateFilterStatus();
    }

    private void UpdateFilterStatus()
    {
        var visible = FilteredRows.Cast<object>().Count();
        var total = Rows.Count;
        FilterStatus = total == 0 ? string.Empty
            : visible == total ? $"{total:N0} files"
            : $"{visible:N0} of {total:N0} files (filtered)";
    }

    private bool ApplyFilter(object obj)
    {
        if (obj is not ComparisonRowViewModel row) return false;

        if (!Matches(row.FileName, FilterFileName)) return false;
        if (!Matches(row.CanonicalPath, FilterCanonicalPath)) return false;
        if (!Matches(row.PathA, FilterPathA)) return false;
        if (!Matches(row.PathB, FilterPathB)) return false;
        if (!Matches(row.PathC, FilterPathC)) return false;
        if (!Matches(row.PathD, FilterPathD)) return false;
        if (!Matches(row.SizeA?.ToString("N0"), FilterSizeA)) return false;
        if (!Matches(row.SizeB?.ToString("N0"), FilterSizeB)) return false;
        if (!Matches(row.SizeC?.ToString("N0"), FilterSizeC)) return false;
        if (!Matches(row.SizeD?.ToString("N0"), FilterSizeD)) return false;
        if (!Matches(row.DateA?.ToString("yyyy-MM-dd HH:mm:ss"), FilterDateA)) return false;
        if (!Matches(row.DateB?.ToString("yyyy-MM-dd HH:mm:ss"), FilterDateB)) return false;
        if (!Matches(row.DateC?.ToString("yyyy-MM-dd HH:mm:ss"), FilterDateC)) return false;
        if (!Matches(row.DateD?.ToString("yyyy-MM-dd HH:mm:ss"), FilterDateD)) return false;
        if (!Matches(row.Result, FilterResult)) return false;
        if (!Matches(row.BinaryResult, FilterBinaryResult)) return false;

        return true;
    }

    private static bool Matches(string? value, string filter) =>
        string.IsNullOrEmpty(filter) ||
        (value?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false);

    [RelayCommand]
    private void ClearFilters()
    {
        FilterFileName = FilterCanonicalPath = string.Empty;
        FilterPathA = FilterPathB = FilterPathC = FilterPathD = string.Empty;
        FilterSizeA = FilterSizeB = FilterSizeC = FilterSizeD = string.Empty;
        FilterDateA = FilterDateB = FilterDateC = FilterDateD = string.Empty;
        FilterResult = FilterBinaryResult = string.Empty;
    }

    [RelayCommand]
    private async Task Scan()
    {
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;

        IsBusy = true;
        Rows.Clear();
        StatusMessage = "Loading translation rules...";

        try
        {
            var rules = await _pathTranslationService.GetAllAsync();
            var slotConfigs = Slots.Select(s => new SlotConfig(
                s.SlotLabel,
                s.FolderPath ?? string.Empty,
                s.SelectedFolderType?.Id)).ToList();

            var activeLabels = slotConfigs
                .Where(s => !string.IsNullOrWhiteSpace(s.FolderPath))
                .Select(s => s.Label)
                .ToList();

            var progress = new Progress<string>(msg => StatusMessage = msg);
            var scanResults = await _scanService.ScanAsync(slotConfigs, rules, progress, ct);

            foreach (var row in scanResults)
                Rows.Add(new ComparisonRowViewModel(row, activeLabels));

            FilteredRows.Refresh();
            StatusMessage = $"Scan complete — {Rows.Count:N0} files found.";
            UpdateFilterStatus();
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Scan cancelled.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Scan Error", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusMessage = "Scan failed.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void Cancel() => _cts?.Cancel();

    [RelayCommand]
    private void SelectAllForBinaryCompare()
    {
        foreach (var row in Rows)
            row.IsSelectedForBinaryCompare = row.NeedsCompare;
    }

    [RelayCommand]
    private async Task BinaryCompare()
    {
        var selected = Rows.Where(r => r.IsSelectedForBinaryCompare).ToList();
        if (selected.Count == 0)
        {
            MessageBox.Show("No files selected for binary compare.", "Binary Compare",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;

        IsBusy = true;
        var done = 0;

        try
        {
            foreach (var row in selected)
            {
                ct.ThrowIfCancellationRequested();
                done++;
                StatusMessage = $"Binary compare {done}/{selected.Count}: {row.FileName}";

                var result = await _binaryService.CompareAsync(row.SlotPaths, ct);
                row.BinaryResult = result.AllIdentical ? "Identical" : "Different";
            }
            StatusMessage = $"Binary compare complete — {done} files compared.";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Binary compare cancelled.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Binary Compare Error", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusMessage = "Binary compare failed.";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
