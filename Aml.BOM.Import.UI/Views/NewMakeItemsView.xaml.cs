using System.Windows;
using System.Windows.Controls;
using Aml.BOM.Import.Domain.Entities;
using Aml.BOM.Import.UI.ViewModels;

namespace Aml.BOM.Import.UI.Views;

public partial class NewMakeItemsView : UserControl
{
    private string? _lastEditedColumn;
    private Dictionary<string, bool> _columnPromptStatus = new();

    public NewMakeItemsView()
    {
        InitializeComponent();
    }

    private void DataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
    {
        if (e.EditAction == DataGridEditAction.Commit)
        {
            var columnHeader = e.Column.Header?.ToString();
            if (string.IsNullOrEmpty(columnHeader)) return;

            // Skip system columns
            if (IsSystemColumn(columnHeader)) return;

            // Skip if this column has been disabled for prompting
            if (_columnPromptStatus.ContainsKey(columnHeader) && !_columnPromptStatus[columnHeader])
                return;

            var editingElement = e.EditingElement;
            object? newValue = null;

            // Get the new value based on element type
            if (editingElement is TextBox textBox)
            {
                newValue = textBox.Text;
            }
            else if (editingElement is CheckBox checkBox)
            {
                newValue = checkBox.IsChecked ?? false;
            }

            // Get the ViewModel
            if (DataContext is NewMakeItemsViewModel viewModel && viewModel.Items.Count > 1)
            {
                // Schedule the prompt after the edit completes
                Dispatcher.BeginInvoke(new Action(async () =>
                {
                    var result = MessageBox.Show(
                        $"Do you want to copy this value to all currently filtered items?",
                        "Copy Value to All",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        // Copy to all filtered items
                        await viewModel.CopyValueToAllFilteredItemsAsync(columnHeader, newValue);
                    }
                    else
                    {
                        // Disable prompt for this column
                        _columnPromptStatus[columnHeader] = false;
                        MessageBox.Show(
                            $"Future edits to '{columnHeader}' will not prompt to copy.\n\n" +
                            $"To re-enable, right-click on the cell and select 'Enable Prompt'.",
                            "Prompt Disabled",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                }), System.Windows.Threading.DispatcherPriority.Background);
            }
        }
    }

    private void DataGrid_MouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        // Find the clicked cell
        var dep = (DependencyObject)e.OriginalSource;
        while (dep != null && dep is not DataGridCell)
        {
            dep = System.Windows.Media.VisualTreeHelper.GetParent(dep);
        }

        if (dep is DataGridCell cell)
        {
            // Select the row
            if (cell.DataContext is NewMakeItem item)
            {
                var dataGrid = (DataGrid)sender;
                dataGrid.SelectedItem = item;
                
                // Store the column for context menu operations
                if (cell.Column?.Header != null)
                {
                    _lastEditedColumn = cell.Column.Header.ToString();
                    
                    // Build and show context menu
                    ShowContextMenu(cell, _lastEditedColumn);
                }
            }
        }
    }

    private void ShowContextMenu(DataGridCell cell, string columnHeader)
    {
        // Skip system columns
        if (IsSystemColumn(columnHeader)) return;

        var contextMenu = new ContextMenu();
        var viewModel = DataContext as NewMakeItemsViewModel;

        // Copy to all filtered items (blank only)
        var copyToBlankMenuItem = new MenuItem
        {
            Header = "Copy to all filtered items (blank only)",
            Tag = columnHeader
        };
        copyToBlankMenuItem.Click += async (s, e) =>
        {
            if (viewModel?.SelectedItem != null)
            {
                var value = GetPropertyValue(viewModel.SelectedItem, columnHeader);
                await viewModel.CopyToAllFilteredBlankCommand.ExecuteAsync(columnHeader);
            }
        };
        contextMenu.Items.Add(copyToBlankMenuItem);

        // Copy to all filtered items (overwrite)
        var copyToAllMenuItem = new MenuItem
        {
            Header = "Copy to all filtered items",
            Tag = columnHeader
        };
        copyToAllMenuItem.Click += async (s, e) =>
        {
            if (viewModel != null)
            {
                await viewModel.CopyToAllFilteredCommand.ExecuteAsync(columnHeader);
            }
        };
        contextMenu.Items.Add(copyToAllMenuItem);

        // Separator
        contextMenu.Items.Add(new Separator());

        // Clear for all filtered items
        var clearMenuItem = new MenuItem
        {
            Header = "Clear for all filtered items",
            Tag = columnHeader
        };
        clearMenuItem.Click += async (s, e) =>
        {
            if (viewModel != null)
            {
                var result = MessageBox.Show(
                    $"Are you sure you want to clear '{columnHeader}' for all {viewModel.Items.Count} filtered items?",
                    "Clear Column",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    await viewModel.ClearForAllFilteredCommand.ExecuteAsync(columnHeader);
                }
            }
        };
        contextMenu.Items.Add(clearMenuItem);

        // Separator
        contextMenu.Items.Add(new Separator());

        // Enable/Disable Prompt
        var isPromptEnabled = !_columnPromptStatus.ContainsKey(columnHeader) || _columnPromptStatus[columnHeader];
        var promptMenuItem = new MenuItem
        {
            Header = isPromptEnabled ? "? Prompt Enabled" : "Enable Prompt",
            Tag = columnHeader
        };
        promptMenuItem.Click += (s, e) =>
        {
            _columnPromptStatus[columnHeader] = true;
            MessageBox.Show(
                $"Prompt re-enabled for '{columnHeader}'.\n\nFuture edits will ask if you want to copy the value to all filtered items.",
                "Prompt Enabled",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        };
        contextMenu.Items.Add(promptMenuItem);

        // Show the context menu
        cell.ContextMenu = contextMenu;
        contextMenu.IsOpen = true;
    }

    private bool IsSystemColumn(string columnHeader)
    {
        return columnHeader switch
        {
            "Import File Name" => true,
            "Import Date" => true,
            "Item Code" => true,
            "Status" => true,
            _ => false
        };
    }

    private object? GetPropertyValue(NewMakeItem item, string columnHeader)
    {
        var propertyName = MapColumnHeaderToPropertyName(columnHeader);
        if (string.IsNullOrEmpty(propertyName)) return null;

        return item.GetType().GetProperty(propertyName)?.GetValue(item);
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
}
