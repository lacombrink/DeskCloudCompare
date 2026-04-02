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

    public ObservableCollection<FolderPreset> Presets { get; } = new();
    public ObservableCollection<PresetSlotViewModel> Slots { get; } = new();
    public ObservableCollection<FolderType> FolderTypeOptions { get; } = new();

    [ObservableProperty]
    private FolderPreset? _selectedPreset;

    public event Action<FolderPreset>? LoadPresetRequested;

    public PresetsViewModel(PresetService presetService)
    {
        _presetService = presetService;
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
        if (value == null) return;
        foreach (var slot in value.Slots.OrderBy(s => s.SlotLabel))
            Slots.Add(new PresetSlotViewModel(slot, FolderTypeOptions));
    }

    [RelayCommand]
    private async Task AddPreset()
    {
        var preset = await _presetService.AddAsync("New Preset");
        // Reload to get slots
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
}
