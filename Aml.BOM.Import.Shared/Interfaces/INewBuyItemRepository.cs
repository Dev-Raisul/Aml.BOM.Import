namespace Aml.BOM.Import.Shared.Interfaces;

public interface INewBuyItemRepository
{
    Task<IEnumerable<object>> GetAllAsync();
    Task<object?> GetByIdAsync(int id);
    Task<int> AddAsync(object newBuyItem);
    Task UpdateAsync(object newBuyItem);
    Task DeleteAsync(int id);
    Task<IEnumerable<object>> GetByStatusAsync(int status);
    Task<int> GetCountAsync();
}
