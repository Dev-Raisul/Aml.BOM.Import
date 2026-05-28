using Aml.BOM.Import.Domain.Entities;

namespace Aml.BOM.Import.Shared.Interfaces;

public interface INewBuyItemRepository
{
    Task<IEnumerable<NewBuyItem>> GetAllAsync();
    Task<NewBuyItem?> GetByIdAsync(int id);
    Task<int> AddAsync(NewBuyItem newBuyItem);
    Task UpdateAsync(NewBuyItem newBuyItem);
    Task DeleteAsync(int id);
    Task<IEnumerable<NewBuyItem>> GetByStatusAsync(int status);
    Task<int> GetCountAsync();
}
