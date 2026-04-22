# Logging System Documentation

## Overview

The Aml.BOM.Import application includes a comprehensive file-based logging system that tracks all application activities, errors, and important events.

## Log File Location

Logs are automatically saved to:
```
%APPDATA%\Aml.BOM.Import\Logs\
```

Example full path:
```
C:\Users\YourUsername\AppData\Roaming\Aml.BOM.Import\Logs\
```

### Quick Access to Logs

1. **Via Windows Run Dialog**:
   - Press `Win + R`
   - Type: `%APPDATA%\Aml.BOM.Import\Logs`
   - Press Enter

2. **Via File Explorer**:
   - Open File Explorer
   - Paste in address bar: `%APPDATA%\Aml.BOM.Import\Logs`
   - Press Enter

## Log File Format

### File Naming Convention

Logs are organized by date:
```
BomImport_2024-01-15.log
BomImport_2024-01-16.log
```

Each day gets its own log file for easy organization and archival.

### Log Entry Format

```
[2024-01-15 14:30:45.123] [INFO] [Thread-1] Application host started successfully
--------------------------------------------------------------------------------
[2024-01-15 14:30:46.456] [ERROR] [Thread-3] Failed to connect to database
Exception: System.Data.SqlClient.SqlException
Message: Cannot open database "AmlBomImport" requested by the login
StackTrace: at System.Data.SqlClient.SqlConnection.OpenAsync()
   at Aml.BOM.Import.Infrastructure.Services.DatabaseConnectionService...
--------------------------------------------------------------------------------
```

Each log entry includes:
- **Timestamp**: Precise time including milliseconds
- **Log Level**: INFO, WARN, ERROR, DEBUG, CRITICAL
- **Thread ID**: Which thread logged the message
- **Message**: The log message
- **Exception Details** (if applicable):
  - Exception type
  - Exception message
  - Stack trace
  - Inner exception (if present)

## Log Levels

### 1. DEBUG
**Purpose**: Detailed diagnostic information  
**Use Cases**: 
- Connection string parsing
- Method entry/exit
- Variable state inspection

**Example**:
```csharp
_logger.LogDebug("Parsing connection string: {0}", connectionString);
```

### 2. INFO (Information)
**Purpose**: General informational messages  
**Use Cases**:
- Application startup/shutdown
- Successful operations
- Configuration loading
- User actions

**Example**:
```csharp
_logger.LogInformation("Settings loaded successfully");
_logger.LogInformation("Application Version: {0}", version);
```

### 3. WARN (Warning)
**Purpose**: Warning messages for potentially harmful situations  
**Use Cases**:
- Using default settings
- Connection test failures
- Validation warnings

**Example**:
```csharp
_logger.LogWarning("Using default connection string");
_logger.LogWarning("Connection test failed for Server={0}", server);
```

### 4. ERROR
**Purpose**: Error events that might still allow the application to continue  
**Use Cases**:
- Failed operations
- Exception handling
- Connection failures

**Example**:
```csharp
_logger.LogError("Failed to save settings", exception);
_logger.LogError("SQL connection test failed: {0}", exception, exception.Message);
```

### 5. CRITICAL
**Purpose**: Critical failures that may cause application termination  
**Use Cases**:
- Unhandled exceptions
- Fatal errors
- Application crashes

**Example**:
```csharp
_logger.LogCritical("Unhandled exception occurred", exception);
```

## What Gets Logged

### Application Lifecycle
- ? Application startup
- ? Application version and environment info
- ? Application shutdown
- ? Exit code

### Settings Management
- ? Settings loading
- ? Settings saving
- ? Settings file path
- ? Connection string parsing

### Database Operations
- ? Connection attempts
- ? Connection test results
- ? Connection string building
- ? SQL exceptions

### User Actions
- ? Settings changes
- ? Connection tests
- ? Save operations

### Errors and Exceptions
- ? All exceptions with full stack traces
- ? Inner exceptions
- ? Unhandled exceptions
- ? Dispatcher exceptions
- ? Task exceptions

## Log Rotation

### Automatic Rotation

The logging system automatically manages log file size:

