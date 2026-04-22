using System.Windows.Controls;
using Aml.BOM.Import.UI.ViewModels;

namespace Aml.BOM.Import.UI.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is SettingsViewModel viewModel)
        {
            DatabasePasswordBox.Password = viewModel.DatabasePassword;
            SagePasswordBox.Password = viewModel.SagePassword;

            DatabasePasswordBox.PasswordChanged += (s, args) =>
            {
                viewModel.DatabasePassword = DatabasePasswordBox.Password;
            };

            SagePasswordBox.PasswordChanged += (s, args) =>
            {
                viewModel.SagePassword = SagePasswordBox.Password;
            };
        }
    }
}
