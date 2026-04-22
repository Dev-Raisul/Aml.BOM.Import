using Aml.BOM.Import.Shared.Interfaces;
using System.Text;

namespace Aml.BOM.Import.Infrastructure.Services;

public class FileLoggerService : ILoggerService
{
    private readonly string _logDirectory;
    private readonly string _logFileName;
    private readonly object _lockObject = new();
    private readonly int _maxFileSizeMB = 10;
    private readonly int _maxLogFiles = 5;

    public FileLoggerService()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _logDirectory = Path.Combine(appDataPath, "Aml.BOM.Import", "Logs");
        Directory.CreateDirectory(_logDirectory);
        
        var today = DateTime.Now.ToString("yyyy-MM-dd");
        _logFileName = $"BomImport_{today}.log";
    }

    public void LogInformation(string message, params object[] args)
    {
        WriteLog("INFO", message, null, args);
    }

    public void LogWarning(string message, params object[] args)
    {
        WriteLog("WARN", message, null, args);
    }

    public void LogError(string message, Exception? exception = null, params object[] args)
    {
        WriteLog("ERROR", message, exception, args);
    }

    public void LogDebug(string message, params object[] args)
    {
        WriteLog("DEBUG", message, null, args);
    }

    public void LogCritical(string message, Exception? exception = null, params object[] args)
    {
        WriteLog("CRITICAL", message, exception, args);
    }

    private void WriteLog(string level, string message, Exception? exception, params object[] args)
    {
        lock (_lockObject)
        {
            try
            {
                var logFilePath = Path.Combine(_logDirectory, _logFileName);
                
                // Check file size and rotate if necessary
                RotateLogFileIfNeeded(logFilePath);

                var formattedMessage = args.Length > 0 ? string.Format(message, args) : message;
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                var threadId = Environment.CurrentManagedThreadId;
                
                var logEntry = new StringBuilder();
                logEntry.AppendLine($"[{timestamp}] [{level}] [Thread-{threadId}] {formattedMessage}");
                
                if (exception != null)
                {
                    logEntry.AppendLine($"Exception: {exception.GetType().FullName}");
                    logEntry.AppendLine($"Message: {exception.Message}");
                    logEntry.AppendLine($"StackTrace: {exception.StackTrace}");
                    
                    if (exception.InnerException != null)
                    {
                        logEntry.AppendLine($"Inner Exception: {exception.InnerException.GetType().FullName}");
                        logEntry.AppendLine($"Inner Message: {exception.InnerException.Message}");
                    }
                }
                
                logEntry.AppendLine(new string('-', 80));

                File.AppendAllText(logFilePath, logEntry.ToString());
            }
            catch
            {
                // Fail silently - logging should never crash the application
            }
        }
    }

    private void RotateLogFileIfNeeded(string logFilePath)
    {
        if (!File.Exists(logFilePath))
            return;

        var fileInfo = new FileInfo(logFilePath);
        var fileSizeMB = fileInfo.Length / (1024.0 * 1024.0);

        if (fileSizeMB >= _maxFileSizeMB)
        {
            // Rotate log files
            for (int i = _maxLogFiles - 1; i >= 1; i--)
            {
                var oldFile = Path.Combine(_logDirectory, $"{Path.GetFileNameWithoutExtension(_logFileName)}_{i}.log");
                var newFile = Path.Combine(_logDirectory, $"{Path.GetFileNameWithoutExtension(_logFileName)}_{i + 1}.log");
                
                if (File.Exists(oldFile))
                {
                    if (File.Exists(newFile))
                        File.Delete(newFile);
                    File.Move(oldFile, newFile);
                }
            }

            // Rename current log file
            var rotatedFile = Path.Combine(_logDirectory, $"{Path.GetFileNameWithoutExtension(_logFileName)}_1.log");
            if (File.Exists(rotatedFile))
                File.Delete(rotatedFile);
            File.Move(logFilePath, rotatedFile);
        }
    }

    public string GetLogDirectory() => _logDirectory;

    public IEnumerable<string> GetLogFiles()
    {
        return Directory.GetFiles(_logDirectory, "*.log")
            .OrderByDescending(f => new FileInfo(f).LastWriteTime);
    }
}
