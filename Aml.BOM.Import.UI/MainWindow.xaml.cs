using Aml.BOM.Import.UI.ViewModels;

namespace Aml.BOM.Import.UI;

public partial class MainWindow : System.Windows.Window
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
