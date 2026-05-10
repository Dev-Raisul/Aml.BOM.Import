using System.Data;
using Aml.BOM.Import.Domain.Entities;
using Aml.BOM.Import.Shared.Interfaces;
using Microsoft.Data.SqlClient;

namespace Aml.BOM.Import.Infrastructure.Repositories;

public class NewMakeItemRepository : INewMakeItemRepository
{
    private readonly string _connectionString;

    public NewMakeItemRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<IEnumerable<object>> GetAllAsync()
    {
        var items = new List<NewMakeItem>();

        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
                SELECT DISTINCT
                    ComponentItemCode AS ItemCode,
                    ComponentDescription AS ItemDescription,
                    ImportFileName,
                    ImportDate AS ImportFileDate,
                    MIN(Id) AS Id,
                    MAX(DateValidated) AS CreatedDate,
                    MAX(DateValidated) AS ModifiedDate
                FROM isBOMImportBills
                WHERE Status = 'NewMakeItem'
                GROUP BY ComponentItemCode, ComponentDescription, ImportFileName, ImportDate
                ORDER BY ImportDate DESC, ComponentItemCode";

            using var command = new SqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var item = new NewMakeItem
                {
                    Id = reader.GetInt32("Id"),
                    ItemCode = reader.GetString("ItemCode"),
                    ItemDescription = reader.IsDBNull("ItemDescription") ? string.Empty : reader.GetString("ItemDescription"),
                    ImportFileName = reader.GetString("ImportFileName"),
                    ImportFileDate = reader.GetDateTime("ImportFileDate"),
                    ProductLine = string.Empty,
                    ProductType = "F",
                    Procurement = "M",
                    StandardUnitOfMeasure = "EACH",
                    SubProductFamily = string.Empty,
                    StagedItem = false,
                    Coated = false,
                    GoldenStandard = false,
                    IsEdited = false,
                    IsIntegrated = false,
                    CreatedDate = reader.IsDBNull("CreatedDate") ? DateTime.Now : reader.GetDateTime("CreatedDate"),
                    ModifiedDate = reader.IsDBNull("ModifiedDate") ? DateTime.Now : reader.GetDateTime("ModifiedDate")
                };

                items.Add(item);
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Error retrieving new make items: {ex.Message}", ex);
        }