- **Maximum File Size**: 10 MB per log file
- **Maximum Files**: 5 rotated files kept
- **Rotation Process**:
  1. When current log reaches 10 MB
  2. Files are renamed: `_1.log`, `_2.log`, etc.
  3. Oldest file (`_5.log`) is deleted
  4. New log file is created

### Example File Structure

```
Logs\
??? BomImport_2024-01-15.log      (current - 8 MB)
??? BomImport_2024-01-15_1.log    (rotated - 10 MB)
??? BomImport_2024-01-15_2.log    (rotated - 10 MB)
??? BomImport_2024-01-14.log      (previous day)
??? BomImport_2024-01-13.log      (older)
```

## Architecture

### Components

#### 1. ILoggerService Interface
**Location**: `Aml.BOM.Import.Shared\Interfaces\ILoggerService.cs`

```csharp
public interface ILoggerService
{
    void LogInformation(string message, params object[] args);
    void LogWarning(string message, params object[] args);
    void LogError(string message, Exception? exception = null, params object[] args);
    void LogDebug(string message, params object[] args);
    void LogCritical(string message, Exception? exception = null, params object[] args);
}
```

#### 2. FileLoggerService Implementation
**Location**: `Aml.BOM.Import.Infrastructure\Services\FileLoggerService.cs`

**Features**:
- Thread-safe logging
- Automatic log rotation
- Exception handling
- Formatted output
- Parameterized messages

#### 3. Integration Points

**Services Using Logging**:
- `DatabaseConnectionService` - Connection operations
- `SettingsService` - Settings management
- `SettingsViewModel` - User interactions
- `App` - Application lifecycle

**Global Exception Handlers**:
- `AppDomain.UnhandledException` - Unhandled exceptions
- `DispatcherUnhandledException` - UI thread exceptions
- `TaskScheduler.UnobservedTaskException` - Async exceptions

## Usage Examples

### Basic Logging

```csharp
public class MyService
{
    private readonly ILoggerService _logger;

    public MyService(ILoggerService logger)
    {
        _logger = logger;
    }

    public void DoSomething()
    {
        _logger.LogInformation("Starting operation");
        
        try
        {
            // Do work
            _logger.LogInformation("Operation completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError("Operation failed", ex);
            throw;
        }
    }
}
```

### Parameterized Logging

```csharp
// Good - uses parameters (avoids string allocation if logging is disabled)
_logger.LogInformation("Processing item {0} of {1}", currentItem, totalItems);

// Bad - string concatenation (always allocates memory)
_logger.LogInformation("Processing item " + currentItem + " of " + totalItems);
```

### Exception Logging

```csharp
try
{
    // Database operation
}
catch (SqlException ex)
{
    _logger.LogError("Database operation failed", ex);
    // Handle exception
}
```

### Structured Logging

```csharp
_logger.LogInformation("User {0} performed action {1} on {2}", 
    username, actionName, DateTime.Now);
```

## Troubleshooting with Logs

### Common Scenarios

#### 1. Application Won't Start

**Check logs for**:
```
[CRITICAL] Unhandled exception occurred
[ERROR] Failed to load connection string from settings file
```

**Solution**: Look at the exception details and stack trace

#### 2. Connection Failures

**Check logs for**:
```
[ERROR] SQL connection test failed
[WARN] Connection test failed for Server=localhost
```

**Solution**: Verify server name, credentials, and SQL Server status

#### 3. Settings Not Saving

**Check logs for**:
```
[ERROR] Failed to save settings to file
[WARN] Cannot validate connection: settings or connection string is null/empty
```

**Solution**: Check file permissions and path accessibility

#### 4. Unexpected Behavior

**Look for**:
- `[ERROR]` entries around the time of the issue
- `[WARN]` entries that might indicate the problem
- Exception stack traces for root cause

## Viewing Logs

### Using Notepad

1. Navigate to log directory
2. Open log file with Notepad
3. Search for ERROR or CRITICAL

### Using Notepad++

1. Open log file in Notepad++
2. Use Search > Find
3. Search for log levels: `[ERROR]`, `[CRITICAL]`
4. Enable "Bookmark line" to mark all occurrences

### Using PowerShell

