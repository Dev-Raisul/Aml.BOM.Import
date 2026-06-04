namespace Aml.BOM.Import.Shared.Interfaces;

public interface IBomIntegrationService
{
    Task<bool> IntegrateBomAsync(int bomImportRecordId);
    Task<bool> IntegrateBomByParentAsync(string parentItemCode);
    Task<bool> IntegrateNewItemsAsync(IEnumerable<object> items);
    Task<object> GetIntegrationStatusAsync(int bomImportRecordId);
    Task<(int successCount, int failedCount, List<string> errors)> IntegrateBatchBomsAsync(IEnumerable<string> parentItemCodes);
}

