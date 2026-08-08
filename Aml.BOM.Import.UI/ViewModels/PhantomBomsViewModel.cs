using System.Collections.ObjectModel;
using Aml.BOM.Import.Application.Services;
using Aml.BOM.Import.Domain.Entities;
using Aml.BOM.Import.Shared.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Aml.BOM.Import.UI.ViewModels;

public partial class PhantomBomsViewModel : ObservableObject
{
    private readonly BomImportService _bomImportService;
    private readonly IBomImportBillRepository _bomBillRepository;
    private readonly ISageItemRepository _sageItemRepository;

    [ObservableProperty]
    private ObservableCollection<BomImportBill> phantomBoms = new();

    [ObservableProperty]
    private BomImportBill? selectedPhantom;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string loadingMessage = "Loading...";

    [ObservableProperty]
    private int totalPhantomBoms;

    [ObservableProperty]
    private int totalPhantomRecords;

    [ObservableProperty]
    private string statusMessage = "Ready";

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private DateTime lastUpdated;

    [ObservableProperty]
    private string notificationMessage = string.Empty;

    [ObservableProperty]
    private bool showNotification = false;

    private List<BomImportBill> _allPhantomBoms = new();

    public PhantomBomsViewModel(
        BomImportService bomImportService,
        IBomImportBillRepository bomBillRepository,
        ISageItemRepository sageItemRepository)
    {
        _bomImportService = bomImportService;
        _bomBillRepository = bomBillRepository;
        _sageItemRepository = sageItemRepository;
        LoadBomsCommand.Execute(null);
    }

    [RelayCommand]
    private async Task LoadBoms()
    {
        IsLoading = true;
        LoadingMessage = "Loading all BOMs...";
        StatusMessage = "Loading all BOMs...";

        try
        {
            // Load only BOMs with "Missing Phantom" status
            var phantomBills = (await _bomBillRepository.GetByStatusAsync("MissingPhantom")).ToList();

            _allPhantomBoms = phantomBills;

            LoadingMessage = "Filtering phantom BOMs...";
            StatusMessage = "Filtering phantom BOMs...";
            // Apply filter if search text exists
            ApplyFilter();

            LoadingMessage = "Calculating statistics...";
            StatusMessage = "Calculating statistics...";
            // Calculate statistics
            TotalPhantomRecords = _allPhantomBoms.Count;

            // Count unique parent items with missing phantom components
            TotalPhantomBoms = _allPhantomBoms
                .Select(b => b.ParentItemCode)
                .Distinct()
                .Count();

            LastUpdated = DateTime.Now;
            StatusMessage = $"Found {TotalPhantomBoms} BOMs with {TotalPhantomRecords} missing phantom items";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading phantom BOMs: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void Search()
    {
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        List<BomImportBill> filtered;

        if (string.IsNullOrWhiteSpace(SearchText))
        {
            filtered = _allPhantomBoms.ToList();
            StatusMessage = $"Showing all {_allPhantomBoms.Count} phantom items";
        }
        else
        {
            filtered = _allPhantomBoms
                .Where(b => 
                    (b.ParentItemCode?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (b.ComponentItemCode.Contains(SearchText, StringComparison.OrdinalIgnoreCase)) ||
                    (b.ComponentDescription?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false))
                .ToList();

            StatusMessage = $"Found {filtered.Count} matching phantom items";
        }

        // Group by ParentItemCode and sort
        var grouped = filtered
            .GroupBy(b => b.ParentItemCode)
            .OrderBy(g => g.Key)
            .SelectMany(g => g.OrderBy(b => b.LineNumber))
            .ToList();

        PhantomBoms = new ObservableCollection<BomImportBill>(grouped);
    }

    [RelayCommand]
    private void Export()
    {
        try
        {
            var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var fileName = $"PhantomBOMs_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            var filePath = System.IO.Path.Combine(desktopPath, fileName);

            using (var writer = new System.IO.StreamWriter(filePath))
            {
                // Write header
                writer.WriteLine("Parent Item,Component Item,Quantity,Component Description,Status,BOM Date,Import Date");

                // Write data
                foreach (var bom in _allPhantomBoms)
                {
                    var line = $"\"{bom.ParentItemCode}\",\"{bom.ComponentItemCode}\",{bom.Quantity},\"{bom.ComponentDescription}\",\"{bom.Status}\",\"{bom.ImportDate:d}\",\"{bom.ImportDate:d}\"";
                    writer.WriteLine(line);
                }
            }

            StatusMessage = $"Exported {_allPhantomBoms.Count} phantom items to {fileName}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error exporting phantom BOMs: {ex.Message}";
        }
    }

    [RelayCommand]
    private void CopyComponentItem()
    {
        if (SelectedPhantom == null)
        {
            StatusMessage = "No item selected to copy";
            return;
        }

        try
        {
            System.Windows.Forms.Clipboard.SetText(SelectedPhantom.ComponentItemCode);
            StatusMessage = $"Copied '{SelectedPhantom.ComponentItemCode}' to clipboard";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error copying to clipboard: {ex.Message}";
        }
    }

    [RelayCommand]
    private void CopyParentItem()
    {
        if (SelectedPhantom == null)
        {
            StatusMessage = "No item selected to copy";
            return;
        }

        try
        {
            if (!string.IsNullOrEmpty(SelectedPhantom.ParentItemCode))
            {
                System.Windows.Forms.Clipboard.SetText(SelectedPhantom.ParentItemCode);
                StatusMessage = $"Copied '{SelectedPhantom.ParentItemCode}' to clipboard";
            }
            else
            {
                StatusMessage = "Parent item code is empty";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error copying to clipboard: {ex.Message}";
        }
    }

    [RelayCommand]
    private void CopyAllToClipboard()
    {
        if (!PhantomBoms.Any())
        {
            StatusMessage = "No phantom items to copy";
            return;
        }

        try
        {
            var sb = new System.Text.StringBuilder();

            // Add header row
            sb.AppendLine("Parent Item\tComponent Item\tQuantity\tComponent Description\tStatus\tImport Date");

            // Add data rows (tab-separated for Excel compatibility)
            foreach (var bom in PhantomBoms)
            {
                sb.AppendLine($"{bom.ParentItemCode}\t{bom.ComponentItemCode}\t{bom.Quantity}\t{bom.ComponentDescription}\t{bom.Status}\t{bom.ImportDate:d}");
            }

            System.Windows.Forms.Clipboard.SetText(sb.ToString());

            // Show notification
            NotificationMessage = $"Copied to clipboard ({PhantomBoms.Count} items)";
            ShowNotification = true;

            // Auto-hide notification after 3 seconds
            Task.Delay(3000).ContinueWith(_ =>
            {
                ShowNotification = false;
            });

            StatusMessage = $"Copied {PhantomBoms.Count} phantom items to clipboard. Ready to paste into Excel!";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error copying to clipboard: {ex.Message}";
            NotificationMessage = $"Error: {ex.Message}";
            ShowNotification = true;

            Task.Delay(3000).ContinueWith(_ =>
            {
                ShowNotification = false;
            });
        }
    }
}
