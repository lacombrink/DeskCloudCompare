using CommunityToolkit.Mvvm.ComponentModel;
using DeskCloudCompare.Models;
using DeskCloudCompare.Services;

namespace DeskCloudCompare.ViewModels;

public partial class MainViewModel : ObservableObject
{
    public SettingsViewModel Settings { get; }
    public PresetsViewModel Presets { get; }
    public ComparisonViewModel Comparison { get; }

    [ObservableProperty]
    private int _selectedTabIndex;

    public MainViewModel(
        SettingsViewModel settings,
        PresetsViewModel presets,
        ComparisonViewModel comparison)
    {
        Settings = settings;
        Presets = presets;
        Comparison = comparison;

        // When user clicks "Load into Comparison" in Presets view
        Presets.LoadPresetRequested += preset =>
        {
            Comparison.LoadPreset(preset);
            SelectedTabIndex = 0; // Switch to Comparison tab
        };
    }

    public async Task LoadAsync()
    {
        await Settings.LoadAsync();
        await Presets.LoadAsync(Settings.FolderTypeOptions);
        Comparison.Initialize(Settings.FolderTypeOptions);
    }
}
