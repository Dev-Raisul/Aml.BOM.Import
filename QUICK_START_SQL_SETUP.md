# Quick Start Guide - SQL Connection Setup

This guide will help you set up the database connection for the BOM Import application in **5 minutes**.

## Prerequisites Checklist

- [ ] SQL Server installed (LocalDB, Express, or Full)
- [ ] SQL Server Management Studio (SSMS) or Azure Data Studio
- [ ] Application built successfully

## Step 1: Create the Database (2 minutes)

### Option A: Using SQL Server Management Studio (Recommended)

1. Open SQL Server Management Studio
2. Connect to your SQL Server instance
3. Open the file: `Database\CreateDatabase.sql`
4. Click **Execute** (or press F5)
5. Wait for "Database AmlBomImport created successfully!" message

### Option B: Using Command Line

```cmd
sqlcmd -S localhost -E -i Database\CreateDatabase.sql
```

## Step 2: Create the Tables (1 minute)

### In SQL Server Management Studio

1. Keep SSMS open from Step 1
2. Open the file: `Database\CreateSchema.sql`
3. Click **Execute** (or press F5)
4. Wait for "Database schema created successfully!" message
5. Verify 5 tables were created (SageItems, NewBuyItems, NewMakeItems, BomImportRecords, BomImportLines)

## Step 3: Configure Application Settings (2 minutes)

### Launch the Application

1. Run the application
2. Click **Settings** in the left navigation panel

### For Windows Authentication (Easier)

If you want to use Windows Authentication (your current Windows login):

1. **Server**: Enter your server name (e.g., `localhost` or `.\SQLEXPRESS`)
2. **Database**: Enter `AmlBomImport`
3. **Username**: Leave blank
4. **Password**: Leave blank

### For SQL Server Authentication

If you want to use SQL Server credentials:

1. **Server**: Enter your server name (e.g., `localhost`)
2. **Database**: Enter `AmlBomImport`
3. **Username**: Enter your SQL username (e.g., `sa`)
4. **Password**: Enter your SQL password

### Test and Save

1. Click **Test Connection**
2. You should see: **"? Connection successful!"**
3. If successful, click **Save Settings**
4. You should see: **"Settings saved successfully!"**

### If Connection Fails

Common issues and solutions:

**"Connection failed"**
- Verify SQL Server is running
- Check server name is correct
- For named instances, use format: `.\SQLEXPRESS`

**"Login failed"**
- For Windows Auth: Ensure you have database access
- For SQL Auth: Verify username/password are correct

**"Server not found"**
- Try `localhost` instead of `.`
- Try `.\SQLEXPRESS` for SQL Express
- Check SQL Server Configuration Manager - TCP/IP must be enabled

## Step 4: Verify Everything Works

### Check Settings File

1. Press `Win + R`
2. Type: `%APPDATA%\Aml.BOM.Import`
3. Press Enter
4. You should see `appsettings.json`
5. Open it - verify your connection string is saved

### Check Database Tables

In SQL Server Management Studio:

```sql
USE AmlBomImport;
GO

-- List all tables
SELECT TABLE_NAME 
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_TYPE = 'BASE TABLE'
ORDER BY TABLE_NAME;

-- Check sample data
SELECT 'SageItems' AS [Table], COUNT(*) AS [Records] FROM SageItems
UNION ALL
SELECT 'NewBuyItems', COUNT(*) FROM NewBuyItems
UNION ALL
SELECT 'NewMakeItems', COUNT(*) FROM NewMakeItems;
```

You should see:
- 5 tables listed
- 3 records in SageItems
- 2 records in NewBuyItems
- 2 records in NewMakeItems

## You're Done! ??

Your database is now set up and ready to use.

## Common Server Names

Choose the one that matches your installation:

| Installation Type | Server Name |
|------------------|-------------|
| SQL Server Express (default instance) | `.\SQLEXPRESS` |
| SQL Server LocalDB | `(localdb)\MSSQLLocalDB` |
| SQL Server Full (default instance) | `localhost` or `.` |
| SQL Server Full (named instance) | `.\InstanceName` |
| Remote SQL Server | `192.168.1.100` or `servername` |

## Quick Reference Commands

### Find SQL Server Instances
```cmd
sqlcmd -L
```

### Test Connection from Command Line
```cmd
sqlcmd -S localhost -E -Q "SELECT @@VERSION"
```

### Open Settings Folder
```cmd
explorer %APPDATA%\Aml.BOM.Import
```

### Restart SQL Server Service (if needed)
```cmd
net stop MSSQLSERVER
net start MSSQLSERVER
```

For SQL Express:
```cmd
net stop MSSQL$SQLEXPRESS
net start MSSQL$SQLEXPRESS
```

## Troubleshooting

### Can't Find SQL Server?

1. Check SQL Server Configuration Manager
2. Ensure SQL Server service is running
3. Enable TCP/IP protocol
4. Restart SQL Server service

### Access Denied?

1. For Windows Auth: Add your Windows account to SQL Server
2. For SQL Auth: Enable Mixed Mode Authentication
3. Restart SQL Server after changing authentication mode

### Port Issues?

Default SQL Server port is 1433. If using custom port:
```
Server=localhost,1433;Database=AmlBomImport;...
```

## Need More Help?

See detailed documentation:
- [DATABASE_CONNECTION_GUIDE.md](DATABASE_CONNECTION_GUIDE.md) - Comprehensive guide
- [Database\README.md](Database/README.md) - Database-specific help
- [SETTINGS_FILE_LOCATION.md](SETTINGS_FILE_LOCATION.md) - Settings file info

## Next Steps

After setup is complete:
1. ? Database created and configured
2. ? Application connected to database
3. Start using the application:
   - Import BOM files
   - Manage new buy/make items
   - Integrate BOMs into Sage

---

**Estimated Time**: 5 minutes  
**Difficulty**: Easy  
**Prerequisites**: SQL Server installed
