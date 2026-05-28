using Aml.BOM.Import.UI.ViewModels;
using Aml.BOM.Import.UI.Views;
using System.Windows;

namespace Aml.BOM.Import.UI;

public partial class MainWindow : System.Windows.Window
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.NavigateCommand.Execute("NewBoms");
    }

    private void AboutButton_Click(object sender, RoutedEventArgs e)
    {
        var aboutWindow = new AboutWindow
        {
            Owner = this
        };
        aboutWindow.ShowDialog();
    }
}
