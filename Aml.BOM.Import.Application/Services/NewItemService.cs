using Aml.BOM.Import.Shared.Interfaces;

namespace Aml.BOM.Import.Application.Services;

public class NewItemService
{
    private readonly INewMakeItemRepository _newMakeItemRepository;
    private readonly INewBuyItemRepository _newBuyItemRepository;
    private readonly ISageItemRepository _sageItemRepository;

    public NewItemService(
        INewMakeItemRepository newMakeItemRepository,
        INewBuyItemRepository newBuyItemRepository,
        ISageItemRepository sageItemRepository)
    {
        _newMakeItemRepository = newMakeItemRepository;
        _newBuyItemRepository = newBuyItemRepository;
        _sageItemRepository = sageItemRepository;
    }

    public async Task<IEnumerable<object>> GetNewMakeItemsAsync()
    {
        return await _newMakeItemRepository.GetAllAsync();
    }

    public async Task<IEnumerable<object>> GetNewBuyItemsAsync()
    {
        return await _newBuyItemRepository.GetAllAsync();
    }

    public async Task BulkUpdateMakeItemFieldAsync(IEnumerable<int> itemIds, string fieldName, object value)
    {
        await _newMakeItemRepository.BulkUpdateFieldAsync(itemIds, fieldName, value);
    }

    public async Task CopyItemFieldsAsync(string sourceItemCode, int targetItemId)
    {
        // TODO: Implement copy-from-item functionality
        await Task.CompletedTask;
    }
}
