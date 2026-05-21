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

    // -------------------------------------------------------------------------
    // Read helpers
    // -------------------------------------------------------------------------

    private static NewMakeItem MapRow(SqlDataReader reader) => new()
    {
        Id                    = reader.GetInt32("Id"),
        ItemCode              = reader.GetString("ItemCode"),
        ImportFileName        = reader.GetString("ImportFileName"),
        ImportFileDate        = reader.GetDateTime("ImportDate"),
        ItemDescription       = reader.IsDBNull("ItemDescription") ? string.Empty : reader.GetString("ItemDescription"),
        ProductLine           = reader.IsDBNull("ProductLine") ? string.Empty : reader.GetString("ProductLine"),
        ProductType           = reader.GetString("ProductType"),
        Procurement           = reader.GetString("Procurement"),
        StandardUnitOfMeasure = reader.GetString("StandardUnitOfMeasure"),
        SubProductFamily      = reader.IsDBNull("SubProductFamily") ? string.Empty : reader.GetString("SubProductFamily"),
        StagedItem            = reader.GetBoolean("StagedItem"),
        Coated                = reader.GetBoolean("Coated"),
        GoldenStandard        = reader.GetBoolean("GoldenStandard"),
        IsIntegrated          = reader.GetBoolean("IsIntegrated"),
        IntegratedDate        = reader.IsDBNull("DateIntegrated") ? null : reader.GetDateTime("DateIntegrated"),
        IntegratedBy          = reader.IsDBNull("IntegratedBy") ? null : reader.GetString("IntegratedBy"),
        CreatedDate           = reader.GetDateTime("CreatedDate"),
        CreatedWindowsUser    = reader.GetString("CreatedWindowsUser"),
        ModifiedDate          = reader.GetDateTime("ModifiedDate"),
        ModifiedWindowsUser   = reader.GetString("ModifiedWindowsUser"),
        IsEdited              = false
    };

    // -------------------------------------------------------------------------
    // GetAllAsync – reads from isBOMImport_NewMakeItems
    // -------------------------------------------------------------------------

    public async Task<IEnumerable<object>> GetAllAsync()
    {
        var items = new List<NewMakeItem>();

        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            const string query = @"
                SELECT
                    Id, ItemCode, ImportFileName, ImportDate, ItemDescription,
                    ProductLine, ProductType, Procurement, StandardUnitOfMeasure,
                    SubProductFamily, StagedItem, Coated, GoldenStandard,
                    IsIntegrated, DateIntegrated, IntegratedBy,
                    CreatedDate, CreatedWindowsUser, ModifiedDate, ModifiedWindowsUser
                FROM dbo.isBOMImport_NewMakeItems
                ORDER BY ImportDate DESC, ItemCode";

            using var command = new SqlCommand(query, connection);
            using var reader  = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
                items.Add(MapRow(reader));
        }
        catch (Exception ex)
        {
            throw new Exception($"Error retrieving new make items: {ex.Message}", ex);
        }

        return items;
    }

    // -------------------------------------------------------------------------
    // GetByIdAsync – reads from isBOMImport_NewMakeItems
    // -------------------------------------------------------------------------

    public async Task<object?> GetByIdAsync(int id)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            const string query = @"
                SELECT
                    Id, ItemCode, ImportFileName, ImportDate, ItemDescription,
                    ProductLine, ProductType, Procurement, StandardUnitOfMeasure,
                    SubProductFamily, StagedItem, Coated, GoldenStandard,
                    IsIntegrated, DateIntegrated, IntegratedBy,
                    CreatedDate, CreatedWindowsUser, ModifiedDate, ModifiedWindowsUser
                FROM dbo.isBOMImport_NewMakeItems
                WHERE Id = @Id";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@Id", id);

            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
                return MapRow(reader);
        }
        catch (Exception ex)
        {
            throw new Exception($"Error retrieving new make item by ID: {ex.Message}", ex);
        }

        return null;
    }

    // -------------------------------------------------------------------------
    // AddAsync – insert a single item into isBOMImport_NewMakeItems
    // -------------------------------------------------------------------------

    public async Task<int> AddAsync(object newMakeItem)
    {
        if (newMakeItem is not NewMakeItem item)
            throw new ArgumentException("Invalid item type", nameof(newMakeItem));

        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            const string query = @"
                INSERT INTO dbo.isBOMImport_NewMakeItems
                (
                    ItemCode, ImportFileName, ImportDate, ItemDescription,
                    ProductLine, ProductType, Procurement, StandardUnitOfMeasure,
                    SubProductFamily, StagedItem, Coated, GoldenStandard,
                    IsIntegrated, CreatedDate, CreatedWindowsUser, ModifiedDate, ModifiedWindowsUser
                )
                VALUES
                (
                    @ItemCode, @ImportFileName, @ImportDate, @ItemDescription,
                    @ProductLine, @ProductType, @Procurement, @StandardUnitOfMeasure,
                    @SubProductFamily, @StagedItem, @Coated, @GoldenStandard,
                    0, GETDATE(), @WindowsUser, GETDATE(), @WindowsUser
                );
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@ItemCode",              item.ItemCode);
            command.Parameters.AddWithValue("@ImportFileName",        item.ImportFileName);
            command.Parameters.AddWithValue("@ImportDate",            item.ImportFileDate);
            command.Parameters.AddWithValue("@ItemDescription",       (object?)item.ItemDescription ?? DBNull.Value);
            command.Parameters.AddWithValue("@ProductLine",           (object?)item.ProductLine ?? DBNull.Value);
            command.Parameters.AddWithValue("@ProductType",           item.ProductType);
            command.Parameters.AddWithValue("@Procurement",           item.Procurement);
            command.Parameters.AddWithValue("@StandardUnitOfMeasure", item.StandardUnitOfMeasure);
            command.Parameters.AddWithValue("@SubProductFamily",      (object?)item.SubProductFamily ?? DBNull.Value);
            command.Parameters.AddWithValue("@StagedItem",            item.StagedItem);
            command.Parameters.AddWithValue("@Coated",                item.Coated);
            command.Parameters.AddWithValue("@GoldenStandard",        item.GoldenStandard);
            command.Parameters.AddWithValue("@WindowsUser",           Environment.UserName);

            var newId = await command.ExecuteScalarAsync();
            return Convert.ToInt32(newId);
        }
        catch (Exception ex)
        {
            throw new Exception($"Error adding new make item: {ex.Message}", ex);
        }
    }

    // -------------------------------------------------------------------------
    // UpdateAsync – update the editable fields in isBOMImport_NewMakeItems
    //              whenever the user edits an item in the Make Items view.
    // -------------------------------------------------------------------------

    public async Task UpdateAsync(object newMakeItem)
    {
        if (newMakeItem is not NewMakeItem item)
            throw new ArgumentException("Invalid item type", nameof(newMakeItem));

        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            const string query = @"
                UPDATE dbo.isBOMImport_NewMakeItems
                SET
                    ItemDescription       = @ItemDescription,
                    ProductLine           = @ProductLine,
                    ProductType           = @ProductType,
                    Procurement           = @Procurement,
                    StandardUnitOfMeasure = @StandardUnitOfMeasure,
                    SubProductFamily      = @SubProductFamily,
                    StagedItem            = @StagedItem,
                    Coated                = @Coated,
                    GoldenStandard        = @GoldenStandard,
                    ModifiedDate          = GETDATE(),
                    ModifiedWindowsUser   = @ModifiedWindowsUser
                WHERE ItemCode = @ItemCode";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@ItemCode",              item.ItemCode);
            command.Parameters.AddWithValue("@ItemDescription",       (object?)item.ItemDescription ?? DBNull.Value);
            command.Parameters.AddWithValue("@ProductLine",           (object?)item.ProductLine ?? DBNull.Value);
            command.Parameters.AddWithValue("@ProductType",           item.ProductType);
            command.Parameters.AddWithValue("@Procurement",           item.Procurement);
            command.Parameters.AddWithValue("@StandardUnitOfMeasure", item.StandardUnitOfMeasure);
            command.Parameters.AddWithValue("@SubProductFamily",      (object?)item.SubProductFamily ?? DBNull.Value);
            command.Parameters.AddWithValue("@StagedItem",            item.StagedItem);
            command.Parameters.AddWithValue("@Coated",                item.Coated);
            command.Parameters.AddWithValue("@GoldenStandard",        item.GoldenStandard);
            command.Parameters.AddWithValue("@ModifiedWindowsUser",   Environment.UserName);

            await command.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            throw new Exception($"Error updating new make item: {ex.Message}", ex);
        }
    }

    // -------------------------------------------------------------------------
    // DeleteAsync
    // -------------------------------------------------------------------------

    public async Task DeleteAsync(int id)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            const string query = "DELETE FROM dbo.isBOMImport_NewMakeItems WHERE Id = @Id";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@Id", id);

            await command.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            throw new Exception($"Error deleting new make item: {ex.Message}", ex);
        }
    }

    // -------------------------------------------------------------------------
    // GetByStatusAsync – kept for interface compatibility; returns all items
    //                    (the dedicated table has no separate status column)
    // -------------------------------------------------------------------------

    public async Task<IEnumerable<object>> GetByStatusAsync(int status)
    {
        return await GetAllAsync();
    }

    // -------------------------------------------------------------------------
    // BulkUpdateFieldAsync – update a single column for a set of item IDs
    // -------------------------------------------------------------------------

    public async Task BulkUpdateFieldAsync(IEnumerable<int> itemIds, string fieldName, object value)
    {
        var idList = itemIds.ToList();
        if (!idList.Any())
            return;

        // Map property names to database columns
        var columnName = fieldName switch
        {
            nameof(NewMakeItem.ItemDescription)       => "ItemDescription",
            nameof(NewMakeItem.ProductLine)           => "ProductLine",
            nameof(NewMakeItem.ProductType)           => "ProductType",
            nameof(NewMakeItem.Procurement)           => "Procurement",
            nameof(NewMakeItem.StandardUnitOfMeasure) => "StandardUnitOfMeasure",
            nameof(NewMakeItem.SubProductFamily)      => "SubProductFamily",
            nameof(NewMakeItem.StagedItem)            => "StagedItem",
            nameof(NewMakeItem.Coated)                => "Coated",
            nameof(NewMakeItem.GoldenStandard)        => "GoldenStandard",
            _ => throw new ArgumentException($"Bulk update not supported for field: {fieldName}")
        };

        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = $@"
                UPDATE dbo.isBOMImport_NewMakeItems
                SET {columnName}         = @Value,
                    ModifiedDate         = GETDATE(),
                    ModifiedWindowsUser  = @ModifiedWindowsUser
                WHERE Id IN ({string.Join(",", idList)})";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@Value",               value ?? DBNull.Value);
            command.Parameters.AddWithValue("@ModifiedWindowsUser", Environment.UserName);

            await command.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            throw new Exception($"Error bulk updating field {fieldName}: {ex.Message}", ex);
        }
    }

    // -------------------------------------------------------------------------
    // MarkAsIntegratedAsync – set integration details on isBOMImport_NewMakeItems
    //                         and update Status on isBOMImportBills
    // -------------------------------------------------------------------------

    public async Task MarkAsIntegratedAsync(string itemCode, string importFileName)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();

            try
            {
                // 1. Update the dedicated new make items table
                const string updateNewMakeItems = @"
                    UPDATE dbo.isBOMImport_NewMakeItems
                    SET IsIntegrated        = 1,
                        DateIntegrated      = GETDATE(),
                        IntegratedBy        = @IntegratedBy,
                        ModifiedDate        = GETDATE(),
                        ModifiedWindowsUser = @IntegratedBy
                    WHERE ItemCode = @ItemCode";

                using (var cmd = new SqlCommand(updateNewMakeItems, connection, transaction))
                {
                    cmd.Parameters.AddWithValue("@ItemCode",    itemCode);
                    cmd.Parameters.AddWithValue("@IntegratedBy", Environment.UserName);
                    await cmd.ExecuteNonQueryAsync();
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Error marking item as integrated: {ex.Message}", ex);
        }
    }

    // -------------------------------------------------------------------------
    // CopyFromBillsAsync – copy all new make items identified during the import
    //                      of @importFileName into isBOMImport_NewMakeItems
    //                      using the stored procedure isSp_CopyNewMakeItemsFromBills.
    //                      Only the first occurrence of each unique item code is
    //                      inserted; existing entries are left untouched.
    // -------------------------------------------------------------------------

    public async Task<int> CopyFromBillsAsync(string importFileName)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand("dbo.isSp_CopyNewMakeItemsFromBills", connection);
            command.CommandType = CommandType.StoredProcedure;
            
            command.Parameters.AddWithValue("@ImportFileName", importFileName);
            command.Parameters.AddWithValue("@WindowsUser",    Environment.UserName);

            return await command.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            throw new Exception($"Error copying new make items from bills: {ex.Message}", ex);
        }
    }
}
