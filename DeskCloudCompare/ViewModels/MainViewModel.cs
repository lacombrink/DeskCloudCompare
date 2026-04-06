using CommunityToolkit.Mvvm.ComponentModel;

namespace DeskCloudCompare.ViewModels;

public partial class MainViewModel : ObservableObject
{
    public SettingsViewModel Settings { get; }
    public PresetsViewModel Presets { get; }
    public ComparisonViewModel Comparison { get; }
    public CountryManagerViewModel CountryManager { get; }
    public FrameworkManagerViewModel FrameworkManager { get; }
    public SubFrameworkManagerViewModel SubFrameworkManager { get; }

    [ObservableProperty]
    private int _selectedTabIndex;

    public MainViewModel(
        SettingsViewModel settings,
        PresetsViewModel presets,
        ComparisonViewModel comparison,
        CountryManagerViewModel countryManager,
        FrameworkManagerViewModel frameworkManager,
        SubFrameworkManagerViewModel subFrameworkManager)
    {
        Settings = settings;
        Presets = presets;
        Comparison = comparison;
        CountryManager = countryManager;
        FrameworkManager = frameworkManager;
        SubFrameworkManager = subFrameworkManager;

        // When user clicks "Load into Comparison" in Presets view
        Presets.LoadPresetRequested += preset =>
        {
            Comparison.LoadPreset(preset, Presets.Exclusions);
            SelectedTabIndex = 2; // DeskCloud Manager tab (index after reorder)
        };
    }

    public async Task LoadAsync()
    {
        await Settings.LoadAsync();
        await Presets.LoadAsync(Settings.FolderTypeOptions);
        Comparison.Initialize(Settings.FolderTypeOptions);
        await CountryManager.LoadAsync();
        await FrameworkManager.LoadAsync();
        await SubFrameworkManager.LoadAsync();
    }
}
