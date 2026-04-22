# Database Scripts

This folder contains SQL scripts for setting up the Aml.BOM.Import database.

## Quick Start

### Option 1: Run Scripts in SQL Server Management Studio (SSMS)

1. **Create the Database**
   - Open `CreateDatabase.sql` in SSMS
   - Ensure you're connected to your SQL Server instance
   - Click Execute (or press F5)
   - Verify success message

2. **Create the Schema**
   - Open `CreateSchema.sql` in SSMS
   - Click Execute (or press F5)
   - Verify all tables were created

### Option 2: Run via Command Line (sqlcmd)

```cmd
REM Create database
sqlcmd -S localhost -E -i CreateDatabase.sql

REM Create schema
sqlcmd -S localhost -E -i CreateSchema.sql
```

### Option 3: Run via PowerShell

```powershell
# Create database
Invoke-Sqlcmd -ServerInstance "localhost" -InputFile "CreateDatabase.sql"

# Create schema
Invoke-Sqlcmd -ServerInstance "localhost" -InputFile "CreateSchema.sql"
```

## Files

### CreateDatabase.sql
Creates the `AmlBomImport` database.
- **WARNING**: Drops existing database if it exists
- Safe to run multiple times for development
- Use caution in production environments

### CreateSchema.sql
Creates all tables, indexes, and relationships.
- Drops existing tables if they exist
- Creates sample data for testing
- Can be run multiple times

## Database Structure

### Tables

1. **SageItems**
   - Cache of items from Sage system
   - Tracks buy vs make items
   - Indexed on ItemCode and ItemType

2. **NewBuyItems**
   - New buy items identified during import
   - Tracks integration status
   - Pending items await integration into Sage

3. **NewMakeItems**
   - New make items with editable fields
   - Includes cost and lead time data
   - Supports bulk editing operations

4. **BomImportRecords**
   - Header records for BOM imports
   - Tracks import status and validation
   - Links to duplicate BOMs if applicable

5. **BomImportLines**
   - Line items for BOM imports
   - Foreign key to BomImportRecords
   - Cascade delete enabled

## Connection Requirements

### Windows Authentication (Recommended)
```
Server=localhost;Database=AmlBomImport;Integrated Security=true;TrustServerCertificate=true;
```

### SQL Server Authentication
```
Server=localhost;Database=AmlBomImport;User Id=sa;Password=YourPassword;TrustServerCertificate=true;
```

## After Database Setup

1. **Configure Application Settings**
   - Launch the application
   - Go to Settings page
   - Enter database connection details
   - Test connection
   - Save settings

2. **Verify Tables**
   ```sql
   USE AmlBomImport;
   GO
   
   SELECT TABLE_NAME 
   FROM INFORMATION_SCHEMA.TABLES 
   WHERE TABLE_TYPE = 'BASE TABLE'
   ORDER BY TABLE_NAME;
   ```

3. **Check Sample Data**
   ```sql
   SELECT 'SageItems' AS [Table], COUNT(*) AS [Count] FROM dbo.SageItems
   UNION ALL
   SELECT 'NewBuyItems', COUNT(*) FROM dbo.NewBuyItems
   UNION ALL
   SELECT 'NewMakeItems', COUNT(*) FROM dbo.NewMakeItems;
   ```

## Troubleshooting

### Cannot create database
**Error**: "Database 'AmlBomImport' already exists"
- The CreateDatabase.sql script should drop the existing database
- Ensure no connections are open to the database
- Close the application before running the script

### Permission denied
**Error**: "User does not have permission to create database"
- Run SSMS as Administrator
- Or ensure your SQL login has `dbcreator` server role

### Cannot execute script
**Error**: "Could not find file"
- Ensure you're in the correct directory
- Use full path to SQL files
- Check file exists in Database folder

## Maintenance Scripts

### Drop All Tables
```sql
USE AmlBomImport;
GO

DROP TABLE IF EXISTS dbo.BomImportLines;
DROP TABLE IF EXISTS dbo.BomImportRecords;
DROP TABLE IF EXISTS dbo.NewMakeItems;
DROP TABLE IF EXISTS dbo.NewBuyItems;
DROP TABLE IF EXISTS dbo.SageItems;
GO
```

### Clear All Data (Keep Tables)
```sql
USE AmlBomImport;
GO

DELETE FROM dbo.BomImportLines;
DELETE FROM dbo.BomImportRecords;
DELETE FROM dbo.NewMakeItems;
DELETE FROM dbo.NewBuyItems;
DELETE FROM dbo.SageItems;
GO
```

### View Table Sizes
```sql
USE AmlBomImport;
GO

SELECT 
    t.NAME AS TableName,
    p.rows AS RowCounts,
    SUM(a.total_pages) * 8 AS TotalSpaceKB,
    SUM(a.used_pages) * 8 AS UsedSpaceKB
FROM sys.tables t
INNER JOIN sys.indexes i ON t.OBJECT_ID = i.object_id
INNER JOIN sys.partitions p ON i.object_id = p.OBJECT_ID AND i.index_id = p.index_id
INNER JOIN sys.allocation_units a ON p.partition_id = a.container_id
WHERE t.is_ms_shipped = 0
GROUP BY t.Name, p.Rows
ORDER BY t.Name;
GO
```

## Backup and Restore

### Backup Database
```sql
BACKUP DATABASE AmlBomImport
TO DISK = 'C:\Backups\AmlBomImport.bak'
WITH FORMAT, INIT, COMPRESSION;
GO
```

### Restore Database
```sql
RESTORE DATABASE AmlBomImport
FROM DISK = 'C:\Backups\AmlBomImport.bak'
WITH REPLACE;
GO
```

## Next Steps

1. ? Create database
2. ? Create schema
3. ? Configure application settings
4. ? Test connection
5. Start using the application

## Related Documentation

- [DATABASE_CONNECTION_GUIDE.md](../DATABASE_CONNECTION_GUIDE.md) - Detailed connection configuration guide
- [SETTINGS_FILE_LOCATION.md](../SETTINGS_FILE_LOCATION.md) - Settings file location and management
- [README.md](../README.md) - Main project documentation
