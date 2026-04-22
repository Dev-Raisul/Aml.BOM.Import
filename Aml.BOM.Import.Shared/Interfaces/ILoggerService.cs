namespace Aml.BOM.Import.Shared.Interfaces;

public interface ILoggerService
{
    void LogInformation(string message, params object[] args);
    void LogWarning(string message, params object[] args);
    void LogError(string message, Exception? exception = null, params object[] args);
    void LogDebug(string message, params object[] args);
    void LogCritical(string message, Exception? exception = null, params object[] args);
}
