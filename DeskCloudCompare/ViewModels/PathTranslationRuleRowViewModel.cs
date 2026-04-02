using CommunityToolkit.Mvvm.ComponentModel;
using DeskCloudCompare.Models;
using System.Collections.ObjectModel;

namespace DeskCloudCompare.ViewModels;

public partial class PathTranslationRuleRowViewModel : ObservableObject
{
    public PathTranslationRule Entity { get; }
    public ObservableCollection<FolderType> FolderTypeOptions { get; }

    [ObservableProperty]
    private FolderType? _fromType;

    [ObservableProperty]
    private FolderType? _toType;

    [ObservableProperty]
    private string _findText;

    [ObservableProperty]
    private string _replaceText;

    [ObservableProperty]
    private int _sortOrder;

    public PathTranslationRuleRowViewModel(PathTranslationRule entity, ObservableCollection<FolderType> folderTypeOptions)
    {
        Entity = entity;
        FolderTypeOptions = folderTypeOptions;
        _fromType = entity.FromType;
        _toType = entity.ToType;
        _findText = entity.FindText;
        _replaceText = entity.ReplaceText;
        _sortOrder = entity.SortOrder;
    }

    partial void OnFromTypeChanged(FolderType? value)
    {
        if (value != null)
        {
            Entity.FromTypeId = value.Id;
            Entity.FromType = value;
        }
    }

    partial void OnToTypeChanged(FolderType? value)
    {
        if (value != null)
        {
            Entity.ToTypeId = value.Id;
            Entity.ToType = value;
        }
    }

    partial void OnFindTextChanged(string value) => Entity.FindText = value;
    partial void OnReplaceTextChanged(string value) => Entity.ReplaceText = value;
    partial void OnSortOrderChanged(int value) => Entity.SortOrder = value;
}
