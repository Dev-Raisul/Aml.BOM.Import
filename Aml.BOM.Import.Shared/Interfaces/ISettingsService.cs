namespace Aml.BOM.Import.Shared.Interfaces;

public interface ISettingsService
{
    Task<object> GetSettingsAsync();
    Task SaveSettingsAsync(object settings);
    Task<bool> ValidateConnectionAsync();
}
