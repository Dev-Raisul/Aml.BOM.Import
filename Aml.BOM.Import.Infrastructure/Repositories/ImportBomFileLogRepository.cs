using Aml.BOM.Import.Domain.Entities;
using Aml.BOM.Import.Shared.Interfaces;
using Microsoft.Data.SqlClient;

namespace Aml.BOM.Import.Infrastructure.Repositories;

public class ImportBomFileLogRepository : IImportBomFileLogRepository
{
    private readonly string _connectionString;
    private readonly ILoggerService _logger;

    public ImportBomFileLogRepository(string connectionString, ILoggerService logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    public async Task<int> CreateAsync(ImportBomFileLog fileLog)
    {
        _logger.LogInformation("Creating new BOM file import log entry for file: {0}", fileLog.FileName);

        const string sql = @"
            INSERT INTO isBOMImportFileLog (FileName, UploadDate)
            VALUES (@FileName, @UploadDate);
            SELECT CAST(SCOPE_IDENTITY() as int);";

        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@FileName", fileLog.FileName);
            command.Parameters.AddWithValue("@UploadDate", fileLog.UploadDate);

            var fileId = (int)(await command.ExecuteScalarAsync() ?? 0);
            
            _logger.LogInformation("BOM file import log created successfully. FileId: {0}", fileId);
            return fileId;
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to create BOM file import log for file: {0}", ex, fileLog.FileName);
            throw;
        }
    }

    public async Task<ImportBomFileLog?> GetByIdAsync(int fileId)
    {
        _logger.LogDebug("Retrieving BOM file import log by FileId: {0}", fileId);

        const string sql = @"
            SELECT FileId, FileName, UploadDate
            FROM isBOMImportFileLog
            WHERE FileId = @FileId";

        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@FileId", fileId);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapFromReader(reader);
            }

            _logger.LogWarning("BOM file import log not found for FileId: {0}", fileId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to retrieve BOM file import log by FileId: {0}", ex, fileId);
            throw;
        }
    }

    public async Task<IEnumerable<ImportBomFileLog>> GetAllAsync()
    {
        _logger.LogDebug("Retrieving all BOM file import logs");

        const string sql = @"
            SELECT FileId, FileName, UploadDate
            FROM isBOMImportFileLog
            ORDER BY UploadDate DESC";

        var logs = new List<ImportBomFileLog>();

        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(sql, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                logs.Add(MapFromReader(reader));
            }

            _logger.LogInformation("Retrieved {0} BOM file import logs", logs.Count);
            return logs;
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to retrieve all BOM file import logs", ex);
            throw;
        }
    }

    public async Task<IEnumerable<ImportBomFileLog>> GetRecentAsync(int count = 50)
    {
        _logger.LogDebug("Retrieving recent {0} BOM file import logs", count);

        string sql = $@"
            SELECT TOP {count} FileId, FileName, UploadDate
            FROM isBOMImportFileLog
            ORDER BY UploadDate DESC";

        var logs = new List<ImportBomFileLog>();

        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(sql, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                logs.Add(MapFromReader(reader));
            }

            _logger.LogInformation("Retrieved {0} recent BOM file import logs", logs.Count);
            return logs;
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to retrieve recent BOM file import logs", ex);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(int fileId)
    {
        _logger.LogInformation("Deleting BOM file import log. FileId: {0}", fileId);

        const string sql = "DELETE FROM isBOMImportFileLog WHERE FileId = @FileId";

        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@FileId", fileId);

            var rowsAffected = await command.ExecuteNonQueryAsync();
            var deleted = rowsAffected > 0;

            if (deleted)
            {
                _logger.LogInformation("BOM file import log deleted successfully. FileId: {0}", fileId);
            }
            else
            {
                _logger.LogWarning("BOM file import log not found for deletion. FileId: {0}", fileId);
            }

            return deleted;
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to delete BOM file import log. FileId: {0}", ex, fileId);
            throw;
        }
    }

    private ImportBomFileLog MapFromReader(SqlDataReader reader)
    {
        return new ImportBomFileLog
        {
            FileId = reader.GetInt32(reader.GetOrdinal("FileId")),
            FileName = reader.GetString(reader.GetOrdinal("FileName")),
            UploadDate = reader.GetDateTime(reader.GetOrdinal("UploadDate"))
        };
    }
}
