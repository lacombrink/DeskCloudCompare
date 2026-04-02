using CommunityToolkit.Mvvm.ComponentModel;
using DeskCloudCompare.Models;

namespace DeskCloudCompare.ViewModels;

public partial class FolderTypeRowViewModel : ObservableObject
{
    public FolderType Entity { get; }

    [ObservableProperty]
    private string _name;

    public FolderTypeRowViewModel(FolderType entity)
    {
        Entity = entity;
        _name = entity.Name;
    }

    partial void OnNameChanged(string value) => Entity.Name = value;
}