        return items;
    }

    public async Task<object?> GetByIdAsync(int id)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
                SELECT DISTINCT
                    ComponentItemCode AS ItemCode,
                    ComponentDescription AS ItemDescription,
                    ImportFileName,
                    ImportDate AS ImportFileDate,
                    MIN(Id) AS Id,
                    MAX(DateValidated) AS CreatedDate,
                    MAX(DateValidated) AS ModifiedDate
                FROM isBOMImportBills
                WHERE Status = 'NewMakeItem'
                  AND Id = @Id
                GROUP BY ComponentItemCode, ComponentDescription, ImportFileName, ImportDate";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@Id", id);

            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new NewMakeItem
                {
                    Id = reader.GetInt32("Id"),
                    ItemCode = reader.GetString("ItemCode"),
                    ItemDescription = reader.IsDBNull("ItemDescription") ? string.Empty : reader.GetString("ItemDescription"),
                    ImportFileName = reader.GetString("ImportFileName"),
                    ImportFileDate = reader.GetDateTime("ImportFileDate"),
                    ProductLine = string.Empty,
                    ProductType = "F",
                    Procurement = "M",
                    StandardUnitOfMeasure = "EACH",
                    SubProductFamily = string.Empty,
                    StagedItem = false,
                    Coated = false,
                    GoldenStandard = false,
                    IsEdited = false,
                    IsIntegrated = false,
                    CreatedDate = reader.IsDBNull("CreatedDate") ? DateTime.Now : reader.GetDateTime("CreatedDate"),
                    ModifiedDate = reader.IsDBNull("ModifiedDate") ? DateTime.Now : reader.GetDateTime("ModifiedDate")
                };
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Error retrieving new make item by ID: {ex.Message}", ex);
        }

        return null;
    }

    public async Task<int> AddAsync(object newMakeItem)
    {
        if (newMakeItem is not NewMakeItem item)
            throw new ArgumentException("Invalid item type", nameof(newMakeItem));

        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
                INSERT INTO isBOMImportBills 
                (
                    ImportFileName,
                    ImportDate,
                    ComponentItemCode,
                    ComponentDescription,
                    Status,
                    ItemExists,
                    DateValidated
                )
                VALUES 
                (
                    @ImportFileName,
                    @ImportFileDate,
                    @ItemCode,
                    @ItemDescription,
                    'NewMakeItem',
                    0,
                    GETDATE()
                );
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@ImportFileName", item.ImportFileName);
            command.Parameters.AddWithValue("@ImportFileDate", item.ImportFileDate);
            command.Parameters.AddWithValue("@ItemCode", item.ItemCode);
            command.Parameters.AddWithValue("@ItemDescription", (object?)item.ItemDescription ?? DBNull.Value);

            var newId = await command.ExecuteScalarAsync();
            return Convert.ToInt32(newId);
        }
        catch (Exception ex)
        {
            throw new Exception($"Error adding new make item: {ex.Message}", ex);
        }
    }

    public async Task UpdateAsync(object newMakeItem)
    {
        if (newMakeItem is not NewMakeItem item)
            throw new ArgumentException("Invalid item type", nameof(newMakeItem));

        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            // Update all records with this component item code from the same import file
            var query = @"
                UPDATE isBOMImportBills
                SET 
                    ComponentDescription = @ItemDescription,
                    ValidationMessage = @ValidationMessage,
                    DateValidated = GETDATE()
                WHERE Status = 'NewMakeItem'
                  AND ComponentItemCode = @ItemCode
                  AND ImportFileName = @ImportFileName";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@ItemCode", item.ItemCode);
            command.Parameters.AddWithValue("@ImportFileName", item.ImportFileName);
            command.Parameters.AddWithValue("@ItemDescription", (object?)item.ItemDescription ?? DBNull.Value);
            command.Parameters.AddWithValue("@ValidationMessage", 
                item.IsEdited ? "Item edited - ready for review" : "New make item identified");

            await command.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            throw new Exception($"Error updating new make item: {ex.Message}", ex);
        }
    }

    public async Task DeleteAsync(int id)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
                DELETE FROM isBOMImportBills
                WHERE Id = @Id AND Status = 'NewMakeItem'";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@Id", id);

            await command.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            throw new Exception($"Error deleting new make item: {ex.Message}", ex);
        }
    }

    public async Task<IEnumerable<object>> GetByStatusAsync(int status)
    {
        // For make items, status is always "NewMakeItem"
        // This parameter is kept for interface compatibility
        return await GetAllAsync();
    }

    public async Task BulkUpdateFieldAsync(IEnumerable<int> itemIds, string fieldName, object value)
    {
        if (!itemIds.Any())
            return;

        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            // Map field names to database columns
            var columnName = fieldName switch
            {
                nameof(NewMakeItem.ItemDescription) => "ComponentDescription",
                _ => throw new ArgumentException($"Bulk update not supported for field: {fieldName}")
            };

            var query = $@"
                UPDATE isBOMImportBills
                SET {columnName} = @Value,
                    DateValidated = GETDATE()
                WHERE Id IN ({string.Join(",", itemIds)})
                  AND Status = 'NewMakeItem'";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@Value", value ?? DBNull.Value);

            await command.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            throw new Exception($"Error bulk updating field {fieldName}: {ex.Message}", ex);
        }
    }

    public async Task<int> GetCountByImportFileAsync(string importFileName)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
                SELECT COUNT(DISTINCT ComponentItemCode)
                FROM isBOMImportBills
                WHERE Status = 'NewMakeItem'
                  AND ImportFileName = @ImportFileName";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@ImportFileName", importFileName);

            var count = await command.ExecuteScalarAsync();
            return Convert.ToInt32(count);
        }
        catch (Exception ex)
        {
            throw new Exception($"Error getting count by import file: {ex.Message}", ex);
        }
    }

    public async Task<IEnumerable<object>> GetByImportFileAsync(string importFileName)
    {
        var items = new List<NewMakeItem>();

        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
                SELECT DISTINCT
                    ComponentItemCode AS ItemCode,
                    ComponentDescription AS ItemDescription,
                    ImportFileName,
                    ImportDate AS ImportFileDate,
                    MIN(Id) AS Id,
                    MAX(DateValidated) AS CreatedDate,
                    MAX(DateValidated) AS ModifiedDate
                FROM isBOMImportBills
                WHERE Status = 'NewMakeItem'
                  AND ImportFileName = @ImportFileName
                GROUP BY ComponentItemCode, ComponentDescription, ImportFileName, ImportDate
                ORDER BY ComponentItemCode";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@ImportFileName", importFileName);

            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var item = new NewMakeItem
                {
                    Id = reader.GetInt32("Id"),
                    ItemCode = reader.GetString("ItemCode"),
                    ItemDescription = reader.IsDBNull("ItemDescription") ? string.Empty : reader.GetString("ItemDescription"),
                    ImportFileName = reader.GetString("ImportFileName"),
                    ImportFileDate = reader.GetDateTime("ImportFileDate"),
                    ProductLine = string.Empty,
                    ProductType = "F",
                    Procurement = "M",
                    StandardUnitOfMeasure = "EACH",
                    SubProductFamily = string.Empty,
                    StagedItem = false,
                    Coated = false,
                    GoldenStandard = false,
                    IsEdited = false,
                    IsIntegrated = false,
                    CreatedDate = reader.IsDBNull("CreatedDate") ? DateTime.Now : reader.GetDateTime("CreatedDate"),
                    ModifiedDate = reader.IsDBNull("ModifiedDate") ? DateTime.Now : reader.GetDateTime("ModifiedDate")
                };

                items.Add(item);
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Error retrieving new make items by import file: {ex.Message}", ex);
        }

        return items;
    }

    public async Task MarkAsIntegratedAsync(string itemCode, string importFileName)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
                UPDATE isBOMImportBills
                SET 
                    Status = 'Integrated',
                    DateIntegrated = GETDATE(),
                    IntegratedBy = @IntegratedBy
                WHERE ComponentItemCode = @ItemCode
                  AND ImportFileName = @ImportFileName
                  AND Status = 'NewMakeItem'";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@ItemCode", itemCode);
            command.Parameters.AddWithValue("@ImportFileName", importFileName);
            command.Parameters.AddWithValue("@IntegratedBy", Environment.UserName);

            await command.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            throw new Exception($"Error marking item as integrated: {ex.Message}", ex);
        }
    }
}
