using Aml.BOM.Import.Shared.Interfaces;
using Microsoft.Data.SqlClient;

namespace Aml.BOM.Import.Infrastructure.Repositories;

public class BomImportRepository : IBomImportRepository
{
    private readonly string _connectionString;
    private readonly ILoggerService _logger;

    public BomImportRepository(string connectionString, ILoggerService logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    public async Task<IEnumerable<object>> GetAllAsync()
    {
        _logger.LogDebug("Retrieving all parent BOMs with 'Ready' status");

        const string sql = @"
            SELECT DISTINCT
                ib.ComponentItemCode AS ItemCode,
                COALESCE(ci.ItemCodeDesc, ib.ComponentItemCode) AS Description,
                ib.ImportFileName,
                ib.ImportDate,
                ib.ImportWindowsUser AS ImportedBy,
                ib.Status,
                (SELECT COUNT(*) 
                 FROM isBOMImportBills components 
                 WHERE components.ParentItemCode = ib.ComponentItemCode 
                   AND components.Status = 'Ready') AS ComponentCount
            FROM isBOMImportBills ib
            LEFT JOIN CI_Item ci ON ib.ComponentItemCode = ci.ItemCode
            WHERE ib.ParentItemCode IS NULL
              AND ib.Status = 'Ready'
            ORDER BY ib.ImportDate DESC, ib.ComponentItemCode";

        var boms = new List<object>();

        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(sql, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                boms.Add(new
                {
                    ItemCode = reader.GetString(reader.GetOrdinal("ItemCode")),
                    Description = reader.IsDBNull(reader.GetOrdinal("Description")) 
                        ? string.Empty 
                        : reader.GetString(reader.GetOrdinal("Description")),
                    ImportFileName = reader.GetString(reader.GetOrdinal("ImportFileName")),
                    ImportDate = reader.GetDateTime(reader.GetOrdinal("ImportDate")),
                    ImportedBy = reader.GetString(reader.GetOrdinal("ImportedBy")),
                    Status = reader.GetString(reader.GetOrdinal("Status")),
                    ComponentCount = reader.GetInt32(reader.GetOrdinal("ComponentCount"))
                });
            }

            _logger.LogInformation("Retrieved {0} BOMs with 'Ready' status", boms.Count);
            return boms;
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to retrieve Ready BOMs", ex);
            throw;
        }
    }

    public async Task<IEnumerable<object>> GetIntegratedBomsAsync()
    {
        _logger.LogDebug("Retrieving integrated BOMs (parent items with Status='Integrated')");

        const string sql = @"
            SELECT DISTINCT
                ib.ParentItemCode AS ItemCode,
                COALESCE(ci.ItemCodeDesc, ib.ParentDescription) AS Description,
                MIN(ib.ImportFileName) AS ImportFileName,
                MIN(ib.ImportDate) AS ImportDate,
                MIN(ib.DateIntegrated) AS IntegratedDate,
                MIN(ib.ImportWindowsUser) AS ImportedBy,
                'Integrated' AS Status,
                COUNT(*) AS ComponentCount
            FROM isBOMImportBills ib
            LEFT JOIN CI_Item ci ON ib.ParentItemCode = ci.ItemCode
            WHERE ib.ParentItemCode IS NOT NULL
              AND ib.Status = 'Integrated'
            GROUP BY ib.ParentItemCode, COALESCE(ci.ItemCodeDesc, ib.ParentDescription)
            ORDER BY MIN(ib.DateIntegrated) DESC, ib.ParentItemCode";

        var boms = new List<object>();

        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(sql, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                boms.Add(new
                {
                    ItemCode = reader.GetString(reader.GetOrdinal("ItemCode")),
                    Description = reader.IsDBNull(reader.GetOrdinal("Description")) 
                        ? string.Empty 
                        : reader.GetString(reader.GetOrdinal("Description")),
                    ImportFileName = reader.GetString(reader.GetOrdinal("ImportFileName")),
                    ImportDate = reader.GetDateTime(reader.GetOrdinal("ImportDate")),
                    IntegratedDate = reader.IsDBNull(reader.GetOrdinal("IntegratedDate"))
                        ? (DateTime?)null
                        : reader.GetDateTime(reader.GetOrdinal("IntegratedDate")),
                    ImportedBy = reader.GetString(reader.GetOrdinal("ImportedBy")),
                    Status = reader.GetString(reader.GetOrdinal("Status")),
                    ComponentCount = reader.GetInt32(reader.GetOrdinal("ComponentCount"))
                });
            }

            _logger.LogInformation("Retrieved {0} integrated BOMs", boms.Count);
            return boms;
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to retrieve integrated BOMs", ex);
            throw;
        }
    }

    public async Task<object?> GetByIdAsync(int id)
    {
        // TODO: Implement SQL query to retrieve BOM import record by ID
        await Task.CompletedTask;
        return null;
    }

    public async Task<int> AddAsync(object bomImportRecord)
    {
        // TODO: Implement SQL insert for BOM import record
        await Task.CompletedTask;
        return 0;
    }

    public async Task UpdateAsync(object bomImportRecord)
    {
        // TODO: Implement SQL update for BOM import record
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(int id)
    {
        // TODO: Implement SQL delete for BOM import record
        await Task.CompletedTask;
    }

    public async Task<IEnumerable<object>> GetByStatusAsync(int status)
    {
        // TODO: Implement SQL query to retrieve BOM import records by status
        await Task.CompletedTask;
        return new List<object>();
    }

    public async Task<bool> IsDuplicateAsync(string bomNumber)
    {
        // TODO: Implement SQL query to check for duplicate BOM numbers
        await Task.CompletedTask;
        return false;
    }
}
