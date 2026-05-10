using System.Collections.ObjectModel;
using Aml.BOM.Import.Application.Models;
using Aml.BOM.Import.Application.Services;
using Aml.BOM.Import.Shared.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

namespace Aml.BOM.Import.UI.ViewModels;

public partial class NewBomsViewModel : ObservableObject
{
    private readonly BomImportService _bomImportService;
    private readonly IBomImportBillRepository _bomBillRepository;
    private readonly IBomValidationService _validationService;

    [ObservableProperty]
    private ObservableCollection<object> _boms = new();

    [ObservableProperty]
    private object? _selectedBom;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private int _totalPendingBoms;

    [ObservableProperty]
    private int _validatedBomsCount;

    [ObservableProperty]
    private int _newMakeItemsCount;

    [ObservableProperty]
    private int _newBuyItemsCount;

    [ObservableProperty]
    private int _duplicateBomsCount;

    [ObservableProperty]
    private int _failedBomsCount;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    public NewBomsViewModel(
        BomImportService bomImportService, 
        IBomImportBillRepository bomBillRepository,
        IBomValidationService validationService)
    {
        _bomImportService = bomImportService;
        _bomBillRepository = bomBillRepository;
        _validationService = validationService;
        LoadBomsCommand.Execute(null);
    }

    [RelayCommand]
    private async Task LoadBoms()
    {
        IsLoading = true;
        StatusMessage = "Loading BOMs...";
        
        try
        {
            // Load BOM data
            var boms = await _bomImportService.GetAllBomsAsync();
            Boms = new ObservableCollection<object>(boms);

            // Load statistics from isBOMImportBills table
            await LoadBomStatisticsAsync();

            StatusMessage = "Ready";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading BOMs: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadBomStatisticsAsync()
    {
        try
        {
            // Get status summary from repository
            var statusSummary = await _bomBillRepository.GetStatusSummaryAsync();

            // Update counts based on status
            ValidatedBomsCount = statusSummary.ContainsKey("Validated") ? statusSummary["Validated"] : 0;
            NewMakeItemsCount = statusSummary.ContainsKey("NewMakeItem") ? statusSummary["NewMakeItem"] : 0;
            NewBuyItemsCount = statusSummary.ContainsKey("NewBuyItem") ? statusSummary["NewBuyItem"] : 0;
            DuplicateBomsCount = statusSummary.ContainsKey("Duplicate") ? statusSummary["Duplicate"] : 0;
            FailedBomsCount = statusSummary.ContainsKey("Failed") ? statusSummary["Failed"] : 0;

            // Calculate total pending (exclude Integrated and Duplicate)
            TotalPendingBoms = statusSummary
                .Where(kvp => kvp.Key != "Integrated" && kvp.Key != "Duplicate")
                .Sum(kvp => kvp.Value);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading statistics: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ImportFile()
    {
        var openFileDialog = new OpenFileDialog
        {
            Filter = "Excel Files (*.xlsx;*.xls)|*.xlsx;*.xls|All Files (*.*)|*.*",
            Title = "Select BOM Import File"
        };

        if (openFileDialog.ShowDialog() == true)
        {
            IsLoading = true;
            StatusMessage = "Importing file...";
            
            try
            {
                var request = new ImportFileRequest
                {
                    FilePath = openFileDialog.FileName,
                    ImportedBy = Environment.UserName
                };

                var result = await _bomImportService.ImportBomFileAsync(request);
                
                if (result.Success)
                {
                    StatusMessage = $"Import successful: {result.ImportedRecords} records imported, {result.ValidatedRecords} validated";
                    
                    // Show detailed message
                    System.Windows.MessageBox.Show(
                        $"File: {result.FileName}\n" +
                        $"Records Imported: {result.ImportedRecords}\n" +
                        $"Tabs Processed: {result.TabsProcessed}\n\n" +
                        $"Validation Results:\n" +
                        $"  Validated: {result.ValidatedRecords}\n" +
                        $"  New Buy Items: {result.NewBuyItems}\n" +
                        $"  New Make Items: {result.NewMakeItems}\n" +
                        $"  Duplicates: {result.DuplicateBoms}\n" +
                        $"  Failed: {result.FailedRecords}",
                        "Import Complete",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Information);
                }
                else
                {
                    StatusMessage = $"Import failed: {result.Message}";
                    System.Windows.MessageBox.Show(
                        result.Message,
                        "Import Failed",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Error);
                }
                
                await LoadBoms();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Import error: {ex.Message}";
                System.Windows.MessageBox.Show(
                    $"Error importing file: {ex.Message}",
                    "Import Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }
    }

    [RelayCommand]
    private async Task RevalidateAll()
    {
        IsLoading = true;
        StatusMessage = "Re-validating all pending BOMs...";
        
        try
        {
            var result = await _validationService.RevalidateAllPendingAsync();
            
            StatusMessage = $"Re-validation complete: {result.ValidatedRecords} validated, {result.FailedRecords} failed";
            
            System.Windows.MessageBox.Show(
                $"Re-validation Results:\n\n" +
                $"Total Records: {result.TotalRecords}\n" +
                $"Validated: {result.ValidatedRecords}\n" +
                $"New Buy Items: {result.NewBuyItems}\n" +
                $"New Make Items: {result.NewMakeItems}\n" +
                $"Duplicates: {result.DuplicateBoms}\n" +
                $"Failed: {result.FailedRecords}",
                "Re-validation Complete",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
            
            await LoadBoms();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Re-validation error: {ex.Message}";
            System.Windows.MessageBox.Show(
                $"Error during re-validation: {ex.Message}",
                "Re-validation Error",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
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
