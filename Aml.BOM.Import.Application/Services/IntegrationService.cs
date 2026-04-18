using Aml.BOM.Import.Shared.Interfaces;

namespace Aml.BOM.Import.Application.Services;

public class IntegrationService
{
    private readonly IBomIntegrationService _bomIntegrationService;

    public IntegrationService(IBomIntegrationService bomIntegrationService)
    {
        _bomIntegrationService = bomIntegrationService;
    }

    public async Task<bool> IntegrateBomToSageAsync(int bomImportRecordId)
    {
        return await _bomIntegrationService.IntegrateBomAsync(bomImportRecordId);
    }

    public async Task<bool> IntegrateItemsToSageAsync(IEnumerable<int> itemIds)
    {
        return await _bomIntegrationService.IntegrateNewItemsAsync(itemIds);
    }

    public async Task<object> GetIntegrationStatusAsync(int bomImportRecordId)
    {
        return await _bomIntegrationService.GetIntegrationStatusAsync(bomImportRecordId);
    }
}
