using System.Collections.ObjectModel;
using Aml.BOM.Import.Domain.Models;
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
    private readonly IBomIntegrationService _bomIntegrationService;
    private readonly ILoggerService _logger;
    private readonly MainWindowViewModel _mainWindowViewModel;

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
    private int _totalValidatedRecords;

    [ObservableProperty]
    private int _newMakeItemsCount;

    [ObservableProperty]
    private int _newBuyItemsCount;

    [ObservableProperty]
    private int _duplicateBomsCount;

    [ObservableProperty]
    private int _failedBomsCount;

    [ObservableProperty]
    private int _newMakeItemsParentCount;

    [ObservableProperty]
    private int _newBuyItemsParentCount;

    [ObservableProperty]
    private int _duplicateBomsParentCount;

    [ObservableProperty]
    private int _totalPendingBomsParentCount;

    [ObservableProperty]
    private int _validatedBomsParentCount;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    // Computed property for not validated count (uses total validated records, not ready to integrate count)
    public int NotValidatedCount => TotalPendingBoms - TotalValidatedRecords;

    public NewBomsViewModel(
        BomImportService bomImportService, 
        IBomImportBillRepository bomBillRepository,
        IBomValidationService validationService,
        IBomIntegrationService bomIntegrationService,
        ILoggerService logger,
        MainWindowViewModel mainWindowViewModel)
    {
        _bomImportService = bomImportService;
        _bomBillRepository = bomBillRepository;
        _validationService = validationService;
        _bomIntegrationService = bomIntegrationService;
        _logger = logger;
        _mainWindowViewModel = mainWindowViewModel;
        LoadBomsCommand.Execute(null);
    }

    [RelayCommand]
    private void NavigateToNewMakeItems() => _mainWindowViewModel.NavigateCommand.Execute("NewMakeItems");

    [RelayCommand]
    private void NavigateToNewBuyItems() => _mainWindowViewModel.NavigateCommand.Execute("NewBuyItems");

    [RelayCommand]
    private void NavigateToDuplicateBoms() => _mainWindowViewModel.NavigateCommand.Execute("DuplicateBoms");

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

            // Get ready to integrate count (records with Status='Ready')
            ValidatedBomsCount = statusSummary.ContainsKey("Ready") ? statusSummary["Ready"] : 0;
            
            // Get total validated records (all records with Status='Validated' for the validation label)
            TotalValidatedRecords = statusSummary.ContainsKey("Validated") ? statusSummary["Validated"] : 0;
            
            NewMakeItemsCount = statusSummary.ContainsKey("NewMakeItem") ? statusSummary["NewMakeItem"] : 0;
            NewBuyItemsCount = statusSummary.ContainsKey("NewBuyItem") ? statusSummary["NewBuyItem"] : 0;
            DuplicateBomsCount = statusSummary.ContainsKey("Duplicate") ? statusSummary["Duplicate"] : 0;
            FailedBomsCount = statusSummary.ContainsKey("Failed") ? statusSummary["Failed"] : 0;

            // Calculate total pending (exclude Integrated, Duplicate, and Ready)
            TotalPendingBoms = statusSummary
                .Where(kvp => kvp.Key != "Integrated" && kvp.Key != "Duplicate" && kvp.Key != "Ready")
                .Sum(kvp => kvp.Value);

            // Get parent item counts for each status
            NewMakeItemsParentCount = await _bomBillRepository.GetParentItemCountByStatusAsync("NewMakeItem");
            NewBuyItemsParentCount = await _bomBillRepository.GetParentItemCountByStatusAsync("NewBuyItem");
            DuplicateBomsParentCount = await _bomBillRepository.GetParentItemCountByStatusAsync("Duplicate");

            // Get parent counts for pending and validated (Ready status)
            TotalPendingBomsParentCount = await _bomBillRepository.GetPendingParentItemCountAsync();
            ValidatedBomsParentCount = await _bomBillRepository.GetParentItemCountByReadyStatus();

            // Notify computed property changes
            OnPropertyChanged(nameof(NotValidatedCount));
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

    [RelayCommand]
    private async Task IntegrateBoms()
    {
        IsLoading = true;
        StatusMessage = "Preparing BOMs for integration...";
        
        try
        {
            // Get count of BOMs ready to integrate (Ready status)
            var readyCount = await _bomBillRepository.GetCountByStatusAsync("Ready");
            
            if (readyCount == 0)
            {
                System.Windows.MessageBox.Show(
                    "No BOMs are ready for integration.\n\n" +
                    "BOMs must be validated and cannot contain:\n" +
                    "• New buy items (must be created in Sage first)\n" +
                    "• New make items that haven't been integrated\n\n" +
                    "Once a BOM and all its components are validated, it will be marked as 'Ready'.",
                    "No BOMs Ready",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
                
                StatusMessage = "No BOMs ready for integration";
                return;
            }

            var result = System.Windows.MessageBox.Show(
                $"Ready to integrate {readyCount} BOM record(s) into Sage 100.\n\n" +
                $"This will create Bill of Materials in Sage using Sage Business Logic.\n\n" +
                $"Continue?",
                "Integrate BOMs to Sage",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);

            if (result == System.Windows.MessageBoxResult.No)
            {
                StatusMessage = "BOM integration cancelled";
                return;
            }

            StatusMessage = "Integrating BOMs into Sage 100...";
            
            // Get all Ready parent BOM records
            var readyBills = await _bomBillRepository.GetByStatusAsync("Ready");
            var parentBoms = readyBills
                .Where(b => b.ParentItemCode == null) // Only parent items
                .ToList();

            // Extract parent item codes for batch integration
            var parentItemCodes = parentBoms.Select(p => p.ComponentItemCode).ToList();

            _logger.LogInformation("Starting batch integration of {0} BOMs", parentItemCodes.Count);

            // Use batch integration with shared Sage session
            var (successCount, failedCount, errors) = await _bomIntegrationService.IntegrateBatchBomsAsync(parentItemCodes);

            _logger.LogInformation("Batch integration complete: {0} succeeded, {1} failed", successCount, failedCount);

            // Reload BOMs to reflect updated statuses
            await LoadBoms();

            if (failedCount == 0)
            {
                StatusMessage = $"Successfully integrated {successCount} BOM(s) into Sage 100";
                System.Windows.MessageBox.Show(
                    $"BOM Integration Complete!\n\n" +
                    $"Successfully integrated {successCount} BOM(s) into Sage 100.\n\n" +
                    $"The BOMs have been created in the Bill of Materials module.",
                    "Integration Successful",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
            }
            else
            {
                StatusMessage = $"Integration completed with errors: {successCount} succeeded, {failedCount} failed";
                
                var errorDetails = errors.Count > 5 
                    ? string.Join("\n", errors.Take(5)) + $"\n... and {errors.Count - 5} more errors"
                    : string.Join("\n", errors);
                
                System.Windows.MessageBox.Show(
                    $"BOM Integration Partial Success\n\n" +
                    $"Successful: {successCount}\n" +
                    $"Failed: {failedCount}\n\n" +
                    $"Errors:\n{errorDetails}\n\n" +
                    $"Check the logs for detailed error information.",
                    "Integration Partial Success",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
            }
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Sage settings"))
        {
            StatusMessage = "Sage settings not configured";
            System.Windows.MessageBox.Show(
                "Sage 100 settings are not configured.\n\n" +
                "Please go to Settings and configure:\n" +
                "• Sage Home Directory\n" +
                "• Username\n" +
                "• Password\n" +
                "• Company Code",
                "Configuration Required",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error during BOM integration: {ex.Message}";
            System.Windows.MessageBox.Show(
                $"BOM integration failed:\n\n{ex.Message}\n\n" +
                $"Please check:\n" +
                $"• Sage 100 is installed and accessible\n" +
                $"• Sage settings are correct\n" +
                $"• You have permission to create BOMs\n" +
                $"• Parent items exist in Sage\n" +
                $"• All component items exist in Sage",
                "Integration Error",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }
}
