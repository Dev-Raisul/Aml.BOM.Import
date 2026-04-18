using Aml.BOM.Import.Application.Services;
using Aml.BOM.Import.Infrastructure.Repositories;
using Aml.BOM.Import.Infrastructure.Services;
using Aml.BOM.Import.Shared.Interfaces;
using Aml.BOM.Import.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Aml.BOM.Import.UI;

public partial class App : System.Windows.Application
{
    private readonly IHost _host;

    public App()
    {
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                // Register repositories
                services.AddSingleton<IBomImportRepository>(sp => 
                    new BomImportRepository(GetConnectionString()));
                services.AddSingleton<INewMakeItemRepository>(sp => 
                    new NewMakeItemRepository(GetConnectionString()));
                services.AddSingleton<INewBuyItemRepository>(sp => 
                    new NewBuyItemRepository(GetConnectionString()));
                services.AddSingleton<ISageItemRepository>(sp => 
                    new SageItemRepository(GetConnectionString()));

                // Register services
                services.AddSingleton<IFileImportService, FileImportService>();
                services.AddSingleton<IBomValidationService, BomValidationService>();
                services.AddSingleton<IBomIntegrationService, BomIntegrationService>();
                services.AddSingleton<ISettingsService, SettingsService>();

                // Register application services
                services.AddSingleton<BomImportService>();
                services.AddSingleton<NewItemService>();
                services.AddSingleton<IntegrationService>();

                // Register ViewModels
                services.AddSingleton<MainWindowViewModel>();
                services.AddTransient<NewBuyItemsViewModel>();
                services.AddTransient<NewMakeItemsViewModel>();
                services.AddTransient<NewBomsViewModel>();
                services.AddTransient<IntegratedBomsViewModel>();
                services.AddTransient<DuplicateBomsViewModel>();
                services.AddTransient<SettingsViewModel>();

                // Register MainWindow
                services.AddSingleton<MainWindow>();
            })
            .Build();
    }

    protected override async void OnStartup(System.Windows.StartupEventArgs e)
    {
        await _host.StartAsync();

        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();

        base.OnStartup(e);
    }

    protected override async void OnExit(System.Windows.ExitEventArgs e)
    {
        using (_host)
        {
            await _host.StopAsync();
        }

        base.OnExit(e);
    }

    private static string GetConnectionString()
    {
        // TODO: Load from settings
        return "Server=localhost;Database=AmlBomImport;Trusted_Connection=true;TrustServerCertificate=true;";
    }
}
