using Aml.BOM.Import.Application.Models;
using Aml.BOM.Import.Shared.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.IO;
using System.Windows.Forms;

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
    private string _sagePath = string.Empty;

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
                SagePath = settings.SageSettings.SagePath;
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
        return $"Server={DatabaseServer};Database={DatabaseName};User Id={DatabaseUsername};Password={DatabasePassword};TrustServerCertificate=true";
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
                    SagePath = SagePath,
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
                ? "✓ Connection successful!" 
                : "✗ Connection failed. Please check your settings.";
            
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
            StatusMessage = $"✗ Connection error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void BrowseSagePath()
    {
        try
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = "Select Sage 100 Home Directory (e.g., C:\\Sage\\Sage100Standard\\MAS90\\Home)",
                UseDescriptionForTitle = true,
                ShowNewFolderButton = false
            };

            // Set initial directory if SagePath is already set
            if (!string.IsNullOrWhiteSpace(SagePath) && Directory.Exists(SagePath))
            {
                dialog.InitialDirectory = SagePath;
            }
            else
            {
                // Default to C:\Sage if it exists
                if (Directory.Exists(@"C:\Sage"))
                {
                    dialog.InitialDirectory = @"C:\Sage";
                }
            }

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                SagePath = dialog.SelectedPath;
                _logger.LogInformation("Sage path selected: {0}", SagePath);
                
                // Validate if the selected path looks like a Sage directory
                if (!ValidateSagePath(SagePath))
                {
                    StatusMessage = "⚠ Warning: Selected directory may not be a valid Sage 100 Home directory.";
                }
                else
                {
                    StatusMessage = "✓ Sage path selected successfully!";
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Error browsing for Sage path", ex);
            StatusMessage = $"Error selecting folder: {ex.Message}";
        }
    }

    [RelayCommand]
    private void BrowseReportOutputDirectory()
    {
        try
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = "Select Report Output Directory",
                UseDescriptionForTitle = true,
                ShowNewFolderButton = true
            };

            // Set initial directory if ReportOutputDirectory is already set
            if (!string.IsNullOrWhiteSpace(ReportOutputDirectory) && Directory.Exists(ReportOutputDirectory))
            {
                dialog.InitialDirectory = ReportOutputDirectory;
            }

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                ReportOutputDirectory = dialog.SelectedPath;
                _logger.LogInformation("Report output directory selected: {0}", ReportOutputDirectory);
                StatusMessage = "✓ Report directory selected successfully!";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Error browsing for report output directory", ex);
            StatusMessage = $"Error selecting folder: {ex.Message}";
        }
    }

    /// <summary>
    /// Validates if the selected path looks like a valid Sage 100 Home directory
    /// </summary>
    private bool ValidateSagePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            return false;

        // Check for common Sage files/folders
        // Typical Sage 100 Home directory contains files like pvx.exe, pvxwin32.exe, or folders like SOA, MAS_* etc.
        var sageIndicators = new[]
        {
            "pvx.exe",
            "pvxwin32.exe",
            "pvx.ini",
            "SOA",
            "MAS90.exe"
        };

        foreach (var indicator in sageIndicators)
        {
            var fullPath = Path.Combine(path, indicator);
            if (File.Exists(fullPath) || Directory.Exists(fullPath))
            {
                return true;
            }
        }

        // Check if path contains typical Sage folder names
        var pathLower = path.ToLower();
        if (pathLower.Contains("sage") && pathLower.Contains("mas90"))
        {
            return true;
        }

        return false;
    }
}
