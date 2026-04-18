using Aml.BOM.Import.Shared.Interfaces;

namespace Aml.BOM.Import.Infrastructure.Repositories;

public class NewBuyItemRepository : INewBuyItemRepository
{
    private readonly string _connectionString;

    public NewBuyItemRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<IEnumerable<object>> GetAllAsync()
    {
        // TODO: Implement SQL query to retrieve all new buy items
        await Task.CompletedTask;
        return new List<object>();
    }

    public async Task<object?> GetByIdAsync(int id)
    {
        // TODO: Implement SQL query to retrieve new buy item by ID
        await Task.CompletedTask;
        return null;
    }

    public async Task<int> AddAsync(object newBuyItem)
    {
        // TODO: Implement SQL insert for new buy item
        await Task.CompletedTask;
        return 0;
    }

    public async Task UpdateAsync(object newBuyItem)
    {
        // TODO: Implement SQL update for new buy item
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(int id)
    {
        // TODO: Implement SQL delete for new buy item
        await Task.CompletedTask;
    }

    public async Task<IEnumerable<object>> GetByStatusAsync(int status)
    {
        // TODO: Implement SQL query to retrieve new buy items by status
        await Task.CompletedTask;
        return new List<object>();
    }
}
