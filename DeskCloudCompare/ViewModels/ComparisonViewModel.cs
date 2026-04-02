using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeskCloudCompare.Models;
using DeskCloudCompare.Services;
using System.Collections.ObjectModel;
using System.Windows;

namespace DeskCloudCompare.ViewModels;

public partial class ComparisonViewModel : ObservableObject
{
    private readonly FolderScanService _scanService;
    private readonly BinaryCompareService _binaryService;
    private readonly PathTranslationService _pathTranslationService;

    private CancellationTokenSource? _cts;

    public ObservableCollection<PresetSlotViewModel> Slots { get; } = new();
    public ObservableCollection<ComparisonRowViewModel> Rows { get; } = new();
    public ObservableCollection<FolderType> FolderTypeOptions { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusMessage = "Ready";

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

            StatusMessage = $"Scan complete — {Rows.Count} files found.";
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
    private void Cancel()
    {
        _cts?.Cancel();
    }

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
