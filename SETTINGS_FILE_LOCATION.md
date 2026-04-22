# Settings File Location

The application stores all settings in a JSON file located at:

```
%APPDATA%\Aml.BOM.Import\appsettings.json
```

## Quick Access

### Windows Run Dialog
1. Press `Win + R`
2. Type: `%APPDATA%\Aml.BOM.Import`
3. Press Enter

### File Explorer
1. Open File Explorer
2. Paste in address bar: `%APPDATA%\Aml.BOM.Import`
3. Press Enter

### Command Prompt
```cmd
explorer %APPDATA%\Aml.BOM.Import
```

## Full Path Examples

- **Windows 10/11**: `C:\Users\YourUsername\AppData\Roaming\Aml.BOM.Import\appsettings.json`
- **Windows Server**: `C:\Users\YourUsername\AppData\Roaming\Aml.BOM.Import\appsettings.json`

## Settings Included

- Database connection settings (server, database, username, password)
- Sage integration settings (server URL, credentials, company code)
- Report settings (output directory, auto-generate flag)

## Managing Settings

### Through UI (Recommended)
Use the Settings page in the application to modify all settings.

### Manual Editing
1. Close the application
2. Navigate to settings file location
3. Edit `appsettings.json` with a text editor
4. Save changes
5. Restart application

### Backup Settings
Copy the `appsettings.json` file to a safe location.

### Reset Settings
Delete the `appsettings.json` file. The application will create a new one with defaults on next startup.

## File Structure

```json
{
  "DatabaseConnectionString": "Server=localhost;Database=AmlBomImport;...",
  "SageSettings": {
    "ServerUrl": "",
    "Username": "",
    "Password": "",
    "CompanyCode": ""
  },
  "ReportSettings": {
    "OutputDirectory": "",
    "AutoGenerateReports": false
  }
}
```
