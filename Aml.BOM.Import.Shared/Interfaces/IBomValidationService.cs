namespace Aml.BOM.Import.Shared.Interfaces;

public interface IBomValidationService
{
    Task<object> ValidateBomAsync(object bomImportRecord);
    Task<IEnumerable<object>> IdentifyNewItemsAsync(object bomImportRecord);
    Task<bool> CheckForDuplicatesAsync(string bomNumber);
}
