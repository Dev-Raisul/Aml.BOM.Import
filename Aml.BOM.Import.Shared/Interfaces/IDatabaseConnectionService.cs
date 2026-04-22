namespace Aml.BOM.Import.Shared.Interfaces;

public interface IDatabaseConnectionService
{
    Task<bool> TestConnectionAsync(string connectionString);
    Task<bool> TestConnectionAsync(string server, string database, string username, string password);
    string BuildConnectionString(string server, string database, string username, string password);
}
