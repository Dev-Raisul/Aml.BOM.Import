using Aml.BOM.Import.Domain.Entities;
using Aml.BOM.Import.Shared.Interfaces;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Aml.BOM.Import.Infrastructure.Repositories;

public class BomImportBillRepository : IBomImportBillRepository
{
    private readonly string _connectionString;
    private readonly ILoggerService _logger;

    public BomImportBillRepository(string connectionString, ILoggerService logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    public async Task<int> CreateAsync(BomImportBill bill)
    {
        _logger.LogInformation("Creating BOM import bill for file: {0}, Tab: {1}", bill.ImportFileName, bill.TabName);

        const string sql = @"
            INSERT INTO isBOMImportBills 
            (ImportFileName, ImportDate, ImportWindowsUser, TabName, Status, 
             ParentItemCode, ParentDescription, BOMLevel, BOMNumber,
             LineNumber, ComponentItemCode, ComponentDescription, Quantity, UnitOfMeasure, 
             ItemExists, ValidationMessage, 
             ProductLine, ProductType, ProcurementType, SubProductFamily, StagedItem, Coated, GoldenStandard,
             CreatedDate, ModifiedDate)
            VALUES 
            (@ImportFileName, @ImportDate, @ImportWindowsUser, @TabName, @Status,
             @ParentItemCode, @ParentDescription, @BOMLevel, @BOMNumber,
             @LineNumber, @ComponentItemCode, @ComponentDescription, @Quantity, @UnitOfMeasure,
             @ItemExists, @ValidationMessage,
             @ProductLine, @ProductType, @ProcurementType, @SubProductFamily, @StagedItem, @Coated, @GoldenStandard,
             @CreatedDate, @ModifiedDate);
            SELECT CAST(SCOPE_IDENTITY() as int);";

        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(sql, connection);
            AddBillParameters(command, bill);

            var id = (int)(await command.ExecuteScalarAsync() ?? 0);
            
            _logger.LogInformation("BOM import bill created successfully. Id: {0}", id);
            return id;
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to create BOM import bill", ex);
            throw;
        }
    }

    public async Task<int> CreateBatchAsync(IEnumerable<BomImportBill> bills)
    {
        var billsList = bills.ToList();
        _logger.LogInformation("Creating batch of {0} BOM import bills", billsList.Count);

        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();
            
            try
            {
                int count = 0;
                foreach (var bill in billsList)
                {
                    const string sql = @"
                        INSERT INTO isBOMImportBills 
                        (ImportFileName, ImportDate, ImportWindowsUser, TabName, Status, 
                         ParentItemCode, ParentDescription, BOMLevel, BOMNumber,
                         LineNumber, ComponentItemCode, ComponentDescription, Quantity, UnitOfMeasure, 
                         ItemExists, ValidationMessage,
                         ProductLine, ProductType, ProcurementType, SubProductFamily, StagedItem, Coated, GoldenStandard,
                         CreatedDate, ModifiedDate)
                        VALUES 
                        (@ImportFileName, @ImportDate, @ImportWindowsUser, @TabName, @Status,
                         @ParentItemCode, @ParentDescription, @BOMLevel, @BOMNumber,
                         @LineNumber, @ComponentItemCode, @ComponentDescription, @Quantity, @UnitOfMeasure,
                         @ItemExists, @ValidationMessage,
                         @ProductLine, @ProductType, @ProcurementType, @SubProductFamily, @StagedItem, @Coated, @GoldenStandard,
                         @CreatedDate, @ModifiedDate)";

                    using var command = new SqlCommand(sql, connection, transaction);
                    AddBillParameters(command, bill);
                    await command.ExecuteNonQueryAsync();
                    count++;
                }

                transaction.Commit();
                _logger.LogInformation("Successfully created {0} BOM import bills", count);
                return count;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to create batch of BOM import bills", ex);
            throw;
        }
    }

