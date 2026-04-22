using System.Text.Json;
using Aml.BOM.Import.Application.Models;
using Aml.BOM.Import.Shared.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Aml.BOM.Import.Infrastructure.Services;

public class SettingsService : ISettingsService
{
    private readonly string _settingsFilePath;
    private readonly IDatabaseConnectionService _databaseConnectionService;
    private readonly ILoggerService _logger;
    private AppSettings? _cachedSettings;

    public SettingsService(IDatabaseConnectionService databaseConnectionService, ILoggerService logger)
    {
        _databaseConnectionService = databaseConnectionService;
        _logger = logger;
        
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appFolder = Path.Combine(appDataPath, "Aml.BOM.Import");
        Directory.CreateDirectory(appFolder);
        _settingsFilePath = Path.Combine(appFolder, "appsettings.json");
        
        _logger.LogInformation("SettingsService initialized. Settings file path: {0}", _settingsFilePath);
    }

    public async Task<object> GetSettingsAsync()
    {
        _logger.LogDebug("GetSettingsAsync called");
        
        if (_cachedSettings != null)
        {
            _logger.LogDebug("Returning cached settings");
            return _cachedSettings;
        }

        if (!File.Exists(_settingsFilePath))
        {
            _logger.LogInformation("Settings file does not exist. Creating default settings.");
            _cachedSettings = new AppSettings();
            await SaveSettingsAsync(_cachedSettings);
            return _cachedSettings;
        }

        try
        {
            _logger.LogInformation("Loading settings from file: {0}", _settingsFilePath);
            var json = await File.ReadAllTextAsync(_settingsFilePath);
            _cachedSettings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            _logger.LogInformation("Settings loaded successfully");
            return _cachedSettings;
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to load settings from file", ex);
            _cachedSettings = new AppSettings();
            return _cachedSettings;
        }
    }

    public async Task SaveSettingsAsync(object settings)
    {
        try
        {
            _logger.LogInformation("Saving settings to file: {0}", _settingsFilePath);
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_settingsFilePath, json);
            _cachedSettings = settings as AppSettings;
            _logger.LogInformation("Settings saved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to save settings to file", ex);
            throw;
        }
    }

    public async Task<bool> ValidateConnectionAsync()
    {
        _logger.LogInformation("Validating database connection");
        
        var settings = await GetSettingsAsync() as AppSettings;
        if (settings == null || string.IsNullOrWhiteSpace(settings.DatabaseConnectionString))
        {
            _logger.LogWarning("Cannot validate connection: settings or connection string is null/empty");
            return false;
        }

        var isValid = await _databaseConnectionService.TestConnectionAsync(settings.DatabaseConnectionString);
        
        if (isValid)
        {
            _logger.LogInformation("Database connection validation successful");
        }
        else
        {
            _logger.LogWarning("Database connection validation failed");
        }
        
        return isValid;
    }
}
