using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Aml.BOM.Import.UI.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly IServiceProvider _serviceProvider;

    [ObservableProperty]
    private object? _currentViewModel;

    [ObservableProperty]
    private string _currentViewTitle = "Welcome";

    [ObservableProperty]
    private string _currentViewDescription = "Select a section from the menu to get started";

    public MainWindowViewModel(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    [RelayCommand]
    private void Navigate(string viewName)
    {
        CurrentViewModel = viewName switch
        {
            "NewBuyItems" => GetViewModel<NewBuyItemsViewModel>("New Buy Items", "Manage items identified as new buy items"),
            "NewMakeItems" => GetViewModel<NewMakeItemsViewModel>("New Make Items", "Manage items identified as new make items"),
            "NewBoms" => GetViewModel<NewBomsViewModel>("New BOMs", "View and validate imported BOMs pending integration"),
            "IntegratedBoms" => GetViewModel<IntegratedBomsViewModel>("Integrated BOMs", "View BOMs that have been integrated into Sage"),
            "DuplicateBoms" => GetViewModel<DuplicateBomsViewModel>("Duplicate BOMs", "View BOMs identified as duplicates"),
            "Settings" => GetViewModel<SettingsViewModel>("Settings", "Configure application settings and connections"),
            _ => null
        };
    }

    private object GetViewModel<T>(string title, string description) where T : class
    {
        CurrentViewTitle = title;
        CurrentViewDescription = description;
        return _serviceProvider.GetService(typeof(T))!;
    }
}
