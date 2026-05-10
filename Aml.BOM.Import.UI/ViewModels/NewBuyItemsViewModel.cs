using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
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

    [ObservableProperty]
    private int _totalItems;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    public NewBuyItemsViewModel(NewItemService newItemService)
    {
        _newItemService = newItemService;
        LoadItemsCommand.Execute(null);
    }

    [RelayCommand]
    private async Task LoadItems()
    {
        IsLoading = true;
        StatusMessage = "Loading new buy items...";
        
        try
        {
            var items = await _newItemService.GetNewBuyItemsAsync();
            
            Items = new ObservableCollection<object>(items);
            TotalItems = Items.Count;
            StatusMessage = $"Loaded {TotalItems} new buy item(s)";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading items: {ex.Message}";
            MessageBox.Show($"Failed to load new buy items: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
    private void Print()
    {
        if (!Items.Any())
        {
            MessageBox.Show("No items to print.", "Print", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            var printDialog = new PrintDialog();
            if (printDialog.ShowDialog() == true)
            {
                var document = CreatePrintDocument();
                printDialog.PrintDocument(((IDocumentPaginatorSource)document).DocumentPaginator, "New Buy Items Report");
                MessageBox.Show("Document sent to printer successfully.", "Print", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to print: {ex.Message}", "Print Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private FlowDocument CreatePrintDocument()
    {
        var document = new FlowDocument
        {
            PagePadding = new Thickness(50),
            FontFamily = new System.Windows.Media.FontFamily("Arial"),
            FontSize = 12
        };

        // Title
        var title = new Paragraph(new Run("New Buy Items Report"))
        {
            FontSize = 18,
            FontWeight = FontWeights.Bold,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 20)
        };
        document.Blocks.Add(title);

        // Date
        var date = new Paragraph(new Run($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}"))
        {
            FontSize = 10,
            TextAlignment = TextAlignment.Right,
            Margin = new Thickness(0, 0, 0, 10)
        };
        document.Blocks.Add(date);

        // Summary
        var summary = new Paragraph(new Run($"Total Items: {TotalItems}"))
        {
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 0, 0, 20)
        };
        document.Blocks.Add(summary);

        // Table
        var table = new Table
        {
            CellSpacing = 0,
            BorderBrush = System.Windows.Media.Brushes.Black,
            BorderThickness = new Thickness(1)
        };

        // Define columns
        table.Columns.Add(new TableColumn { Width = new GridLength(100) }); // Item Code
        table.Columns.Add(new TableColumn { Width = new GridLength(200) }); // Description
        table.Columns.Add(new TableColumn { Width = new GridLength(60) });  // UOM
        table.Columns.Add(new TableColumn { Width = new GridLength(80) });  // Identified Date
        table.Columns.Add(new TableColumn { Width = new GridLength(80) });  // Count

        // Header
        var headerGroup = new TableRowGroup();
        var headerRow = new TableRow
        {
            Background = System.Windows.Media.Brushes.LightGray,
            FontWeight = FontWeights.Bold
        };

        headerRow.Cells.Add(new TableCell(new Paragraph(new Run("Item Code"))) { BorderBrush = System.Windows.Media.Brushes.Black, BorderThickness = new Thickness(1), Padding = new Thickness(5) });
        headerRow.Cells.Add(new TableCell(new Paragraph(new Run("Description"))) { BorderBrush = System.Windows.Media.Brushes.Black, BorderThickness = new Thickness(1), Padding = new Thickness(5) });
        headerRow.Cells.Add(new TableCell(new Paragraph(new Run("UOM"))) { BorderBrush = System.Windows.Media.Brushes.Black, BorderThickness = new Thickness(1), Padding = new Thickness(5) });
        headerRow.Cells.Add(new TableCell(new Paragraph(new Run("Identified"))) { BorderBrush = System.Windows.Media.Brushes.Black, BorderThickness = new Thickness(1), Padding = new Thickness(5) });
        headerRow.Cells.Add(new TableCell(new Paragraph(new Run("Count"))) { BorderBrush = System.Windows.Media.Brushes.Black, BorderThickness = new Thickness(1), Padding = new Thickness(5) });

        headerGroup.Rows.Add(headerRow);
        table.RowGroups.Add(headerGroup);

        // Data rows
        var dataGroup = new TableRowGroup();
        foreach (var item in Items)
        {
            dynamic dyn = item;
            var row = new TableRow();
            
            row.Cells.Add(new TableCell(new Paragraph(new Run(dyn.ItemCode?.ToString() ?? ""))) { BorderBrush = System.Windows.Media.Brushes.Black, BorderThickness = new Thickness(1), Padding = new Thickness(5) });
            row.Cells.Add(new TableCell(new Paragraph(new Run(dyn.Description?.ToString() ?? ""))) { BorderBrush = System.Windows.Media.Brushes.Black, BorderThickness = new Thickness(1), Padding = new Thickness(5) });
            row.Cells.Add(new TableCell(new Paragraph(new Run(dyn.UnitOfMeasure?.ToString() ?? ""))) { BorderBrush = System.Windows.Media.Brushes.Black, BorderThickness = new Thickness(1), Padding = new Thickness(5) });
            row.Cells.Add(new TableCell(new Paragraph(new Run(((DateTime)dyn.IdentifiedDate).ToString("yyyy-MM-dd")))) { BorderBrush = System.Windows.Media.Brushes.Black, BorderThickness = new Thickness(1), Padding = new Thickness(5) });
            row.Cells.Add(new TableCell(new Paragraph(new Run(dyn.OccurrenceCount?.ToString() ?? ""))) { BorderBrush = System.Windows.Media.Brushes.Black, BorderThickness = new Thickness(1), Padding = new Thickness(5) });

            dataGroup.Rows.Add(row);
        }
        table.RowGroups.Add(dataGroup);

        document.Blocks.Add(table);

        // Footer
        var footer = new Paragraph(new Run($"\nEnd of Report - {TotalItems} item(s)"))
        {
            FontSize = 10,
            FontStyle = FontStyles.Italic,
            Margin = new Thickness(0, 20, 0, 0)
        };
        document.Blocks.Add(footer);

        return document;
    }
}