    public async Task<BomImportBill?> GetByIdAsync(int id)
    {
        _logger.LogDebug("Retrieving BOM import bill by Id: {0}", id);

        const string sql = "SELECT * FROM isBOMImportBills WHERE Id = @Id";

        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", id);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapFromReader(reader);
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to retrieve BOM import bill by Id: {0}", ex, id);
            throw;
        }
    }

    public async Task<IEnumerable<BomImportBill>> GetAllAsync()
    {
        _logger.LogDebug("Retrieving all BOM import bills");

        const string sql = "SELECT * FROM isBOMImportBills ORDER BY ImportDate DESC, LineNumber";

        return await ExecuteQueryAsync(sql);
    }

    public async Task<IEnumerable<BomImportBill>> GetByFileNameAsync(string fileName)
    {
        _logger.LogDebug("Retrieving BOM import bills by FileName: {0}", fileName);

        const string sql = "SELECT * FROM isBOMImportBills WHERE ImportFileName = @FileName ORDER BY TabName, LineNumber";

        return await ExecuteQueryAsync(sql, cmd => cmd.Parameters.AddWithValue("@FileName", fileName));
    }

    public async Task<IEnumerable<BomImportBill>> GetByStatusAsync(string status)
    {
        _logger.LogDebug("Retrieving BOM import bills by Status: {0}", status);

        const string sql = "SELECT * FROM isBOMImportBills WHERE Status = @Status ORDER BY ImportDate DESC";

        return await ExecuteQueryAsync(sql, cmd => cmd.Parameters.AddWithValue("@Status", status));
    }

    public async Task<IEnumerable<BomImportBill>> GetByTabNameAsync(string tabName)
    {
        _logger.LogDebug("Retrieving BOM import bills by TabName: {0}", tabName);

        const string sql = "SELECT * FROM isBOMImportBills WHERE TabName = @TabName ORDER BY ImportDate DESC, LineNumber";

        return await ExecuteQueryAsync(sql, cmd => cmd.Parameters.AddWithValue("@TabName", tabName));
    }

    public async Task<IEnumerable<BomImportBill>> GetByFileAndTabAsync(string fileName, string tabName)
    {
        _logger.LogDebug("Retrieving BOM import bills by FileName: {0}, TabName: {1}", fileName, tabName);

        const string sql = @"
            SELECT * FROM isBOMImportBills 
            WHERE ImportFileName = @FileName AND TabName = @TabName 
            ORDER BY LineNumber";

        return await ExecuteQueryAsync(sql, cmd =>
        {
            cmd.Parameters.AddWithValue("@FileName", fileName);
            cmd.Parameters.AddWithValue("@TabName", tabName);
        });
    }

    public async Task<IEnumerable<BomImportBill>> GetByComponentItemCodeAsync(string itemCode)
    {
        _logger.LogDebug("Retrieving BOM import bills by ComponentItemCode: {0}", itemCode);

        const string sql = "SELECT * FROM isBOMImportBills WHERE ComponentItemCode = @ItemCode ORDER BY ImportDate DESC";

        return await ExecuteQueryAsync(sql, cmd => cmd.Parameters.AddWithValue("@ItemCode", itemCode));
    }

    public async Task<IEnumerable<BomImportBill>> GetByParentItemCodeAsync(string itemCode)
    {
        _logger.LogDebug("Retrieving BOM import bills by ParentItemCode: {0}", itemCode);

        const string sql = "SELECT * FROM isBOMImportBills WHERE ParentItemCode = @ItemCode ORDER BY ImportDate DESC, LineNumber";

        return await ExecuteQueryAsync(sql, cmd => cmd.Parameters.AddWithValue("@ItemCode", itemCode));
    }

    public async Task<IEnumerable<BomImportBill>> GetRecentAsync(int count = 100)
    {
        _logger.LogDebug("Retrieving recent {0} BOM import bills", count);

        string sql = $"SELECT TOP {count} * FROM isBOMImportBills ORDER BY ImportDate DESC, LineNumber";

        return await ExecuteQueryAsync(sql);
    }

    public async Task UpdateAsync(BomImportBill bill)
    {
        _logger.LogInformation("Updating BOM import bill. Id: {0}", bill.Id);

        const string sql = @"
            UPDATE isBOMImportBills
            SET ImportFileName = @ImportFileName,
                ImportDate = @ImportDate,
                ImportWindowsUser = @ImportWindowsUser,
                TabName = @TabName,
                Status = @Status,
                DateValidated = @DateValidated,
                DateIntegrated = @DateIntegrated,
                ParentItemCode = @ParentItemCode,
                ParentDescription = @ParentDescription,
                BOMLevel = @BOMLevel,
                BOMNumber = @BOMNumber,
                LineNumber = @LineNumber,
                ComponentItemCode = @ComponentItemCode,
                ComponentDescription = @ComponentDescription,
                Quantity = @Quantity,
                UnitOfMeasure = @UnitOfMeasure,
                ItemExists = @ItemExists,
                ValidationMessage = @ValidationMessage,
                ProductLine = @ProductLine,
                ProductType = @ProductType,
                ProcurementType = @ProcurementType,
                SubProductFamily = @SubProductFamily,
                StagedItem = @StagedItem,
                Coated = @Coated,
                GoldenStandard = @GoldenStandard,
                ModifiedDate = @ModifiedDate
            WHERE Id = @Id";

        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", bill.Id);
            AddBillParameters(command, bill);

            await command.ExecuteNonQueryAsync();
            _logger.LogInformation("BOM import bill updated successfully. Id: {0}", bill.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to update BOM import bill. Id: {0}", ex, bill.Id);
            throw;
        }
    }

    public async Task UpdateStatusAsync(int id, string status, DateTime? validatedDate = null, DateTime? integratedDate = null)
    {
        _logger.LogInformation("Updating BOM import bill status. Id: {0}, Status: {1}", id, status);

        const string sql = @"
            UPDATE isBOMImportBills
            SET Status = @Status,
                DateValidated = @DateValidated,
                DateIntegrated = @DateIntegrated,
                ModifiedDate = @ModifiedDate
            WHERE Id = @Id";

        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", id);
            command.Parameters.AddWithValue("@Status", status);
            command.Parameters.AddWithValue("@DateValidated", (object?)validatedDate ?? DBNull.Value);
            command.Parameters.AddWithValue("@DateIntegrated", (object?)integratedDate ?? DBNull.Value);
            command.Parameters.AddWithValue("@ModifiedDate", DateTime.Now);

            await command.ExecuteNonQueryAsync();
            _logger.LogInformation("BOM import bill status updated. Id: {0}", id);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to update BOM import bill status. Id: {0}", ex, id);
            throw;
        }
    }

    public async Task UpdateValidationAsync(int id, bool itemExists, string? itemType, string? validationMessage)
    {
        _logger.LogDebug("Updating BOM import bill validation. Id: {0}", id);

        const string sql = @"
            UPDATE isBOMImportBills
            SET ItemExists = @ItemExists,
                ValidationMessage = @ValidationMessage,
                ModifiedDate = @ModifiedDate
            WHERE Id = @Id";

        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", id);
            command.Parameters.AddWithValue("@ItemExists", itemExists);
            command.Parameters.AddWithValue("@ValidationMessage", (object?)validationMessage ?? DBNull.Value);
            command.Parameters.AddWithValue("@ModifiedDate", DateTime.Now);

            await command.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to update BOM import bill validation. Id: {0}", ex, id);
            throw;
        }
    }

    public async Task<int> GetPendingParentItemCountAsync()
    {
        _logger.LogDebug("Getting pending parent item count");

        const string sql = @"
            SELECT COUNT(DISTINCT ItemCode)
            FROM (
                -- Parent items (items that have a parent code)
                SELECT DISTINCT ParentItemCode AS ItemCode
                FROM isBOMImportBills
                WHERE Status NOT IN ('Integrated', 'Duplicate', 'Ready')
                  AND ParentItemCode IS NOT NULL
                
                UNION
                
                -- Standalone parent items (component items without a parent)
                SELECT DISTINCT ComponentItemCode AS ItemCode
                FROM isBOMImportBills
                WHERE Status NOT IN ('Integrated', 'Duplicate', 'Ready')
                  AND ParentItemCode IS NULL
            ) AS AllParents";

        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(sql, connection);

            return (int)(await command.ExecuteScalarAsync() ?? 0);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to get pending parent item count", ex);
            throw;
        }
    }

    public async Task<int> GetValidatedParentItemCountAsync()
    {
        _logger.LogDebug("Getting validated parent item count (parents with ALL components validated)");

        const string sql = @"
            -- Count distinct parent items where parent is validated and ALL components are validated
            SELECT COUNT(DISTINCT ib.ComponentItemCode)
            FROM isBOMImportBills ib
            WHERE ib.ParentItemCode IS NULL
              AND ib.Status = 'Validated'
              -- Check that ALL components for this parent are validated
              AND NOT EXISTS (
                  SELECT 1
                  FROM isBOMImportBills components
                  WHERE components.ParentItemCode = ib.ComponentItemCode
                    AND components.Status != 'Validated'
              )";

        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(sql, connection);

            return (int)(await command.ExecuteScalarAsync() ?? 0);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to get validated parent item count", ex);
            throw;
        }
    }

    public async Task UpdateBatchStatusAsync(IEnumerable<int> ids, string status)
    {
        var idsList = ids.ToList();
        _logger.LogInformation("Updating batch status for {0} BOM import bills to: {1}", idsList.Count, status);

        const string sql = @"
            UPDATE isBOMImportBills
            SET Status = @Status, ModifiedDate = @ModifiedDate
            WHERE Id = @Id";

        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();
            
            try
            {
                foreach (var id in idsList)
                {
                    using var command = new SqlCommand(sql, connection, transaction);
                    command.Parameters.AddWithValue("@Id", id);
                    command.Parameters.AddWithValue("@Status", status);
                    command.Parameters.AddWithValue("@ModifiedDate", DateTime.Now);
                    await command.ExecuteNonQueryAsync();
                }

                transaction.Commit();
                _logger.LogInformation("Successfully updated {0} BOM import bills", idsList.Count);
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to update batch status", ex);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(int id)
    {
        _logger.LogInformation("Deleting BOM import bill. Id: {0}", id);

        const string sql = "DELETE FROM isBOMImportBills WHERE Id = @Id";

        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", id);

            var rowsAffected = await command.ExecuteNonQueryAsync();
            var deleted = rowsAffected > 0;

            if (deleted)
            {
                _logger.LogInformation("BOM import bill deleted. Id: {0}", id);
            }

            return deleted;
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to delete BOM import bill. Id: {0}", ex, id);
            throw;
        }
    }

    public async Task<int> DeleteByFileNameAsync(string fileName)
    {
        _logger.LogInformation("Deleting BOM import bills by FileName: {0}", fileName);

        const string sql = "DELETE FROM isBOMImportBills WHERE ImportFileName = @FileName";

        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@FileName", fileName);

            var rowsAffected = await command.ExecuteNonQueryAsync();
            _logger.LogInformation("Deleted {0} BOM import bills for file: {1}", rowsAffected, fileName);
            return rowsAffected;
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to delete BOM import bills by FileName: {0}", ex, fileName);
            throw;
        }
    }

    public async Task<int> GetCountByStatusAsync(string status)
    {
        const string sql = "SELECT COUNT(*) FROM isBOMImportBills WHERE Status = @Status";

        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Status", status);

            return (int)(await command.ExecuteScalarAsync() ?? 0);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to get count by status: {0}", ex, status);
            throw;
        }
    }

    public async Task<int> GetCountByFileNameAsync(string fileName)
    {
        const string sql = "SELECT COUNT(*) FROM isBOMImportBills WHERE ImportFileName = @FileName";

        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@FileName", fileName);

            return (int)(await command.ExecuteScalarAsync() ?? 0);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to get count by filename: {0}", ex, fileName);
            throw;
        }
    }

    public async Task<Dictionary<string, int>> GetStatusSummaryAsync()
    {
        const string sql = "SELECT Status, COUNT(*) as Count FROM isBOMImportBills GROUP BY Status";

        var summary = new Dictionary<string, int>();

        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(sql, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var status = reader.GetString(0);
                var count = reader.GetInt32(1);
                summary[status] = count;
            }

            return summary;
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to get status summary", ex);
            throw;
        }
    }

    public async Task<int> GetParentItemCountByReadyStatus()
    {
        var status = "Ready";
        _logger.LogDebug("Getting parent item count for status: {0} (only parents where parent AND all components have this status)", status);

        const string sql = @"
            -- Count parents where the parent itself AND ALL its components have the specified status
            SELECT COUNT(DISTINCT ib.ComponentItemCode)
            FROM isBOMImportBills ib
            WHERE ib.ParentItemCode IS NULL
              AND ib.Status = @Status
              -- Check that ALL components for this parent also have the same status
              AND NOT EXISTS (
                  SELECT 1
                  FROM isBOMImportBills components
                  WHERE components.ParentItemCode = ib.ComponentItemCode
                    AND components.Status != @Status
              )";

        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Status", status);

            return (int)(await command.ExecuteScalarAsync() ?? 0);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to get parent item count by status: {0}", ex, status);
            throw;
        }
    }

    public async Task<int> GetParentItemCountByStatusAsync(string status)
    {
        _logger.LogDebug("Getting parent item count for status: {0}", status);

        const string sql = @"
            SELECT COUNT(DISTINCT ItemCode)
            FROM (
                -- Parent items (items that have a parent code)
                SELECT DISTINCT ParentItemCode AS ItemCode
                FROM isBOMImportBills
                WHERE Status = @Status
                  AND ParentItemCode IS NOT NULL
                
                UNION
                
                -- Standalone parent items (component items without a parent)
                SELECT DISTINCT ComponentItemCode AS ItemCode
                FROM isBOMImportBills
                WHERE Status = @Status
                  AND ParentItemCode IS NULL
            ) AS AllParents";

        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Status", status);

            return (int)(await command.ExecuteScalarAsync() ?? 0);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to get parent item count by status: {0}", ex, status);
            throw;
        }
    }

    public async Task<int> GetReadyToIntegrateRecordCountAsync()
    {
        _logger.LogDebug("Getting ready to integrate record count (parents + components where ALL are validated)");

        const string sql = @"
            -- Count all records (parent + components) for fully validated BOMs
            SELECT COUNT(*)
            FROM isBOMImportBills ib
            WHERE 
                -- Case 1: Parent item itself (has no ParentItemCode and is validated)
                (ib.ParentItemCode IS NULL AND ib.Status = 'Validated'
                 -- Check if ALL components for this parent are validated
                 AND NOT EXISTS (
                     SELECT 1
                     FROM isBOMImportBills components
                     WHERE components.ParentItemCode = ib.ComponentItemCode
                       AND components.Status != 'Validated'
                 )
                )
                OR
                -- Case 2: Component items of validated parents
                (ib.ParentItemCode IS NOT NULL AND ib.Status = 'Validated'
                 -- Parent must be validated
                 AND EXISTS (
                     SELECT 1
                     FROM isBOMImportBills parent
                     WHERE parent.ComponentItemCode = ib.ParentItemCode
                       AND parent.ParentItemCode IS NULL
                       AND parent.Status = 'Validated'
                 )
                 -- ALL siblings (other components of same parent) must be validated
                 AND NOT EXISTS (
                     SELECT 1
                     FROM isBOMImportBills siblings
                     WHERE siblings.ParentItemCode = ib.ParentItemCode
                       AND siblings.Status != 'Validated'
                 )
                )";

        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(sql, connection);

            return (int)(await command.ExecuteScalarAsync() ?? 0);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to get ready to integrate record count", ex);
            throw;
        }
    }

    // Helper methods
    private void AddBillParameters(SqlCommand command, BomImportBill bill)
    {
        command.Parameters.AddWithValue("@ImportFileName", bill.ImportFileName);
        command.Parameters.AddWithValue("@ImportDate", bill.ImportDate);
        command.Parameters.AddWithValue("@ImportWindowsUser", bill.ImportWindowsUser);
        command.Parameters.AddWithValue("@TabName", bill.TabName);
        command.Parameters.AddWithValue("@Status", bill.Status);
        command.Parameters.AddWithValue("@DateValidated", (object?)bill.DateValidated ?? DBNull.Value);
        command.Parameters.AddWithValue("@DateIntegrated", (object?)bill.DateIntegrated ?? DBNull.Value);
        command.Parameters.AddWithValue("@ParentItemCode", (object?)bill.ParentItemCode ?? DBNull.Value);
        command.Parameters.AddWithValue("@ParentDescription", (object?)bill.ParentDescription ?? DBNull.Value);
        command.Parameters.AddWithValue("@BOMLevel", (object?)bill.BOMLevel ?? DBNull.Value);
        command.Parameters.AddWithValue("@BOMNumber", (object?)bill.BOMNumber ?? DBNull.Value);
        command.Parameters.AddWithValue("@LineNumber", bill.LineNumber);
        command.Parameters.AddWithValue("@ComponentItemCode", bill.ComponentItemCode);
        command.Parameters.AddWithValue("@ComponentDescription", (object?)bill.ComponentDescription ?? DBNull.Value);
        command.Parameters.AddWithValue("@Quantity", bill.Quantity);
        command.Parameters.AddWithValue("@UnitOfMeasure", (object?)bill.UnitOfMeasure ?? DBNull.Value);
        command.Parameters.AddWithValue("@ItemExists", bill.ItemExists);
        command.Parameters.AddWithValue("@ValidationMessage", (object?)bill.ValidationMessage ?? DBNull.Value);
        command.Parameters.AddWithValue("@ProductLine", (object?)bill.ProductLine ?? DBNull.Value);
        command.Parameters.AddWithValue("@ProductType", (object?)bill.ProductType ?? DBNull.Value);
        command.Parameters.AddWithValue("@ProcurementType", (object?)bill.ProcurementType ?? DBNull.Value);
        command.Parameters.AddWithValue("@SubProductFamily", (object?)bill.SubProductFamily ?? DBNull.Value);
        command.Parameters.AddWithValue("@StagedItem", (object?)bill.StagedItem ?? DBNull.Value);
        command.Parameters.AddWithValue("@Coated", (object?)bill.Coated ?? DBNull.Value);
        command.Parameters.AddWithValue("@GoldenStandard", (object?)bill.GoldenStandard ?? DBNull.Value);
        command.Parameters.AddWithValue("@CreatedDate", bill.CreatedDate == default ? DateTime.Now : bill.CreatedDate);
        command.Parameters.AddWithValue("@ModifiedDate", DateTime.Now);
    }

    private async Task<IEnumerable<BomImportBill>> ExecuteQueryAsync(string sql, Action<SqlCommand>? parameterAction = null)
    {
        var bills = new List<BomImportBill>();

        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(sql, connection);
            parameterAction?.Invoke(command);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                bills.Add(MapFromReader(reader));
            }

            return bills;
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to execute query", ex);
            throw;
        }
    }

    private BomImportBill MapFromReader(SqlDataReader reader)
    {
        return new BomImportBill
        {
            Id = reader.GetInt32(reader.GetOrdinal("Id")),
            ImportFileName = reader.GetString(reader.GetOrdinal("ImportFileName")),
            ImportDate = reader.GetDateTime(reader.GetOrdinal("ImportDate")),
            ImportWindowsUser = reader.GetString(reader.GetOrdinal("ImportWindowsUser")),
            TabName = reader.GetString(reader.GetOrdinal("TabName")),
            Status = reader.GetString(reader.GetOrdinal("Status")),
            DateValidated = reader.IsDBNull(reader.GetOrdinal("DateValidated")) ? null : reader.GetDateTime(reader.GetOrdinal("DateValidated")),
            DateIntegrated = reader.IsDBNull(reader.GetOrdinal("DateIntegrated")) ? null : reader.GetDateTime(reader.GetOrdinal("DateIntegrated")),
            ParentItemCode = reader.IsDBNull(reader.GetOrdinal("ParentItemCode")) ? null : reader.GetString(reader.GetOrdinal("ParentItemCode")),
            ParentDescription = reader.IsDBNull(reader.GetOrdinal("ParentDescription")) ? null : reader.GetString(reader.GetOrdinal("ParentDescription")),
            BOMLevel = reader.IsDBNull(reader.GetOrdinal("BOMLevel")) ? null : reader.GetString(reader.GetOrdinal("BOMLevel")),
            BOMNumber = reader.IsDBNull(reader.GetOrdinal("BOMNumber")) ? null : reader.GetString(reader.GetOrdinal("BOMNumber")),
            LineNumber = reader.GetInt32(reader.GetOrdinal("LineNumber")),
            ComponentItemCode = reader.GetString(reader.GetOrdinal("ComponentItemCode")),
            ComponentDescription = reader.IsDBNull(reader.GetOrdinal("ComponentDescription")) ? null : reader.GetString(reader.GetOrdinal("ComponentDescription")),
            Quantity = reader.GetDecimal(reader.GetOrdinal("Quantity")),
            UnitOfMeasure = reader.IsDBNull(reader.GetOrdinal("UnitOfMeasure")) ? null : reader.GetString(reader.GetOrdinal("UnitOfMeasure")),
            ItemExists = reader.GetBoolean(reader.GetOrdinal("ItemExists")),
            ValidationMessage = reader.IsDBNull(reader.GetOrdinal("ValidationMessage")) ? null : reader.GetString(reader.GetOrdinal("ValidationMessage")),
            ProductLine = reader.IsDBNull(reader.GetOrdinal("ProductLine")) ? null : reader.GetString(reader.GetOrdinal("ProductLine")),
            ProductType = reader.IsDBNull(reader.GetOrdinal("ProductType")) ? null : reader.GetString(reader.GetOrdinal("ProductType")),
            ProcurementType = reader.IsDBNull(reader.GetOrdinal("ProcurementType")) ? null : reader.GetString(reader.GetOrdinal("ProcurementType")),
            SubProductFamily = reader.IsDBNull(reader.GetOrdinal("SubProductFamily")) ? null : reader.GetString(reader.GetOrdinal("SubProductFamily")),
            StagedItem = reader.IsDBNull(reader.GetOrdinal("StagedItem")) ? null : reader.GetBoolean(reader.GetOrdinal("StagedItem")),
            Coated = reader.IsDBNull(reader.GetOrdinal("Coated")) ? null : reader.GetBoolean(reader.GetOrdinal("Coated")),
            GoldenStandard = reader.IsDBNull(reader.GetOrdinal("GoldenStandard")) ? null : reader.GetBoolean(reader.GetOrdinal("GoldenStandard")),
            CreatedDate = reader.GetDateTime(reader.GetOrdinal("CreatedDate")),
            ModifiedDate = reader.GetDateTime(reader.GetOrdinal("ModifiedDate"))
        };
    }
}
