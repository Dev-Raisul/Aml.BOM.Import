namespace Aml.BOM.Import.Shared.Interfaces;

public interface INewMakeItemRepository
{
    Task<IEnumerable<object>> GetAllAsync();
    Task<object?> GetByIdAsync(int id);
    Task<int> AddAsync(object newMakeItem);
    Task UpdateAsync(object newMakeItem);
    Task DeleteAsync(int id);
    Task<IEnumerable<object>> GetByStatusAsync(int status);
    Task BulkUpdateFieldAsync(IEnumerable<int> itemIds, string fieldName, object value);
}
