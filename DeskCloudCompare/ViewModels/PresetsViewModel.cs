using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeskCloudCompare.Models;
using DeskCloudCompare.Services;
using System.Collections.ObjectModel;
using System.Windows;

namespace DeskCloudCompare.ViewModels;

public partial class PresetsViewModel : ObservableObject
{
    private readonly PresetService _presetService;
    private readonly PresetExclusionService _exclusionService;

    public ObservableCollection<FolderPreset> Presets { get; } = new();
    public ObservableCollection<PresetSlotViewModel> Slots { get; } = new();
    public ObservableCollection<FolderType> FolderTypeOptions { get; } = new();
    public ObservableCollection<PresetExclusion> Exclusions { get; } = new();
    public IEnumerable<ExclusionMatchType> MatchTypeOptions { get; } = Enum.GetValues<ExclusionMatchType>();

    [ObservableProperty] private FolderPreset? _selectedPreset;
    [ObservableProperty] private PresetExclusion? _selectedExclusion;

    public event Action<FolderPreset>? LoadPresetRequested;

    public PresetsViewModel(PresetService presetService, PresetExclusionService exclusionService)
    {
        _presetService = presetService;
        _exclusionService = exclusionService;
    }

    public async Task LoadAsync(IEnumerable<FolderType> folderTypes)
    {
        FolderTypeOptions.Clear();
        foreach (var t in folderTypes)
            FolderTypeOptions.Add(t);

        var presets = await _presetService.GetAllAsync();
        Presets.Clear();
        foreach (var p in presets)
            Presets.Add(p);
    }

    partial void OnSelectedPresetChanged(FolderPreset? value)
    {
        Slots.Clear();
        Exclusions.Clear();
        if (value == null) return;
        foreach (var slot in value.Slots.OrderBy(s => s.SlotLabel))
            Slots.Add(new PresetSlotViewModel(slot, FolderTypeOptions));
        _ = LoadExclusionsAsync(value.Id);
    }

    private async Task LoadExclusionsAsync(int presetId)
    {
        var list = await _exclusionService.GetByPresetAsync(presetId);
        Exclusions.Clear();
        foreach (var e in list)
            Exclusions.Add(e);
    }

    [RelayCommand]
    private async Task AddPreset()
    {
        var preset = await _presetService.AddAsync("New Preset");
        var loaded = await _presetService.GetAllAsync();
        Presets.Clear();
        foreach (var p in loaded)
            Presets.Add(p);
        SelectedPreset = Presets.FirstOrDefault(p => p.Id == preset.Id);
    }

    [RelayCommand]
    private async Task DeletePreset()
    {
        if (SelectedPreset == null) return;
        if (MessageBox.Show($"Delete preset '{SelectedPreset.Name}'?", "Confirm",
            MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        await _presetService.DeleteAsync(SelectedPreset.Id);
        Presets.Remove(SelectedPreset);
        SelectedPreset = null;
    }

    [RelayCommand]
    private async Task SavePreset()
    {
        if (SelectedPreset == null) return;
        await _presetService.UpdateAsync();
    }

    [RelayCommand]
    private void LoadIntoComparison()
    {
        if (SelectedPreset == null) return;
        LoadPresetRequested?.Invoke(SelectedPreset);
    }

    // --- Exclusions ---

    [RelayCommand]
    private async Task AddExclusion()
    {
        if (SelectedPreset == null) return;
        var exclusion = new PresetExclusion
        {
            PresetId = SelectedPreset.Id,
            Pattern = string.Empty,
            MatchType = ExclusionMatchType.Contains,
            IsActive = true
        };
        await _exclusionService.AddAsync(exclusion);
        Exclusions.Add(exclusion);
        SelectedExclusion = exclusion;
    }

    [RelayCommand]
    private async Task DeleteExclusion()
    {
        if (SelectedExclusion == null) return;
        await _exclusionService.DeleteAsync(SelectedExclusion.Id);
        Exclusions.Remove(SelectedExclusion);
        SelectedExclusion = null;
    }

    [RelayCommand]
    private async Task SaveExclusions() => await _exclusionService.UpdateAsync();
}
