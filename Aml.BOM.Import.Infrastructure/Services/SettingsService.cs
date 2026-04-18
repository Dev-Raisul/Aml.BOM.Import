using System.Text.Json;
using Aml.BOM.Import.Application.Models;
using Aml.BOM.Import.Shared.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Aml.BOM.Import.Infrastructure.Services;

public class SettingsService : ISettingsService
{
    private readonly string _settingsFilePath;
    private AppSettings? _cachedSettings;

    public SettingsService()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appFolder = Path.Combine(appDataPath, "Aml.BOM.Import");
        Directory.CreateDirectory(appFolder);
        _settingsFilePath = Path.Combine(appFolder, "appsettings.json");
    }

    public async Task<object> GetSettingsAsync()
    {
        if (_cachedSettings != null)
            return _cachedSettings;

        if (!File.Exists(_settingsFilePath))
        {
            _cachedSettings = new AppSettings();
            await SaveSettingsAsync(_cachedSettings);
            return _cachedSettings;
        }

        var json = await File.ReadAllTextAsync(_settingsFilePath);
        _cachedSettings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        return _cachedSettings;
    }

    public async Task SaveSettingsAsync(object settings)
    {
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(_settingsFilePath, json);
        _cachedSettings = settings as AppSettings;
    }

    public async Task<bool> ValidateConnectionAsync()
    {
        // TODO: Implement database connection validation
        await Task.CompletedTask;
        return false;
    }
}
