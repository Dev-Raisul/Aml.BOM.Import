using Aml.BOM.Import.Shared.Interfaces;

namespace Aml.BOM.Import.Infrastructure.Repositories;

public class SageItemRepository : ISageItemRepository
{
    private readonly string _connectionString;

    public SageItemRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<object?> GetByItemCodeAsync(string itemCode)
    {
        // TODO: Implement Sage database query to retrieve item by code
        await Task.CompletedTask;
        return null;
    }

    public async Task<IEnumerable<object>> SearchAsync(string searchTerm)
    {
        // TODO: Implement Sage database query to search items
        await Task.CompletedTask;
        return new List<object>();
    }

    public async Task<bool> ExistsAsync(string itemCode)
    {
        // TODO: Implement Sage database query to check if item exists
        await Task.CompletedTask;
        return false;
    }
}
