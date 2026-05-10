using System.Collections.ObjectModel;
using Aml.BOM.Import.Application.Services;
using Aml.BOM.Import.Domain.Entities;
using Aml.BOM.Import.Shared.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Aml.BOM.Import.UI.ViewModels;

public partial class DuplicateBomsViewModel : ObservableObject
{
    private readonly BomImportService _bomImportService;
    private readonly IBomImportBillRepository _bomBillRepository;

    [ObservableProperty]
    private ObservableCollection<BomImportBill> _duplicateBoms = new();

    [ObservableProperty]
    private BomImportBill? _selectedBom;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private int _totalDuplicateBoms;

    [ObservableProperty]
    private int _uniqueParentItems;

    [ObservableProperty]
    private int _totalDuplicateRecords;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private string _searchText = string.Empty;

    private List<BomImportBill> _allDuplicateBoms = new();

    public DuplicateBomsViewModel(
        BomImportService bomImportService,
        IBomImportBillRepository bomBillRepository)
    {
        _bomImportService = bomImportService;
        _bomBillRepository = bomBillRepository;
        LoadBomsCommand.Execute(null);
    }

    [RelayCommand]
    private async Task LoadBoms()
    {
        IsLoading = true;
        StatusMessage = "Loading duplicate BOMs...";
        
        try
        {
            // Load only duplicate BOMs
            var duplicateBills = (await _bomBillRepository.GetByStatusAsync("Duplicate")).ToList();
            _allDuplicateBoms = duplicateBills;
            
            // Apply filter if search text exists
            ApplyFilter();

            // Calculate statistics
            TotalDuplicateRecords = _allDuplicateBoms.Count;
            TotalDuplicateBoms = _allDuplicateBoms
                .Select(b => b.ParentItemCode)
                .Distinct()
                .Count();
            UniqueParentItems = TotalDuplicateBoms;

            StatusMessage = $"Found {TotalDuplicateBoms} duplicate BOMs ({TotalDuplicateRecords} records)";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading duplicate BOMs: {ex.Message}";
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

    [RelayCommand]
    private void Search()
    {
        ApplyFilter();
    }

    [RelayCommand]
    private void ClearSearch()
    {
        SearchText = string.Empty;
        ApplyFilter();
    }

    [RelayCommand]
    private async Task DeleteSelected()
    {
        if (SelectedBom == null)
        {
            System.Windows.MessageBox.Show(
                "Please select a BOM to delete.",
                "No Selection",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
            return;
        }

        var result = System.Windows.MessageBox.Show(
            $"Are you sure you want to delete duplicate BOM '{SelectedBom.ParentItemCode}'?\n\n" +
            $"This will delete all {_allDuplicateBoms.Count(b => b.ParentItemCode == SelectedBom.ParentItemCode)} records associated with this BOM.",
            "Confirm Delete",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);

        if (result == System.Windows.MessageBoxResult.Yes)
        {
            IsLoading = true;
            StatusMessage = "Deleting duplicate BOM...";
            
            try
            {
                // Get all records with this parent item code
                var recordsToDelete = _allDuplicateBoms
                    .Where(b => b.ParentItemCode == SelectedBom.ParentItemCode)
                    .Select(b => b.Id)
                    .ToList();

                // Delete each record
                foreach (var id in recordsToDelete)
                {
                    await _bomBillRepository.DeleteAsync(id);
                }

                StatusMessage = $"Deleted {recordsToDelete.Count} records";
                
                System.Windows.MessageBox.Show(
                    $"Successfully deleted {recordsToDelete.Count} duplicate records.",
                    "Delete Complete",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);

                await LoadBoms();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error deleting: {ex.Message}";
                System.Windows.MessageBox.Show(
                    $"Error deleting duplicate BOM: {ex.Message}",
                    "Delete Error",
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
    private async Task DeleteAll()
    {
        if (!_allDuplicateBoms.Any())
        {
            System.Windows.MessageBox.Show(
                "No duplicate BOMs to delete.",
                "No Data",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
            return;
        }

        var result = System.Windows.MessageBox.Show(
            $"Are you sure you want to delete ALL {TotalDuplicateBoms} duplicate BOMs?\n\n" +
            $"This will delete {TotalDuplicateRecords} records from the database.\n\n" +
            $"This action cannot be undone!",
            "Confirm Delete All",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);

        if (result == System.Windows.MessageBoxResult.Yes)
        {
            IsLoading = true;
            StatusMessage = "Deleting all duplicate BOMs...";
            
            try
            {
                int deletedCount = 0;
                
                // Delete all duplicate records
                foreach (var bom in _allDuplicateBoms)
                {
                    await _bomBillRepository.DeleteAsync(bom.Id);
                    deletedCount++;
                }

                StatusMessage = $"Deleted {deletedCount} duplicate records";
                
                System.Windows.MessageBox.Show(
                    $"Successfully deleted {deletedCount} duplicate records.",
                    "Delete Complete",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);

                await LoadBoms();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error deleting: {ex.Message}";
                System.Windows.MessageBox.Show(
                    $"Error deleting duplicate BOMs: {ex.Message}",
                    "Delete Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }
    }

    private void ApplyFilter()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            DuplicateBoms = new ObservableCollection<BomImportBill>(_allDuplicateBoms);
        }
        else
        {
            var filtered = _allDuplicateBoms.Where(b =>
                (b.ParentItemCode?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (b.ParentDescription?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (b.ComponentItemCode?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (b.ComponentDescription?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (b.ImportFileName?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (b.BOMNumber?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false)
            ).ToList();

            DuplicateBoms = new ObservableCollection<BomImportBill>(filtered);
        }
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilter();
    }
}
