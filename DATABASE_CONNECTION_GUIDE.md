# SQL Connection Configuration Guide

## Overview

The BOM Import application uses SQL Server for data persistence. Connection settings are stored in a JSON file in the user's AppData folder, ensuring settings persist across application restarts.

## Settings Storage Location

Settings are automatically saved to:
```
%APPDATA%\Aml.BOM.Import\appsettings.json
```

Full path example:
```
C:\Users\YourUsername\AppData\Roaming\Aml.BOM.Import\appsettings.json
```

## Architecture

### Components

1. **IDatabaseConnectionService** (`Aml.BOM.Import.Shared`)
   - Interface for database connection operations
   - Methods: `TestConnectionAsync()`, `BuildConnectionString()`

2. **DatabaseConnectionService** (`Aml.BOM.Import.Infrastructure`)
   - Concrete implementation of connection service
   - Uses `Microsoft.Data.SqlClient` for SQL Server connections
   - Handles connection string building and validation

3. **ISettingsService** (`Aml.BOM.Import.Shared`)
   - Interface for settings management
   - Methods: `GetSettingsAsync()`, `SaveSettingsAsync()`, `ValidateConnectionAsync()`

4. **SettingsService** (`Aml.BOM.Import.Infrastructure`)
   - Manages settings persistence to JSON file
   - Caches settings in memory for performance
   - Integrates with DatabaseConnectionService for validation

5. **AppSettings Model** (`Aml.BOM.Import.Application.Models`)
   - Data model for application settings
   - Contains: DatabaseConnectionString, SageSettings, ReportSettings

## Settings File Format

The `appsettings.json` file has the following structure:

```json
{
  "DatabaseConnectionString": "Server=localhost;Database=AmlBomImport;User Id=sa;Password=YourPassword;TrustServerCertificate=true;",
  "SageSettings": {
    "ServerUrl": "https://sage-server.example.com",
    "Username": "sage_user",
    "Password": "sage_password",
    "CompanyCode": "COMP001"
  },
  "ReportSettings": {
    "OutputDirectory": "C:\\Reports\\BomImports",
    "AutoGenerateReports": true
  }
}
```

## How to Configure Database Connection

### Using the UI (Recommended)

1. Launch the application
2. Click **Settings** in the left navigation panel
3. Fill in the Database Settings:
   - **Server**: Your SQL Server instance (e.g., `localhost`, `.\SQLEXPRESS`, or `server-name\instance`)
   - **Database**: Database name (e.g., `AmlBomImport`)
   - **Username**: SQL Server username (leave blank for Windows Authentication)
   - **Password**: SQL Server password (leave blank for Windows Authentication)
4. Click **Test Connection** to verify settings
5. Click **Save Settings** to persist changes

### Windows Authentication

For Windows Authentication, you can either:
- Leave Username and Password fields empty (the connection string will be built with `Integrated Security=true`)
- Or manually edit the connection string in the JSON file

### SQL Server Authentication

For SQL Server Authentication:
- Enter the SQL Server username in the **Username** field
- Enter the SQL Server password in the **Password** field

## Connection String Format

The application uses `SqlConnectionStringBuilder` to construct connection strings with the following properties:

- **DataSource**: Server name/instance
- **InitialCatalog**: Database name
- **UserID**: Username (omitted for Windows Auth)
- **Password**: Password (omitted for Windows Auth)
- **TrustServerCertificate**: Set to `true` for development
- **ConnectTimeout**: Set to 10 seconds

### Example Connection Strings

**SQL Server Authentication:**
```
Server=localhost;Database=AmlBomImport;User Id=sa;Password=MyPassword123;TrustServerCertificate=true;
```

**Windows Authentication:**
```
Server=localhost;Database=AmlBomImport;Integrated Security=true;TrustServerCertificate=true;
```

**Named Instance:**
```
Server=.\SQLEXPRESS;Database=AmlBomImport;Integrated Security=true;TrustServerCertificate=true;
```

**Remote Server:**
```
Server=192.168.1.100,1433;Database=AmlBomImport;User Id=dbuser;Password=dbpass;TrustServerCertificate=true;
```

## Connection Testing

The **Test Connection** button performs the following:

1. Builds a connection string from the entered fields
2. Attempts to open a connection to SQL Server
3. Verifies the connection state
4. Returns success or failure with appropriate message

Error messages include:
- ? Connection successful!
- ? Connection failed. Please check your settings.
- ? Connection error: [specific error message]

## Database Setup

### Prerequisites

1. SQL Server installed (LocalDB, Express, or Full)
2. Database created (if not, create it first)
3. Appropriate user permissions

### Creating the Database

Run this script in SQL Server Management Studio or Azure Data Studio:

```sql
CREATE DATABASE AmlBomImport;
GO

USE AmlBomImport;
GO

-- Tables will be created here (TODO: Add schema creation scripts)
```

### User Permissions

The database user needs the following permissions:
- `db_datareader`
- `db_datawriter`
- `db_ddladmin` (if creating tables)

