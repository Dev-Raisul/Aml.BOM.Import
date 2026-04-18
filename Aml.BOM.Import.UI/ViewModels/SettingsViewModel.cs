using Aml.BOM.Import.Application.Models;
using Aml.BOM.Import.Shared.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Aml.BOM.Import.UI.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;

    [ObservableProperty]
    private string _databaseConnectionString = string.Empty;

    [ObservableProperty]
    private string _sageServerUrl = string.Empty;

    [ObservableProperty]
    private string _sageUsername = string.Empty;

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

    public SettingsViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService;
        LoadSettingsCommand.Execute(null);
    }

    [RelayCommand]
    private async Task LoadSettings()
    {
        var settings = await _settingsService.GetSettingsAsync() as AppSettings;
        if (settings != null)
        {
            DatabaseConnectionString = settings.DatabaseConnectionString;
            SageServerUrl = settings.SageSettings.ServerUrl;
            SageUsername = settings.SageSettings.Username;
            SageCompanyCode = settings.SageSettings.CompanyCode;
            ReportOutputDirectory = settings.ReportSettings.OutputDirectory;
            AutoGenerateReports = settings.ReportSettings.AutoGenerateReports;
        }
    }

    [RelayCommand]
    private async Task SaveSettings()
    {
        IsSaving = true;
        StatusMessage = string.Empty;

        try
        {
            var settings = new AppSettings
            {
                DatabaseConnectionString = DatabaseConnectionString,
                SageSettings = new SageSettings
                {
                    ServerUrl = SageServerUrl,
                    Username = SageUsername,
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
        }
        catch (Exception ex)
        {
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
        var isValid = await _settingsService.ValidateConnectionAsync();
        StatusMessage = isValid ? "Connection successful!" : "Connection failed.";
    }
}
