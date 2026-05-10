using Aml.BOM.Import.Domain.Entities;
using Aml.BOM.Import.Shared.Interfaces;
using Microsoft.Data.SqlClient;

namespace Aml.BOM.Import.Infrastructure.Repositories;

public class NewBuyItemRepository : INewBuyItemRepository
{
    private readonly string _connectionString;
    private readonly ILoggerService _logger;

    public NewBuyItemRepository(string connectionString, ILoggerService logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    public async Task<IEnumerable<object>> GetAllAsync()
    {
        _logger.LogDebug("Retrieving all new buy items");

        const string sql = @"
            SELECT DISTINCT
                ComponentItemCode as ItemCode,
                ComponentDescription as Description,
                UnitOfMeasure,
                MIN(ImportDate) as IdentifiedDate,
                MIN(ImportWindowsUser) as IdentifiedBy,
                COUNT(*) as OccurrenceCount
            FROM isBOMImportBills
            WHERE Status = 'NewBuyItem'
            GROUP BY ComponentItemCode, ComponentDescription, UnitOfMeasure
            ORDER BY ComponentItemCode";

        var items = new List<object>();

        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(sql, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                items.Add(new
                {
                    ItemCode = reader.GetString(reader.GetOrdinal("ItemCode")),
                    Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? string.Empty : reader.GetString(reader.GetOrdinal("Description")),
                    UnitOfMeasure = reader.IsDBNull(reader.GetOrdinal("UnitOfMeasure")) ? string.Empty : reader.GetString(reader.GetOrdinal("UnitOfMeasure")),
                    IdentifiedDate = reader.GetDateTime(reader.GetOrdinal("IdentifiedDate")),
                    IdentifiedBy = reader.GetString(reader.GetOrdinal("IdentifiedBy")),
                    OccurrenceCount = reader.GetInt32(reader.GetOrdinal("OccurrenceCount"))
                });
            }

            _logger.LogInformation("Retrieved {0} new buy items", items.Count);
            return items;
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to retrieve new buy items", ex);
            throw;
        }
    }

    public async Task<object?> GetByIdAsync(int id)
    {
        // Not applicable for this implementation as we query directly from isBOMImportBills
        await Task.CompletedTask;
        return null;
    }

    public async Task<int> AddAsync(object newBuyItem)
    {
        // Not applicable - items are added through BOM import process
        await Task.CompletedTask;
        return 0;
    }

    public async Task UpdateAsync(object newBuyItem)
    {
        // Not applicable - items are updated through validation process
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(int id)
    {
        // Not applicable - items are managed through BOM import bills
        await Task.CompletedTask;
    }

    public async Task<IEnumerable<object>> GetByStatusAsync(int status)
    {
        // Returns all new buy items (status is implicit in the query)
        return await GetAllAsync();
    }

    public async Task<int> GetCountAsync()
    {
        _logger.LogDebug("Getting count of new buy items");

        const string sql = @"
            SELECT COUNT(DISTINCT ComponentItemCode)
            FROM isBOMImportBills
            WHERE Status = 'NewBuyItem'";

        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(sql, connection);
            var count = (int)(await command.ExecuteScalarAsync() ?? 0);

            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to get count of new buy items", ex);
            return 0;
        }
    }
}
