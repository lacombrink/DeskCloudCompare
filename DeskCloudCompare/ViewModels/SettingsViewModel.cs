using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeskCloudCompare.Models;
using DeskCloudCompare.Services;
using System.Collections.ObjectModel;
using System.Windows;

namespace DeskCloudCompare.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly FolderTypeService _folderTypeService;
    private readonly PathTranslationService _pathTranslationService;

    public ObservableCollection<FolderTypeRowViewModel> FolderTypes { get; } = new();
    public ObservableCollection<PathTranslationRuleRowViewModel> TranslationRules { get; } = new();
    public ObservableCollection<FolderType> FolderTypeOptions { get; } = new();

    [ObservableProperty]
    private FolderTypeRowViewModel? _selectedFolderType;

    [ObservableProperty]
    private PathTranslationRuleRowViewModel? _selectedRule;

    public SettingsViewModel(FolderTypeService folderTypeService, PathTranslationService pathTranslationService)
    {
        _folderTypeService = folderTypeService;
        _pathTranslationService = pathTranslationService;
    }

    public async Task LoadAsync()
    {
        var types = await _folderTypeService.GetAllAsync();
        FolderTypes.Clear();
        FolderTypeOptions.Clear();
        foreach (var t in types)
        {
            FolderTypes.Add(new FolderTypeRowViewModel(t));
            FolderTypeOptions.Add(t);
        }

        var rules = await _pathTranslationService.GetAllAsync();
        TranslationRules.Clear();
        foreach (var r in rules)
            TranslationRules.Add(new PathTranslationRuleRowViewModel(r, FolderTypeOptions));
    }

    [RelayCommand]
    private async Task AddFolderType()
    {
        var type = await _folderTypeService.AddAsync("New Type");
        var row = new FolderTypeRowViewModel(type);
        FolderTypes.Add(row);
        FolderTypeOptions.Add(type);
        SelectedFolderType = row;
    }

    [RelayCommand]
    private async Task DeleteFolderType()
    {
        if (SelectedFolderType == null) return;
        try
        {
            await _folderTypeService.DeleteAsync(SelectedFolderType.Entity.Id);
            FolderTypeOptions.Remove(SelectedFolderType.Entity);
            FolderTypes.Remove(SelectedFolderType);
        }
        catch (InvalidOperationException ex)
        {
            MessageBox.Show(ex.Message, "Cannot Delete", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    [RelayCommand]
    private async Task SaveFolderTypes()
    {
        await _folderTypeService.UpdateAsync();
    }

    [RelayCommand]
    private void AddRule()
    {
        var rule = new PathTranslationRule { FindText = string.Empty, ReplaceText = string.Empty };
        TranslationRules.Add(new PathTranslationRuleRowViewModel(rule, FolderTypeOptions));
    }

    [RelayCommand]
    private async Task DeleteRule()
    {
        if (SelectedRule == null) return;
        if (SelectedRule.Entity.Id > 0)
            await _pathTranslationService.DeleteAsync(SelectedRule.Entity.Id);
        TranslationRules.Remove(SelectedRule);
    }

    [RelayCommand]
    private async Task SaveRules()
    {
        foreach (var row in TranslationRules.Where(r => r.Entity.Id == 0 && r.FromType != null && r.ToType != null))
            await _pathTranslationService.AddAsync(row.Entity);
        await _pathTranslationService.UpdateAsync();
    }
}
