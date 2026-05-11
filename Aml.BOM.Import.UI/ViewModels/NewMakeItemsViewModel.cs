using Aml.BOM.Import.Application.Services;
using Aml.BOM.Import.Domain.Entities;
using Aml.BOM.Import.Shared.Interfaces;
using Aml.BOM.Import.UI.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows;

namespace Aml.BOM.Import.UI.ViewModels;

public partial class NewMakeItemsViewModel : ObservableObject
{
    private readonly NewItemService _newItemService;
    private readonly INewMakeItemRepository _makeItemRepository;
    private readonly ISageItemRepository _sageItemRepository;
    private readonly IBomIntegrationService _bomIntegrationService;

    private List<NewMakeItem> _allItems = new();
    private string? _lastEditedColumn;
    private bool _promptForBulkCopy = true;

    [ObservableProperty]
    private ObservableCollection<NewMakeItem> _items = new();

    [ObservableProperty]
    private NewMakeItem? _selectedItem;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    // Filter properties
    [ObservableProperty]
    private string _filterImportFileName = string.Empty;

    [ObservableProperty]
    private DateTime? _filterImportDateFrom;

    [ObservableProperty]
    private DateTime? _filterImportDateTo;

    [ObservableProperty]
    private string _filterItemCode = string.Empty;

    [ObservableProperty]
    private bool _filterEditedOnly;

    [ObservableProperty]
    private bool _filterMissingDataOnly;

    [ObservableProperty]
    private bool _showIntegratedItems = false; // Don't show integrated by default

    // Statistics
    [ObservableProperty]
    private int _totalItems;

    [ObservableProperty]
    private int _editedItems;

    [ObservableProperty]
    private int _readyForIntegration;

    [ObservableProperty]
    private int _missingDataItems;

    public NewMakeItemsViewModel(
        NewItemService newItemService,
        INewMakeItemRepository makeItemRepository,
        ISageItemRepository sageItemRepository,
        IBomIntegrationService bomIntegrationService)
    {
        _newItemService = newItemService;
        _makeItemRepository = makeItemRepository;
        _sageItemRepository = sageItemRepository;
        _bomIntegrationService = bomIntegrationService;
        
        // Load all new make items on startup
        LoadItemsCommand.Execute(null);
    }