```powershell
# View latest log
Get-Content "$env:APPDATA\Aml.BOM.Import\Logs\BomImport_$(Get-Date -Format 'yyyy-MM-dd').log" -Tail 50

# Search for errors
Get-Content "$env:APPDATA\Aml.BOM.Import\Logs\*.log" | Select-String -Pattern "\[ERROR\]"

# Count log levels
Get-Content "$env:APPDATA\Aml.BOM.Import\Logs\*.log" | Select-String -Pattern "\[(INFO|WARN|ERROR|CRITICAL)\]" | Group-Object
```

### Using Log Viewer Tools

**Recommended Tools**:
- **BareTail** - Free real-time log viewer
- **LogExpert** - Open-source log analyzer
- **Notepad++** - With NppLogGazer plugin

## Best Practices

### DO ?

- **Log important operations**: Save, Load, Connect
- **Log exceptions**: Always log exceptions with full details
- **Use appropriate levels**: INFO for success, ERROR for failures
- **Include context**: User actions, timestamps, identifiers
- **Use parameters**: `LogInformation("User {0}", username)`

### DON'T ?

- **Log sensitive data**: Passwords, connection strings with passwords
- **Log in tight loops**: Avoid logging in high-frequency operations
- **Catch and hide exceptions**: Always log caught exceptions
- **Use string concatenation**: Use parameterized messages instead
- **Log excessively**: Balance between detail and noise

## Performance Considerations

### Thread Safety

The logging system uses locks to ensure thread-safe file access:
```csharp
private readonly object _lockObject = new();

lock (_lockObject)
{
    // Write to file
}
```

### Fail-Safe Design

Logging operations never throw exceptions to prevent application crashes:
```csharp
catch
{
    // Fail silently - logging should never crash the application
}
```

## Maintenance

### Manual Cleanup

Old log files can be manually deleted:

```powershell
# Delete logs older than 30 days
Get-ChildItem "$env:APPDATA\Aml.BOM.Import\Logs\*.log" | 
    Where-Object {$_.LastWriteTime -lt (Get-Date).AddDays(-30)} | 
    Remove-Item
```

### Disk Space Management

Average disk usage:
- Daily log file: ~1-5 MB (normal use)
- With errors: ~10-20 MB per day
- Rotated files: Up to 50 MB per day (5 files × 10 MB)

## Security Considerations

### What's Logged

? **Safe to Log**:
- Usernames
- Server names
- Database names
- Timestamps
- Operations

?? **Never Logged**:
- Passwords
- Connection strings (with passwords)
- Sensitive user data

### Sanitized Logging

The code ensures passwords are not logged:
```csharp
_logger.LogInformation("Testing connection to Server={0}, Database={1}, Username={2}", 
    server, database, string.IsNullOrEmpty(username) ? "Windows Auth" : username);
// Password is NOT logged
```

## Future Enhancements

Potential improvements:
- [ ] Configurable log levels
- [ ] Log filtering and searching UI
- [ ] Email alerts for critical errors
- [ ] Structured logging (JSON format)
- [ ] Remote logging support
- [ ] Performance metrics logging

## Related Files

- `Aml.BOM.Import.Shared\Interfaces\ILoggerService.cs` - Interface
- `Aml.BOM.Import.Infrastructure\Services\FileLoggerService.cs` - Implementation
- `Aml.BOM.Import.UI\App.xaml.cs` - Global exception handlers
- `Aml.BOM.Import.Infrastructure\Services\DatabaseConnectionService.cs` - Usage example
- `Aml.BOM.Import.Infrastructure\Services\SettingsService.cs` - Usage example
- `Aml.BOM.Import.UI\ViewModels\SettingsViewModel.cs` - Usage example

## Support

If you need help with logs:
1. Navigate to log directory: `%APPDATA%\Aml.BOM.Import\Logs`
2. Open today's log file
3. Search for `[ERROR]` or `[CRITICAL]`
4. Review exception details and stack traces
5. Share relevant log excerpts with support team

---

**Log Directory**: `%APPDATA%\Aml.BOM.Import\Logs\`  
**Log Format**: Daily rotation with automatic size management  
**Thread-Safe**: Yes  
**Exception-Safe**: Yes (never crashes the application)
