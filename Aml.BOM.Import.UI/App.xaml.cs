using Aml.BOM.Import.Application.Services;
using Aml.BOM.Import.Infrastructure.Repositories;
using Aml.BOM.Import.Infrastructure.Services;
using Aml.BOM.Import.Shared.Interfaces;
using Aml.BOM.Import.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.IO;

namespace Aml.BOM.Import.UI;

public partial class App : System.Windows.Application
{
    private readonly IHost _host;

    public App()
    {
        // Setup global exception handlers
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                // Register logger (must be first)
                services.AddSingleton<ILoggerService, FileLoggerService>();
                
                // Log application startup
                var logger = new FileLoggerService();
                logger.LogInformation("=== Application Starting ===");
                logger.LogInformation("Application Version: {0}", System.Reflection.Assembly.GetExecutingAssembly().GetName().Version);
                logger.LogInformation("Operating System: {0}", Environment.OSVersion);
                logger.LogInformation(".NET Version: {0}", Environment.Version);

                // Register repositories
                services.AddSingleton<IBomImportRepository>(sp => 
                    new BomImportRepository(GetConnectionString()));
                services.AddSingleton<INewMakeItemRepository>(sp => 
                    new NewMakeItemRepository(GetConnectionString()));
                services.AddSingleton<INewBuyItemRepository>(sp => 
                    new NewBuyItemRepository(GetConnectionString()));
                services.AddSingleton<ISageItemRepository>(sp => 
                    new SageItemRepository(GetConnectionString()));
                services.AddSingleton<IImportBomFileLogRepository>(sp => 
                    new ImportBomFileLogRepository(GetConnectionString(), sp.GetRequiredService<ILoggerService>()));
                services.AddSingleton<IBomImportBillRepository>(sp => 
                    new BomImportBillRepository(GetConnectionString(), sp.GetRequiredService<ILoggerService>()));

                // Register services
                services.AddSingleton<IDatabaseConnectionService, DatabaseConnectionService>();
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

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var logger = new FileLoggerService();
        logger.LogCritical("Unhandled exception occurred", e.ExceptionObject as Exception);
        logger.LogCritical("IsTerminating: {0}", null, e.IsTerminating);
    }

    private void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        var logger = new FileLoggerService();
        logger.LogCritical("Dispatcher unhandled exception occurred", e.Exception);
        
        System.Windows.MessageBox.Show(
            $"An unexpected error occurred:\n\n{e.Exception.Message}\n\nThe error has been logged.",
            "Error",
            System.Windows.MessageBoxButton.OK,
            System.Windows.MessageBoxImage.Error);
        
        e.Handled = true;
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        var logger = new FileLoggerService();
        logger.LogError("Unobserved task exception occurred", e.Exception);
        e.SetObserved();
    }

    protected override async void OnStartup(System.Windows.StartupEventArgs e)
    {
        await _host.StartAsync();

        var logger = _host.Services.GetRequiredService<ILoggerService>();
        logger.LogInformation("Application host started successfully");

        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();
        
        logger.LogInformation("Main window displayed");

        base.OnStartup(e);
    }

    protected override async void OnExit(System.Windows.ExitEventArgs e)
    {
        var logger = _host.Services.GetRequiredService<ILoggerService>();
        logger.LogInformation("=== Application Shutting Down ===");
        logger.LogInformation("Exit Code: {0}", e.ApplicationExitCode);
        
        using (_host)
        {
            await _host.StopAsync();
        }

        base.OnExit(e);
    }

    private static string GetConnectionString()
    {
        try
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appFolder = Path.Combine(appDataPath, "Aml.BOM.Import");
            var settingsFilePath = Path.Combine(appFolder, "appsettings.json");
            
            if (File.Exists(settingsFilePath))
            {
                var json = File.ReadAllText(settingsFilePath);
                var settings = System.Text.Json.JsonSerializer.Deserialize<Application.Models.AppSettings>(json);
                if (settings != null && !string.IsNullOrWhiteSpace(settings.DatabaseConnectionString))
                {
                    var logger = new FileLoggerService();
                    logger.LogInformation("Connection string loaded from settings file");
                    return settings.DatabaseConnectionString;
                }
            }
        }
        catch (Exception ex)
        {
            var logger = new FileLoggerService();
            logger.LogError("Failed to load connection string from settings file", ex);
        }

        var defaultConnectionString = "Server=localhost;Database=MAS_AML;Trusted_Connection=true;TrustServerCertificate=true;";
        var defaultLogger = new FileLoggerService();
        defaultLogger.LogWarning("Using default connection string for MAS_AML database");
        return defaultConnectionString;
    }
}
