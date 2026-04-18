using System.Collections.ObjectModel;
using Aml.BOM.Import.Application.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Aml.BOM.Import.UI.ViewModels;

public partial class NewMakeItemsViewModel : ObservableObject
{
    private readonly NewItemService _newItemService;

    [ObservableProperty]
    private ObservableCollection<object> _items = new();

    [ObservableProperty]
    private ObservableCollection<object> _selectedItems = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _bulkEditField = string.Empty;

    [ObservableProperty]
    private string _bulkEditValue = string.Empty;

    public NewMakeItemsViewModel(NewItemService newItemService)
    {
        _newItemService = newItemService;
        LoadItemsCommand.Execute(null);
    }

    [RelayCommand]
    private async Task LoadItems()
    {
        IsLoading = true;
        try
        {
            var items = await _newItemService.GetNewMakeItemsAsync();
            Items = new ObservableCollection<object>(items);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Refresh()
    {
        await LoadItems();
    }

    [RelayCommand]
    private async Task BulkEdit()
    {
        // TODO: Implement bulk edit functionality
        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task CopyFromItem(string itemCode)
    {
        // TODO: Implement copy from item functionality
        await Task.CompletedTask;
    }
}
