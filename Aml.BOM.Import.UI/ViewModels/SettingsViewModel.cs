using Aml.BOM.Import.Application.Models;
using Aml.BOM.Import.Shared.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Aml.BOM.Import.UI.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly IDatabaseConnectionService _databaseConnectionService;
    private readonly ILoggerService _logger;

    [ObservableProperty]
    private string _databaseServer = string.Empty;

    [ObservableProperty]
    private string _databaseName = string.Empty;

    [ObservableProperty]
    private string _databaseUsername = string.Empty;

    [ObservableProperty]
    private string _databasePassword = string.Empty;

    [ObservableProperty]
    private string _sageServerUrl = string.Empty;

    [ObservableProperty]
    private string _sageUsername = string.Empty;

    [ObservableProperty]
    private string _sagePassword = string.Empty;

    [ObservableProperty]
    private string _sageCompanyCode = string.Empty;

    [ObservableProperty]
    private string _reportOutputDirectory = string.Empty;

    [ObservableProperty]
    private bool _autoGenerateReports;

    [ObservableProperty]
    private bool _isSaving;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public SettingsViewModel(ISettingsService settingsService, IDatabaseConnectionService databaseConnectionService, ILoggerService logger)
    {
        _settingsService = settingsService;
        _databaseConnectionService = databaseConnectionService;
        _logger = logger;
        
        _logger.LogInformation("SettingsViewModel initialized");
        LoadSettingsCommand.Execute(null);
    }

    [RelayCommand]
    private async Task LoadSettings()
    {
        try
        {
            _logger.LogInformation("Loading settings");
            var settings = await _settingsService.GetSettingsAsync() as AppSettings;
            if (settings != null)
            {
                ParseConnectionString(settings.DatabaseConnectionString);
                SageServerUrl = settings.SageSettings.ServerUrl;
                SageUsername = settings.SageSettings.Username;
                SagePassword = settings.SageSettings.Password ?? string.Empty;
                SageCompanyCode = settings.SageSettings.CompanyCode;
                ReportOutputDirectory = settings.ReportSettings.OutputDirectory;
                AutoGenerateReports = settings.ReportSettings.AutoGenerateReports;
                _logger.LogInformation("Settings loaded successfully");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to load settings", ex);
            StatusMessage = $"Error loading settings: {ex.Message}";
        }
    }

    private void ParseConnectionString(string connectionString)
    {
        if (string.IsNullOrEmpty(connectionString)) return;

        var parts = connectionString.Split(';');
        foreach (var part in parts)
        {
            var keyValue = part.Split('=', 2);
            if (keyValue.Length != 2) continue;

            var key = keyValue[0].Trim().ToLower();
            var value = keyValue[1].Trim();

            switch (key)
            {
                case "server":
                case "data source":
                    DatabaseServer = value;
                    break;
                case "database":
                case "initial catalog":
                    DatabaseName = value;
                    break;
                case "user id":
                case "uid":
                    DatabaseUsername = value;
                    break;
                case "password":
                case "pwd":
                    DatabasePassword = value;
                    break;
            }
        }
    }

    private string BuildConnectionString()
    {
        return $"Server={DatabaseServer};Database={DatabaseName};User Id={DatabaseUsername};Password={DatabasePassword};";
    }

    [RelayCommand]
    private async Task SaveSettings()
    {
        IsSaving = true;
        StatusMessage = string.Empty;

        try
        {
            _logger.LogInformation("Saving settings - Server={0}, Database={1}", DatabaseServer, DatabaseName);
            
            var settings = new AppSettings
            {
                DatabaseConnectionString = BuildConnectionString(),
                SageSettings = new SageSettings
                {
                    ServerUrl = SageServerUrl,
                    Username = SageUsername,
                    Password = SagePassword,
                    CompanyCode = SageCompanyCode
                },
                ReportSettings = new ReportSettings
                {
                    OutputDirectory = ReportOutputDirectory,
                    AutoGenerateReports = AutoGenerateReports
                }
            };

            await _settingsService.SaveSettingsAsync(settings);
            StatusMessage = "Settings saved successfully!";
            _logger.LogInformation("Settings saved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to save settings", ex);
            StatusMessage = $"Error saving settings: {ex.Message}";
        }
        finally
        {
            IsSaving = false;
        }
    }

    [RelayCommand]
    private async Task TestConnection()
    {
        StatusMessage = "Testing connection...";
        
        try
        {
            _logger.LogInformation("Testing database connection - Server={0}, Database={1}", DatabaseServer, DatabaseName);
            
            var isValid = await _databaseConnectionService.TestConnectionAsync(
                DatabaseServer, 
                DatabaseName, 
                DatabaseUsername, 
                DatabasePassword);
            
            StatusMessage = isValid 
                ? "? Connection successful!" 
                : "? Connection failed. Please check your settings.";
            
            if (isValid)
            {
                _logger.LogInformation("Connection test successful");
            }
            else
            {
                _logger.LogWarning("Connection test failed");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Connection test error", ex);
            StatusMessage = $"? Connection error: {ex.Message}";
        }
    }
}
