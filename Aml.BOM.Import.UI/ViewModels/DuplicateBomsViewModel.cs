using System.Collections.ObjectModel;
using Aml.BOM.Import.Application.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Aml.BOM.Import.UI.ViewModels;

public partial class DuplicateBomsViewModel : ObservableObject
{
    private readonly BomImportService _bomImportService;

    [ObservableProperty]
    private ObservableCollection<object> _boms = new();

    [ObservableProperty]
    private object? _selectedBom;

    [ObservableProperty]
    private bool _isLoading;

    public DuplicateBomsViewModel(BomImportService bomImportService)
    {
        _bomImportService = bomImportService;
        LoadBomsCommand.Execute(null);
    }

    [RelayCommand]
    private async Task LoadBoms()
    {
        IsLoading = true;
        try
        {
            // TODO: Filter by duplicate status
            var boms = await _bomImportService.GetAllBomsAsync();
            Boms = new ObservableCollection<object>(boms);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Refresh()
    {
        await LoadBoms();
    }
}
