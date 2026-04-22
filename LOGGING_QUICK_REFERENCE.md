# Logging Quick Reference

## Log Location
```
%APPDATA%\Aml.BOM.Import\Logs\
```

## Quick Access
**Windows Run**: `Win + R` ? Type: `%APPDATA%\Aml.BOM.Import\Logs`

## Log Levels

| Level | Purpose | Example |
|-------|---------|---------|
| **DEBUG** | Detailed diagnostic info | Method entry/exit, variable values |
| **INFO** | General information | Successful operations, app startup |
| **WARN** | Potential issues | Using defaults, validation warnings |
| **ERROR** | Errors that allow continuation | Failed operations, caught exceptions |
| **CRITICAL** | Fatal errors | Unhandled exceptions, app crashes |

## Common Search Terms

When viewing logs, search for:

- `[ERROR]` - All errors
- `[CRITICAL]` - Critical failures
- `Exception:` - Exception details
- `Connection` - Database connection issues
- `Settings` - Settings operations
- `Application Starting` - App startup
- `Application Shutting Down` - App shutdown

## PowerShell Commands

### View Today's Log
```powershell
Get-Content "$env:APPDATA\Aml.BOM.Import\Logs\BomImport_$(Get-Date -Format 'yyyy-MM-dd').log" -Tail 50
```

### Find All Errors
```powershell
Get-Content "$env:APPDATA\Aml.BOM.Import\Logs\*.log" | Select-String "[ERROR]"
```

### Find Critical Issues
```powershell
Get-Content "$env:APPDATA\Aml.BOM.Import\Logs\*.log" | Select-String "[CRITICAL]"
```

### Count Log Entries by Level
```powershell
Get-Content "$env:APPDATA\Aml.BOM.Import\Logs\*.log" | 
    Select-String "\[(INFO|WARN|ERROR|CRITICAL)\]" | 
    Group-Object | 
    Select-Object Count, Name
```

### Delete Old Logs (30+ days)
```powershell
Get-ChildItem "$env:APPDATA\Aml.BOM.Import\Logs\*.log" | 
    Where-Object {$_.LastWriteTime -lt (Get-Date).AddDays(-30)} | 
    Remove-Item
```

## Log Entry Format

```
[Timestamp] [Level] [Thread] Message
Exception details (if applicable)
----------------------------------------------------
```

Example:
```
[2024-01-15 14:30:45.123] [ERROR] [Thread-3] Failed to connect to database
Exception: System.Data.SqlClient.SqlException
Message: Cannot open database "AmlBomImport"
StackTrace: at System.Data.SqlClient.SqlConnection.OpenAsync()
----------------------------------------------------
```

## File Rotation

- **Max File Size**: 10 MB
- **Max Files Kept**: 5 rotated files
- **Naming**: `BomImport_YYYY-MM-DD.log` and `BomImport_YYYY-MM-DD_N.log`

## What's Logged

? **Logged**:
- Application start/stop
- Settings operations
- Connection tests
- User actions
- Errors and exceptions

? **NOT Logged**:
- Passwords
- Sensitive data

## Troubleshooting

### Application Won't Start
**Search for**: `[CRITICAL]` or `Unhandled exception`

### Connection Issues
**Search for**: `Connection` or `Database`

### Settings Problems
**Search for**: `Settings` or `appsettings.json`

### General Errors
**Search for**: `[ERROR]`

## Code Usage

### Inject Logger
```csharp
public MyService(ILoggerService logger)
{
    _logger = logger;
}
```

### Log Information
```csharp
_logger.LogInformation("Operation completed");
_logger.LogInformation("Processing {0} items", count);
```

### Log Errors
```csharp
_logger.LogError("Operation failed", exception);
_logger.LogError("Error: {0}", exception, exception.Message);
```

### Log Warnings
```csharp
_logger.LogWarning("Using default value");
```

## Support

For detailed documentation, see: [LOGGING_SYSTEM_GUIDE.md](LOGGING_SYSTEM_GUIDE.md)
