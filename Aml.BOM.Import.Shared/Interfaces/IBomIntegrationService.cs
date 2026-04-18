namespace Aml.BOM.Import.Shared.Interfaces;

public interface IBomIntegrationService
{
    Task<bool> IntegrateBomAsync(int bomImportRecordId);
    Task<bool> IntegrateNewItemsAsync(IEnumerable<int> itemIds);
    Task<object> GetIntegrationStatusAsync(int bomImportRecordId);
}