Grant permissions:
```sql
USE AmlBomImport;
GO

CREATE LOGIN [your_user] WITH PASSWORD = 'YourPassword123';
GO

CREATE USER [your_user] FOR LOGIN [your_user];
GO

ALTER ROLE db_datareader ADD MEMBER [your_user];
ALTER ROLE db_datawriter ADD MEMBER [your_user];
GO
```

## Settings Persistence Flow

### On Application Startup

1. `App.xaml.cs` calls `GetConnectionString()`
2. Reads `appsettings.json` from AppData folder
3. Deserializes to `AppSettings` object
4. Extracts `DatabaseConnectionString`
5. Uses connection string to initialize repositories

### When User Opens Settings

1. `SettingsViewModel` constructor calls `LoadSettingsCommand`
2. `SettingsService.GetSettingsAsync()` reads from JSON file
3. Connection string is parsed into individual fields
4. Fields are displayed in the UI

### When User Saves Settings

1. User clicks **Save Settings**
2. Individual fields are combined into connection string
3. `AppSettings` object is created with all settings
4. `SettingsService.SaveSettingsAsync()` serializes to JSON
5. JSON is written to `appsettings.json`
6. Settings are cached in memory

### Connection String Parsing

The `ParseConnectionString()` method in `SettingsViewModel` supports various connection string formats:

- `Server=` or `Data Source=`
- `Database=` or `Initial Catalog=`
- `User Id=` or `UID=`
- `Password=` or `PWD=`

## Troubleshooting

### Connection Fails

**Problem**: "Connection failed. Please check your settings."

**Solutions**:
1. Verify SQL Server is running
2. Check server name/instance is correct
3. Verify database exists
4. Check firewall settings
5. Verify user credentials

### Settings Not Persisting

**Problem**: Settings are lost after closing the application

**Solutions**:
1. Check write permissions to `%APPDATA%\Aml.BOM.Import\`
2. Verify `appsettings.json` file exists after saving
3. Check for exceptions in the application logs

### Cannot Find Server

**Problem**: "A network-related or instance-specific error occurred"

**Solutions**:
1. Use `localhost` for local SQL Server
2. Use `.\SQLEXPRESS` for SQL Server Express
3. Verify TCP/IP is enabled in SQL Server Configuration Manager
4. Check SQL Server Browser service is running (for named instances)

### Login Failed

**Problem**: "Login failed for user 'username'"

**Solutions**:
1. Verify SQL Server Authentication is enabled (Mixed Mode)
2. Check username and password are correct
3. Ensure user has access to the database
4. Check user is not locked out

## Code Examples

### Manually Reading Settings

```csharp
var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
var settingsPath = Path.Combine(appDataPath, "Aml.BOM.Import", "appsettings.json");
var json = File.ReadAllText(settingsPath);
var settings = JsonSerializer.Deserialize<AppSettings>(json);
Console.WriteLine($"Connection: {settings.DatabaseConnectionString}");
```

### Manually Testing Connection

```csharp
using var connection = new SqlConnection(connectionString);
await connection.OpenAsync();
Console.WriteLine($"Connection State: {connection.State}");
```

### Building Custom Connection String

```csharp
var builder = new SqlConnectionStringBuilder
{
    DataSource = "localhost",
    InitialCatalog = "AmlBomImport",
    IntegratedSecurity = true,
    TrustServerCertificate = true
};
var connectionString = builder.ConnectionString;
```

## Security Considerations

### Password Storage

- Passwords are stored in **plain text** in the JSON file
- The file is stored in the user's AppData folder (protected by Windows user permissions)
- For production use, consider encrypting sensitive data using Windows DPAPI

### Connection String Security

- Use Windows Authentication when possible
- Store connection strings in user-specific locations
- Avoid hardcoding connection strings in source code
- Use minimum required permissions for database users

### Recommendations for Production

1. Implement connection string encryption
2. Use Azure Key Vault for cloud deployments
3. Implement audit logging for settings changes
4. Use service accounts with minimal permissions
5. Enable SQL Server encryption (TLS/SSL)

## Future Enhancements

- [ ] Add support for connection string encryption
- [ ] Add support for multiple database profiles
- [ ] Add connection pooling configuration
- [ ] Add retry policies for transient failures
- [ ] Add connection string validation rules
- [ ] Add backup/restore settings functionality

## Related Files

- `Aml.BOM.Import.Shared\Interfaces\IDatabaseConnectionService.cs`
- `Aml.BOM.Import.Shared\Interfaces\ISettingsService.cs`
- `Aml.BOM.Import.Infrastructure\Services\DatabaseConnectionService.cs`
- `Aml.BOM.Import.Infrastructure\Services\SettingsService.cs`
- `Aml.BOM.Import.Application\Models\AppSettings.cs`
- `Aml.BOM.Import.UI\ViewModels\SettingsViewModel.cs`
- `Aml.BOM.Import.UI\Views\SettingsView.xaml`
- `Aml.BOM.Import.UI\App.xaml.cs`

## Support

For issues or questions:
1. Check the troubleshooting section above
2. Verify SQL Server is accessible
3. Check application logs
4. Review the settings JSON file manually
5. Contact development team
