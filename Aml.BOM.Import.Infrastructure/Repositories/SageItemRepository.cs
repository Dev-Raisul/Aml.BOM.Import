using Aml.BOM.Import.Shared.Interfaces;
using Aml.BOM.Import.Domain.Entities;
using Microsoft.Data.SqlClient;

namespace Aml.BOM.Import.Infrastructure.Repositories;

public class SageItemRepository : ISageItemRepository
{
    private readonly string _connectionString;
    private readonly ILoggerService _logger;

    public SageItemRepository(string connectionString, ILoggerService logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    public async Task<bool> ItemExistsAsync(string itemCode)
    {
        _logger.LogDebug("Checking if item exists in Sage: {0}", itemCode);

        const string sql = @"
            SELECT COUNT(1) 
            FROM CI_Item 
            WHERE ItemCode = @ItemCode";

        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@ItemCode", itemCode);

            var count = (int)(await command.ExecuteScalarAsync() ?? 0);
            return count > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to check if item exists: {0}", ex, itemCode);
            return false;
        }
    }

    public async Task<string?> GetItemTypeAsync(string itemCode)
    {
        _logger.LogDebug("Getting item type from Sage: {0}", itemCode);

        // Assuming CI_Item has a field like 'ItemType' or 'ProductType'
        // Adjust column names based on actual Sage schema
        const string sql = @"
            SELECT 
                CASE 
                    WHEN ProcurementType = 'B' THEN 'Buy'
                    WHEN ProcurementType = 'M' THEN 'Make'
                    ELSE 'Unknown'
                END as ItemType
            FROM CI_Item 
            WHERE ItemCode = @ItemCode";

        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@ItemCode", itemCode);

            var result = await command.ExecuteScalarAsync();
            return result?.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to get item type: {0}", ex, itemCode);
            return null;
        }
    }

    public async Task<SageItemInfo?> GetItemInfoAsync(string itemCode)
    {
        _logger.LogDebug("Getting item info from Sage: {0}", itemCode);

        // Adjust column names based on actual Sage CI_Item schema
        const string sql = @"
            SELECT 
                ItemCode,
                ItemCodeDesc as Description,
                CASE 
                    WHEN ProcurementType = 'B' THEN 'Buy'
                    WHEN ProcurementType = 'M' THEN 'Make'
                    ELSE 'Unknown'
                END as ItemType,
                Category1 as Category,
                StandardUnitCost as StandardCost
            FROM CI_Item 
            WHERE ItemCode = @ItemCode";

        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@ItemCode", itemCode);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new SageItemInfo
                {
                    ItemCode = reader.GetString(reader.GetOrdinal("ItemCode")),
                    Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
                    ItemType = reader.IsDBNull(reader.GetOrdinal("ItemType")) ? null : reader.GetString(reader.GetOrdinal("ItemType")),
                    Exists = true,
                    Category = reader.IsDBNull(reader.GetOrdinal("Category")) ? null : reader.GetString(reader.GetOrdinal("Category")),
                    StandardCost = reader.IsDBNull(reader.GetOrdinal("StandardCost")) ? null : reader.GetDecimal(reader.GetOrdinal("StandardCost"))
                };
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to get item info: {0}", ex, itemCode);
            return null;
        }
    }

    public async Task<Dictionary<string, bool>> ItemsExistAsync(IEnumerable<string> itemCodes)
    {
        var result = new Dictionary<string, bool>();
        var codesList = itemCodes.ToList();

        _logger.LogDebug("Checking existence of {0} items in Sage", codesList.Count);

        if (!codesList.Any())
            return result;

        // Build parameterized query for batch check
        var parameters = string.Join(",", codesList.Select((_, i) => $"@ItemCode{i}"));
        var sql = $@"
            SELECT ItemCode 
            FROM CI_Item 
            WHERE ItemCode IN ({parameters})";

        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(sql, connection);
            for (int i = 0; i < codesList.Count; i++)
            {
                command.Parameters.AddWithValue($"@ItemCode{i}", codesList[i]);
            }

            var existingItems = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                existingItems.Add(reader.GetString(0));
            }

            // Build result dictionary
            foreach (var code in codesList)
            {
                result[code] = existingItems.Contains(code);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to check items existence", ex);
            // Return all as false on error
            foreach (var code in codesList)
            {
                result[code] = false;
            }
            return result;
        }
    }

    // Legacy methods
    public async Task<object?> GetByItemCodeAsync(string itemCode)
    {
        return await GetItemInfoAsync(itemCode);
    }

    public async Task<IEnumerable<object>> SearchAsync(string searchTerm)
    {
        _logger.LogDebug("Searching items in Sage: {0}", searchTerm);

        const string sql = @"
            SELECT TOP 100
                ItemCode,
                ItemCodeDesc as Description,
                CASE 
                    WHEN ProcurementType = 'B' THEN 'Buy'
                    WHEN ProcurementType = 'M' THEN 'Make'
                    ELSE 'Unknown'
                END as ItemType
            FROM CI_Item 
            WHERE ItemCode LIKE @SearchTerm 
               OR ItemCodeDesc LIKE @SearchTerm
            ORDER BY ItemCode";

        var results = new List<object>();

        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@SearchTerm", $"%{searchTerm}%");

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(new
                {
                    ItemCode = reader.GetString(0),
                    Description = reader.IsDBNull(1) ? null : reader.GetString(1),
                    ItemType = reader.IsDBNull(2) ? null : reader.GetString(2)
                });
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to search items", ex);
            return results;
        }
    }

    public async Task<IEnumerable<object>> SearchItemsWithDetailsAsync(string searchTerm)
    {
        _logger.LogDebug("Searching items with details in Sage: {0}", searchTerm);

        const string sql = @"
            SELECT TOP 100
                ItemCode,
                ItemCodeDesc as ItemDescription,
                ProductLine,
                ProductType,
                ProcurementType as Procurement,
                StandardUnitOfMeasure,
                UDF_SUB_PRODUCT_FAMILY as SubProductFamily,
                UDF_STAGED_ITEM as StagedItem,
                UDF_COATED as Coated,
                'N' as GoldenStandard
            FROM CI_Item 
            WHERE trim(ItemCode) LIKE @SearchTerm 
               OR trim(ItemCodeDesc) LIKE @SearchTerm
            ORDER BY ItemCode";

        var results = new List<object>();

        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@SearchTerm", $"%{searchTerm}%");

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var stagedItemValue = reader.IsDBNull(reader.GetOrdinal("StagedItem")) ? string.Empty : reader.GetString(reader.GetOrdinal("StagedItem"));
                var coatedValue = reader.IsDBNull(reader.GetOrdinal("Coated")) ? string.Empty : reader.GetString(reader.GetOrdinal("Coated"));
                var goldenStandardValue = reader.IsDBNull(reader.GetOrdinal("GoldenStandard")) ? string.Empty : reader.GetString(reader.GetOrdinal("GoldenStandard"));

                results.Add(new SageItemDetails
                {
                    ItemCode = reader.GetString(reader.GetOrdinal("ItemCode")),
                    ItemDescription = reader.IsDBNull(reader.GetOrdinal("ItemDescription")) ? string.Empty : reader.GetString(reader.GetOrdinal("ItemDescription")),
                    ProductLine = reader.IsDBNull(reader.GetOrdinal("ProductLine")) ? string.Empty : reader.GetString(reader.GetOrdinal("ProductLine")),
                    ProductType = reader.IsDBNull(reader.GetOrdinal("ProductType")) ? string.Empty : reader.GetString(reader.GetOrdinal("ProductType")),
                    Procurement = reader.IsDBNull(reader.GetOrdinal("Procurement")) ? string.Empty : reader.GetString(reader.GetOrdinal("Procurement")),
                    StandardUnitOfMeasure = reader.IsDBNull(reader.GetOrdinal("StandardUnitOfMeasure")) ? string.Empty : reader.GetString(reader.GetOrdinal("StandardUnitOfMeasure")),
                    SubProductFamily = reader.IsDBNull(reader.GetOrdinal("SubProductFamily")) ? string.Empty : reader.GetString(reader.GetOrdinal("SubProductFamily")),
                    StagedItem = stagedItemValue.Trim().Equals("Y", StringComparison.OrdinalIgnoreCase),
                    Coated = coatedValue.Trim().Equals("Y", StringComparison.OrdinalIgnoreCase),
                    GoldenStandard = goldenStandardValue.Trim().Equals("Y", StringComparison.OrdinalIgnoreCase)
                });
            }

            _logger.LogInformation("Found {0} items matching '{1}'", results.Count, searchTerm);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to search items with details", ex);
            return results;
        }
    }

    public async Task<bool> ExistsAsync(string itemCode)
    {
        return await ItemExistsAsync(itemCode);
    }

    public async Task<bool> BillExistsInBomHeaderAsync(string billNo)
    {
        _logger.LogDebug("Checking if BillNo exists in BM_BillHeader: {0}", billNo);

        const string sql = @"
            SELECT COUNT(1) 
            FROM BM_BillHeader 
            WHERE BillNo = @BillNo";

        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@BillNo", billNo);

            var count = (int)(await command.ExecuteScalarAsync() ?? 0);
            var exists = count > 0;
            
            if (exists)
            {
                _logger.LogInformation("BillNo found in BM_BillHeader: {0}", billNo);
            }
            
            return exists;
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to check if BillNo exists in BM_BillHeader: {0}", ex, billNo);
            return false;
        }
    }
}
