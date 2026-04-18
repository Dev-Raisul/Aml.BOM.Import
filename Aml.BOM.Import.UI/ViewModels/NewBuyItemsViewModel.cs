using System.Collections.ObjectModel;
using Aml.BOM.Import.Application.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Aml.BOM.Import.UI.ViewModels;

public partial class NewBuyItemsViewModel : ObservableObject
{
    private readonly NewItemService _newItemService;

    [ObservableProperty]
    private ObservableCollection<object> _items = new();

    [ObservableProperty]
    private object? _selectedItem;

    [ObservableProperty]
    private bool _isLoading;

    public NewBuyItemsViewModel(NewItemService newItemService)
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
            var items = await _newItemService.GetNewBuyItemsAsync();
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
}
