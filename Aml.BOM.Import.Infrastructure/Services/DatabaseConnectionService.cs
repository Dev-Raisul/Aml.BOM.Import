using Aml.BOM.Import.Shared.Interfaces;
using Microsoft.Data.SqlClient;

namespace Aml.BOM.Import.Infrastructure.Services;

public class DatabaseConnectionService : IDatabaseConnectionService
{
    private readonly ILoggerService _logger;

    public DatabaseConnectionService(ILoggerService logger)
    {
        _logger = logger;
    }

    public string BuildConnectionString(string server, string database, string username, string password)
    {
        _logger.LogDebug("Building connection string for Server={0}, Database={1}", server, database);
        
        var builder = new SqlConnectionStringBuilder
        {
            DataSource = server,
            InitialCatalog = database,
            UserID = username,
            Password = password,
            TrustServerCertificate = true,
            ConnectTimeout = 10
        };

        _logger.LogInformation("Connection string built successfully for Server={0}, Database={1}", server, database);
        return builder.ConnectionString;
    }

    public async Task<bool> TestConnectionAsync(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            _logger.LogWarning("TestConnectionAsync called with empty connection string");
            return false;
        }

        try
        {
            _logger.LogInformation("Testing database connection...");
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            var isOpen = connection.State == System.Data.ConnectionState.Open;
            
            if (isOpen)
            {
                _logger.LogInformation("Database connection test successful. Server={0}, Database={1}", 
                    connection.DataSource, connection.Database);
            }
            else
            {
                _logger.LogWarning("Database connection test failed. Connection state: {0}", connection.State);
            }
            
            return isOpen;
        }
        catch (SqlException ex)
        {
            _logger.LogError("SQL connection test failed: {0}", ex, ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError("Unexpected error during connection test: {0}", ex, ex.Message);
            return false;
        }
    }

    public async Task<bool> TestConnectionAsync(string server, string database, string username, string password)
    {
        if (string.IsNullOrWhiteSpace(server) || string.IsNullOrWhiteSpace(database))
        {
            _logger.LogWarning("TestConnectionAsync called with missing server or database. Server={0}, Database={1}", 
                server ?? "null", database ?? "null");
            return false;
        }

        _logger.LogInformation("Testing connection to Server={0}, Database={1}, Username={2}", 
            server, database, string.IsNullOrEmpty(username) ? "Windows Auth" : username);
        
        var connectionString = BuildConnectionString(server, database, username, password);
        return await TestConnectionAsync(connectionString);
    }
}
