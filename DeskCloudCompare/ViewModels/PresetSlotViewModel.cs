using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeskCloudCompare.Models;
using Microsoft.Win32;
using System.Collections.ObjectModel;

namespace DeskCloudCompare.ViewModels;

public partial class PresetSlotViewModel : ObservableObject
{
    public FolderPresetSlot Entity { get; }
    public ObservableCollection<FolderType> FolderTypeOptions { get; }

    public string SlotLabel => Entity.SlotLabel;

    [ObservableProperty]
    private string? _folderPath;

    [ObservableProperty]
    private FolderType? _selectedFolderType;

    public PresetSlotViewModel(FolderPresetSlot entity, ObservableCollection<FolderType> folderTypeOptions)
    {
        Entity = entity;
        FolderTypeOptions = folderTypeOptions;
        _folderPath = entity.FolderPath;
        _selectedFolderType = entity.FolderType;
    }

    partial void OnFolderPathChanged(string? value)
    {
        Entity.FolderPath = value;
    }

    partial void OnSelectedFolderTypeChanged(FolderType? value)
    {
        Entity.FolderTypeId = value?.Id;
        Entity.FolderType = value;
    }

    [RelayCommand]
    private void Browse()
    {
        var dialog = new OpenFolderDialog
        {
            Title = $"Select Folder for Slot {SlotLabel}",
            Multiselect = false
        };
        if (dialog.ShowDialog() == true)
            FolderPath = dialog.FolderName;
    }
}
