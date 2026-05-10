using System.Windows;
using System.Windows.Input;
using Aml.BOM.Import.Shared.Interfaces;

namespace Aml.BOM.Import.UI.Views;

public partial class ItemSearchWindow : Window
{
    private readonly ISageItemRepository _sageItemRepository;
    
    public object? SelectedItem { get; private set; }

    public ItemSearchWindow(ISageItemRepository sageItemRepository)
    {
        InitializeComponent();
        _sageItemRepository = sageItemRepository;
        
        // Focus on search box when window opens
        Loaded += (s, e) => SearchTextBox.Focus();
    }

    private async void SearchButton_Click(object sender, RoutedEventArgs e)
    {
        await PerformSearch();
    }

    private async void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            await PerformSearch();
        }
    }

    private async Task PerformSearch()
    {
        var searchTerm = SearchTextBox.Text?.Trim();
        
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            MessageBox.Show(
                "Please enter a search term",
                "Search",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        try
        {
            // Disable controls during search
            SearchTextBox.IsEnabled = false;
            ResultsDataGrid.ItemsSource = null;
            
            // Perform search
            var results = await _sageItemRepository.SearchItemsWithDetailsAsync(searchTerm);
            var resultsList = results.ToList();
            
            // Display results
            ResultsDataGrid.ItemsSource = resultsList;
            
            // Show message if no results
            if (!resultsList.Any())
            {
                MessageBox.Show(
                    $"No items found matching '{searchTerm}'",
                    "Search Results",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Error searching items: {ex.Message}",
                "Search Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            SearchTextBox.IsEnabled = true;
            SearchTextBox.Focus();
        }
    }

    private void SelectButton_Click(object sender, RoutedEventArgs e)
    {
        if (ResultsDataGrid.SelectedItem == null)
        {
            MessageBox.Show(
                "Please select an item from the list",
                "Select Item",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        SelectedItem = ResultsDataGrid.SelectedItem;
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void ResultsDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (ResultsDataGrid.SelectedItem != null)
        {
            SelectedItem = ResultsDataGrid.SelectedItem;
            DialogResult = true;
            Close();
        }
    }
}
