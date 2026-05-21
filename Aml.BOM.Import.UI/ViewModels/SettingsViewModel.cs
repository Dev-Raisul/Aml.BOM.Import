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
            // Validate Sage path before saving
            if (!string.IsNullOrWhiteSpace(SagePath) && IsUncPath(SagePath))
            {
                var result = System.Windows.MessageBox.Show(
                    "⚠ WARNING: Network Path Detected\n\n" +
                    $"Sage Home Directory is set to a network path:\n{SagePath}\n\n" +
                    "This may cause Sage integration to FAIL!\n\n" +
                    "It is STRONGLY RECOMMENDED to map this network path to a drive letter.\n\n" +
                    "Do you want to save these settings anyway?",
                    "Network Path Warning",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Warning);
                
                if (result == System.Windows.MessageBoxResult.No)
                {
                    StatusMessage = "⚠ Settings not saved - Please map network drive to a drive letter.";
                    _logger.LogWarning("User cancelled save due to UNC path warning");
                    return;
                }
                
                _logger.LogWarning("User chose to save settings with UNC path: {0}", SagePath);
            }
            
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
            
            // Add warning to success message if UNC path
            if (IsUncPath(SagePath))
            {
                StatusMessage = "⚠ Settings saved - WARNING: Network path may cause integration failures!";
            }
            else
            {
                StatusMessage = "Settings saved successfully!";
            }
            
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
                Description = "Select Sage 100 Home Directory - MUST be a mapped drive (e.g., Z:\\Sage\\MAS90\\Home)\nNetwork paths (\\\\server\\share) are NOT supported",
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
                var selectedPath = dialog.SelectedPath;
                
                // Check if it's a UNC path (network path)
                if (IsUncPath(selectedPath))
                {
                    _logger.LogWarning("UNC path rejected: {0}", selectedPath);
                    
                    var result = System.Windows.MessageBox.Show(
                        "⚠ Network Path Not Supported\n\n" +
                        $"You selected a network path:\n{selectedPath}\n\n" +
                        "Sage integration requires a MAPPED DRIVE letter (e.g., Z:\\).\n\n" +
                        "Network paths (\\\\server\\share) may cause integration to fail.\n\n" +
                        "Would you like to:\n" +
                        "• Map this network path to a drive letter (recommended)\n" +
                        "• Continue anyway (not recommended - integration may fail)\n\n" +
                        "Click YES to map the drive, NO to continue anyway, or CANCEL to select a different folder.",
                        "Mapped Drive Required",
                        System.Windows.MessageBoxButton.YesNoCancel,
                        System.Windows.MessageBoxImage.Warning);
                    
                    if (result == System.Windows.MessageBoxResult.Yes)
                    {
                        // Show instructions for mapping a drive
                        System.Windows.MessageBox.Show(
                            "How to Map a Network Drive:\n\n" +
                            "1. Open File Explorer (Windows + E)\n" +
                            "2. Click 'This PC' in the left sidebar\n" +
                            "3. Click 'Map network drive' in the ribbon (or right-click 'This PC' → 'Map network drive')\n" +
                            "4. Choose a drive letter (e.g., Z:)\n" +
                            $"5. Enter the network path: {selectedPath}\n" +
                            "6. Check 'Reconnect at sign-in'\n" +
                            "7. Click 'Finish'\n" +
                            "8. Come back here and browse to the new drive letter\n\n" +
                            "After mapping, the path will look like:\nZ:\\MAS90\\Home (instead of \\\\server\\share\\MAS90\\Home)",
                            "Drive Mapping Instructions",
                            System.Windows.MessageBoxButton.OK,
                            System.Windows.MessageBoxImage.Information);
                        
                        StatusMessage = "⚠ Please map the network drive and try again.";
                        return;
                    }
                    else if (result == System.Windows.MessageBoxResult.No)
                    {
                        // User chose to continue anyway
                        SagePath = selectedPath;
                        StatusMessage = "⚠ WARNING: Using network path - Integration may fail! Please map to a drive letter.";
                        _logger.LogWarning("User chose to use UNC path despite warning: {0}", selectedPath);
                        return;
                    }
                    else
                    {
                        // User cancelled - do nothing
                        StatusMessage = "Sage path selection cancelled.";
                        return;
                    }
                }
                
                // Check if it's a mapped drive (preferred)
                if (IsMappedDrive(selectedPath))
                {
                    SagePath = selectedPath;
                    _logger.LogInformation("Mapped drive Sage path selected: {0}", SagePath);
                    
                    // Validate if the selected path looks like a Sage directory
                    if (!ValidateSagePath(SagePath))
                    {
                        StatusMessage = "⚠ Warning: Selected directory may not be a valid Sage 100 Home directory.";
                    }
                    else
                    {
                        StatusMessage = "✓ Sage path selected successfully (Mapped Drive)!";
                    }
                }
                else
                {
                    // Local drive (C:, D:, etc.) - this is also acceptable
                    SagePath = selectedPath;
                    _logger.LogInformation("Local drive Sage path selected: {0}", SagePath);
                    
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

    /// <summary>
    /// Checks if the path is a UNC path (network path like \\server\share)
    /// </summary>
    private bool IsUncPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        // UNC paths start with \\ or //
        return path.StartsWith(@"\\") || path.StartsWith("//");
    }

    /// <summary>
    /// Checks if the path is a mapped drive (has a drive letter)
    /// </summary>
    private bool IsMappedDrive(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        // Mapped drives start with a drive letter followed by colon (e.g., Z:, X:)
        // But we need to differentiate from local drives
        if (path.Length >= 2 && char.IsLetter(path[0]) && path[1] == ':')
        {
            var driveLetter = path.Substring(0, 2);
            var driveInfo = new DriveInfo(driveLetter);
            
            // Network drives have DriveType.Network
            return driveInfo.DriveType == DriveType.Network;
        }

        return false;
    }
}
