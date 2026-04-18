using System.Collections.ObjectModel;
using Aml.BOM.Import.Application.Models;
using Aml.BOM.Import.Application.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

namespace Aml.BOM.Import.UI.ViewModels;

public partial class NewBomsViewModel : ObservableObject
{
    private readonly BomImportService _bomImportService;

    [ObservableProperty]
    private ObservableCollection<object> _boms = new();

    [ObservableProperty]
    private object? _selectedBom;

    [ObservableProperty]
    private bool _isLoading;

    public NewBomsViewModel(BomImportService bomImportService)
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
            var boms = await _bomImportService.GetAllBomsAsync();
            Boms = new ObservableCollection<object>(boms);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ImportFile()
    {
        var openFileDialog = new OpenFileDialog
        {
            Filter = "CSV Files (*.csv)|*.csv|Excel Files (*.xlsx;*.xls)|*.xlsx;*.xls|All Files (*.*)|*.*",
            Title = "Select BOM Import File"
        };

        if (openFileDialog.ShowDialog() == true)
        {
            IsLoading = true;
            try
            {
                var request = new ImportFileRequest
                {
                    FilePath = openFileDialog.FileName,
                    ImportedBy = Environment.UserName
                };

                var result = await _bomImportService.ImportBomFileAsync(request);
                
                // TODO: Show result message to user
                
                await LoadBoms();
            }
            finally
            {
                IsLoading = false;
            }
        }
    }

    [RelayCommand]
    private async Task ValidateBom(int bomId)
    {
        // TODO: Implement BOM validation
        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task Refresh()
    {
        await LoadBoms();
    }
}