    [RelayCommand]
    private async Task LoadItems()
    {
        IsLoading = true;
        StatusMessage = "Loading make items...";
        
        try
        {
            // Load all new make items from service
            var items = await _newItemService.GetNewMakeItemsAsync();
            _allItems = items.Cast<NewMakeItem>().ToList();
            
            // By default, show only non-integrated items (all new make items)
            // Apply filters will respect the ShowIntegratedItems checkbox
            ApplyFilters();
            UpdateStatistics();
            
            StatusMessage = $"Loaded {TotalItems} new make items";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading items: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void ApplyFilters()
    {
        var filtered = _allItems.AsEnumerable();

        // Filter by import file name
        if (!string.IsNullOrWhiteSpace(FilterImportFileName))
        {
            filtered = filtered.Where(i => 
                i.ImportFileName.Contains(FilterImportFileName, StringComparison.OrdinalIgnoreCase));
        }

        // Filter by import date range
        if (FilterImportDateFrom.HasValue)
        {
            filtered = filtered.Where(i => i.ImportFileDate.Date >= FilterImportDateFrom.Value.Date);
        }

        if (FilterImportDateTo.HasValue)
        {
            filtered = filtered.Where(i => i.ImportFileDate.Date <= FilterImportDateTo.Value.Date);
        }

        // Filter by item code with wildcards
        if (!string.IsNullOrWhiteSpace(FilterItemCode))
        {
            var pattern = ConvertWildcardToRegex(FilterItemCode);
            filtered = filtered.Where(i => 
                System.Text.RegularExpressions.Regex.IsMatch(i.ItemCode, pattern, 
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase));
        }

        // Filter by edited status
        if (FilterEditedOnly)
        {
            filtered = filtered.Where(i => i.IsEdited);
        }

        // Filter by missing data
        if (FilterMissingDataOnly)
        {
            filtered = filtered.Where(i => string.IsNullOrWhiteSpace(i.ProductLine));
        }

        // Filter integrated items
        if (!ShowIntegratedItems)
        {
            filtered = filtered.Where(i => !i.IsIntegrated);
        }

        Items = new ObservableCollection<NewMakeItem>(filtered.ToList());
        UpdateStatistics();
    }

    private string ConvertWildcardToRegex(string pattern)
    {
        // Build regex pattern character by character
        // % = zero or more characters (.*)
        // ? = exactly one character (.)
        // All other characters are escaped for regex
        var regexPattern = new System.Text.StringBuilder();
        
        foreach (char c in pattern)
        {
            switch (c)
            {
                case '%':
                    regexPattern.Append(".*");
                    break;
                case '?':
                    regexPattern.Append(".");
                    break;
                case '.':
                case '\\':
                case '+':
                case '*':
                case '[':
                case ']':
                case '(':
                case ')':
                case '{':
                case '}':
                case '^':
                case '$':
                case '|':
                    // Escape regex special characters
                    regexPattern.Append('\\').Append(c);
                    break;
                default:
                    // Regular character
                    regexPattern.Append(c);
                    break;
            }
        }
        
        return "^" + regexPattern.ToString() + "$";
    }

    [RelayCommand]
    private void ClearFilters()
    {
        FilterImportFileName = string.Empty;
        FilterImportDateFrom = null;
        FilterImportDateTo = null;
        FilterItemCode = string.Empty;
        FilterEditedOnly = false;
        FilterMissingDataOnly = false;
        
        ApplyFilters();
    }

    [RelayCommand]
    private async Task CellValueChanged(object parameter)
    {
        if (parameter is not (string columnName, NewMakeItem item, object newValue))
            return;

        if (!_promptForBulkCopy)
            return;

        var result = MessageBox.Show(
            $"Do you want to copy this value to all currently filtered items?",
            "Copy Value",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            await CopyValueToFilteredItems(columnName, newValue, false);
        }
        else
        {
            // Disable bulk copy prompt for this column until user right-clicks
            _lastEditedColumn = columnName;
            _promptForBulkCopy = false;
        }
    }

    [RelayCommand]
    private async Task CopyToAllFilteredBlank(string columnName)
    {
        if (SelectedItem == null) return;
        
        var value = GetPropertyValue(SelectedItem, columnName);
        await CopyValueToFilteredItems(columnName, value, true);
    }

    [RelayCommand]
    private async Task CopyToAllFiltered(string columnName)
    {
        if (SelectedItem == null) return;
        
        var value = GetPropertyValue(SelectedItem, columnName);
        await CopyValueToFilteredItems(columnName, value, false);
    }

    [RelayCommand]
    private async Task ClearForAllFiltered(string columnName)
    {
        await CopyValueToFilteredItems(columnName, GetDefaultValue(columnName), false);
    }

    private async Task CopyValueToFilteredItems(string columnName, object? value, bool onlyBlank)
    {
        IsLoading = true;
        StatusMessage = $"Copying value to filtered items...";
        
        try
        {
            int updatedCount = 0;
            
            foreach (var item in Items)
            {
                if (onlyBlank)
                {
                    var currentValue = GetPropertyValue(item, columnName);
                    if (!IsBlank(currentValue))
                        continue;
                }

                SetPropertyValue(item, columnName, value);
                updatedCount++;
            }

            await SaveChanges();
            
            StatusMessage = $"Updated {updatedCount} items";
            _promptForBulkCopy = true; // Re-enable bulk copy prompt
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error copying value: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task CopyFromItem()
    {
        var searchWindow = new ItemSearchWindow(_sageItemRepository)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        
        if (searchWindow.ShowDialog() == true && searchWindow.SelectedItem != null)
        {
            await CopyFromSageItemAsync(searchWindow.SelectedItem);
        }
    }

    private async Task CopyFromSageItemAsync(dynamic sageItem)
    {
        IsLoading = true;
        StatusMessage = "Copying data from Sage item...";
        
        try
        {
            int updatedCount = 0;
            
            // Copy all fields except description from Sage item to filtered items
            foreach (var item in Items)
            {
                // Copy properties (excluding description as per spec)
                item.ProductLine = sageItem.ProductLine ?? string.Empty;
                item.ProductType = sageItem.ProductType ?? "F";
                item.Procurement = sageItem.Procurement ?? "M";
                item.StandardUnitOfMeasure = sageItem.StandardUnitOfMeasure ?? "EACH";
                item.SubProductFamily = sageItem.SubProductFamily ?? string.Empty;
                item.StagedItem = sageItem.StagedItem;
                item.Coated = sageItem.Coated;
                item.GoldenStandard = sageItem.GoldenStandard;
                
                updatedCount++;
            }

            await SaveChanges();
            UpdateStatistics();
            
            StatusMessage = $"Copied data from '{sageItem.ItemCode}' to {updatedCount} filtered items";
            
            MessageBox.Show(
                $"Successfully copied properties from '{sageItem.ItemCode}' to {updatedCount} filtered items.\n\n" +
                $"Note: Item descriptions were not changed.",
                "Copy Complete",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error copying from item: {ex.Message}";
            MessageBox.Show(
                $"Error copying from item: {ex.Message}",
                "Copy Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ClearAll()
    {
        var result = MessageBox.Show(
            $"Are you sure you want to clear all edited data for {Items.Count} filtered items?",
            "Clear All",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            IsLoading = true;
            StatusMessage = "Clearing all data...";
            
            try
            {
                foreach (var item in Items)
                {
                    ResetItemToDefaults(item);
                }

                await SaveChanges();
                StatusMessage = $"Cleared data for {Items.Count} items";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error clearing data: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }
    }

    [RelayCommand]
    private async Task IntegrateItems()
    {
        var itemsToIntegrate = Items.Where(i => 
            !i.IsIntegrated && 
            !string.IsNullOrWhiteSpace(i.ProductLine)).ToList();

        if (!itemsToIntegrate.Any())
        {
            MessageBox.Show(
                "No items are ready for integration. Please ensure Product Line is set for items.",
                "Integration",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        var result = MessageBox.Show(
            $"Ready to integrate {itemsToIntegrate.Count} make items into Sage 100.\n\n" +
            $"This will create the items in Sage using COM integration. Continue?",
            "Integrate Make Items",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            IsLoading = true;
            StatusMessage = "Integrating items into Sage 100...";
            
            try
            {
                // Pass the actual items with their current edited values (not IDs)
                // This ensures we integrate the in-memory edited data, not database values
                bool success = await _bomIntegrationService.IntegrateNewItemsAsync(itemsToIntegrate);
                
                // Reload items to get updated integration status
                await LoadItems();
                
                // Count results from reloaded data
                int successCount = itemsToIntegrate.Count(i => i.IsIntegrated);
                int failedCount = itemsToIntegrate.Count - successCount;
                
                if (success && failedCount == 0)
                {
                    StatusMessage = $"Successfully integrated {successCount} items into Sage 100";
                    MessageBox.Show(
                        $"Integration successful!\n\n" +
                        $"Created {successCount} make items in Sage 100.",
                        "Integration Complete",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else
                {
                    StatusMessage = $"Integration completed with errors: {successCount} succeeded, {failedCount} failed";
                    MessageBox.Show(
                        $"Integration completed with some errors:\n\n" +
                        $"Successful: {successCount}\n" +
                        $"Failed: {failedCount}\n\n" +
                        $"Check the logs for details.",
                        "Integration Partial Success",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("Sage settings"))
            {
                StatusMessage = "Sage settings not configured";
                MessageBox.Show(
                    "Sage 100 settings are not configured.\n\n" +
                    "Please go to Settings and configure:\n" +
                    "• Sage Home Directory\n" +
                    "• Username\n" +
                    "• Password\n" +
                    "• Company Code",
                    "Configuration Required",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error during integration: {ex.Message}";
                MessageBox.Show(
                    $"Integration failed:\n\n{ex.Message}\n\n" +
                    $"Please check:\n" +
                    $"• Sage 100 is installed and accessible\n" +
                    $"• Sage settings are correct\n" +
                    $"• You have permission to create items\n" +
                    $"• All required fields are populated",
                    "Integration Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }
    }

    [RelayCommand]
    private async Task Refresh()
    {
        await LoadItems();
    }

    public async Task CopyValueToAllFilteredItemsAsync(string columnHeader, object? value)
    {
        IsLoading = true;
        StatusMessage = $"Copying '{columnHeader}' value to all filtered items...";
        
        try
        {
            int updatedCount = 0;
            
            // Map column header to property name
            var propertyName = MapColumnHeaderToPropertyName(columnHeader);
            if (string.IsNullOrEmpty(propertyName))
            {
                StatusMessage = $"Unknown column: {columnHeader}";
                return;
            }

            foreach (var item in Items)
            {
                SetPropertyValue(item, columnHeader, value);
                updatedCount++;
            }

            await SaveChanges();
            UpdateStatistics();
            
            StatusMessage = $"Updated {updatedCount} items with new '{columnHeader}' value";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error copying value: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private string? MapColumnHeaderToPropertyName(string columnHeader)
    {
        return columnHeader switch
        {
            "Item Description" => nameof(NewMakeItem.ItemDescription),
            "Product Line" => nameof(NewMakeItem.ProductLine),
            "Product Type" => nameof(NewMakeItem.ProductType),
            "Procurement" => nameof(NewMakeItem.Procurement),
            "Standard UOM" => nameof(NewMakeItem.StandardUnitOfMeasure),
            "Sub Product Family" => nameof(NewMakeItem.SubProductFamily),
            "Staged" => nameof(NewMakeItem.StagedItem),
            "Coated" => nameof(NewMakeItem.Coated),
            "Golden Std" => nameof(NewMakeItem.GoldenStandard),
            _ => null
        };
    }

    private void UpdateStatistics()
    {
        TotalItems = Items.Count;
        EditedItems = Items.Count(i => i.IsEdited);
        ReadyForIntegration = Items.Count(i => !i.IsIntegrated && !string.IsNullOrWhiteSpace(i.ProductLine));
        MissingDataItems = Items.Count(i => string.IsNullOrWhiteSpace(i.ProductLine));
    }

    private async Task SaveChanges()
    {
        // Save all modified items to database
        foreach (var item in Items.Where(i => i.IsEdited))
        {
            await _makeItemRepository.UpdateAsync(item);
        }
    }

    private void ResetItemToDefaults(NewMakeItem item)
    {
        item.ItemDescription = string.Empty;
        item.ProductLine = string.Empty;
        item.ProductType = "F";
        item.Procurement = "M";
        item.StandardUnitOfMeasure = "EACH";
        item.SubProductFamily = string.Empty;
        item.StagedItem = false;
        item.Coated = false;
        item.GoldenStandard = false;
        item.IsEdited = false;
    }

    private object? GetPropertyValue(NewMakeItem item, string columnName)
    {
        var propertyName = MapColumnHeaderToPropertyName(columnName);
        return item.GetType().GetProperty(propertyName)?.GetValue(item);
    }

    private void SetPropertyValue(NewMakeItem item, string columnName, object? value)
    {
        var propertyName = MapColumnHeaderToPropertyName(columnName);
        item.GetType().GetProperty(propertyName)?.SetValue(item, value);
    }

    private object? GetDefaultValue(string columnName)
    {
        var propertyName = MapColumnHeaderToPropertyName(columnName);
        return propertyName switch
        {
            nameof(NewMakeItem.ProductType) => "F",
            nameof(NewMakeItem.Procurement) => "M",
            nameof(NewMakeItem.StandardUnitOfMeasure) => "EACH",
            nameof(NewMakeItem.StagedItem) => false,
            nameof(NewMakeItem.Coated) => false,
            nameof(NewMakeItem.GoldenStandard) => false,
            _ => string.Empty
        };
    }

    private bool IsBlank(object? value)
    {
        if (value == null) return true;
        
        return value switch
        {
            // String type: check null or whitespace
            string str => string.IsNullOrWhiteSpace(str),
            
            // Boolean type: check if false
            bool b => !b,
            
            // Numeric types: check for 0
            int i => i == 0,
            long l => l == 0,
            short s => s == 0,
            byte by => by == 0,
            decimal d => d == 0,
            double db => db == 0,
            float f => f == 0,
            
            // Default: not empty
            _ => false
        };
    }
}
