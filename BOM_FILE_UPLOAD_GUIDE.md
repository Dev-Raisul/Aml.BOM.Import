# BOM File Upload and Logging Implementation Guide

## Overview

This document describes the implementation of BOM file upload functionality with logging to the `isBOMImportFileLog` table in the MAS_AML database.

## Database Structure

### Database Name
**MAS_AML** - Main database for the Aml.BOM.Import application

### Import Log Table: isBOMImportFileLog

This table tracks all BOM file uploads.

#### Table Structure

| Column Name | Data Type | Description |
|------------|-----------|-------------|
| FileId | INT (PK, Identity) | Auto-generated unique identifier |
| FileName | NVARCHAR(255) | Name of the uploaded file |
| UploadDate | DATETIME2 | When file was uploaded |

**Simple and Clean Design**: Only 3 columns - FileId, FileName, and UploadDate

## Architecture

### Components Created

#### 1. Domain Entity
**File**: `Aml.BOM.Import.Domain\Entities\ImportBomFileLog.cs`

```csharp
public class ImportBomFileLog
{
    public int FileId { get; set; }
    public string FileName { get; set; }
    public DateTime UploadDate { get; set; }
}
```

#### 2. Repository Interface
**File**: `Aml.BOM.Import.Shared\Interfaces\IImportBomFileLogRepository.cs`

Methods:
- `CreateAsync(ImportBomFileLog)` - Create new log entry
- `GetByIdAsync(int)` - Get log by FileId
- `GetAllAsync()` - Get all logs
- `GetRecentAsync(int)` - Get recent logs (default 50)
- `DeleteAsync(int)` - Delete log entry

#### 3. Repository Implementation
**File**: `Aml.BOM.Import.Infrastructure\Repositories\ImportBomFileLogRepository.cs`

Features:
- Full CRUD operations
- SQL Server integration
- Comprehensive logging via ILoggerService
- Exception handling

#### 4. Updated FileImportService
**File**: `Aml.BOM.Import.Infrastructure\Services\FileImportService.cs`

Enhanced with:
- Automatic file logging
- File validation
- Error handling
- Size limits (50MB default)

## Database Setup

### Step 1: Create Database

Run `Database\CreateMAS_AML_Database.sql`:

```sql
-- Creates MAS_AML database
CREATE DATABASE MAS_AML;
GO
```

### Step 2: Create Main Schema

Run `Database\CreateMAS_AML_Schema.sql`:

```sql
-- Creates: SageItems, NewBuyItems, NewMakeItems, 
--          BomImportRecords, BomImportLines
USE MAS_AML;
GO
```

### Step 3: Create Import Log Table

Run `Database\CreateImportBOMFileLogTable.sql`:

```sql
-- Creates: isBOMImportFileLog
CREATE TABLE dbo.isBOMImportFileLog
(
    FileId INT PRIMARY KEY IDENTITY(1,1),
    FileName NVARCHAR(255) NOT NULL,
    UploadDate DATETIME2 NOT NULL DEFAULT GETDATE()
);
GO
```

## Usage Flow

### 1. File Upload Process

```
User Selects File
       ?
FileImportService.ValidateFileFormatAsync()
       ?
Validate File (extension, size, exists)
       ?
FileImportService.ImportFileAsync()
       ?
Create ImportBomFileLog record
       ?
Log entry created with FileId
       ?
Return FileId to caller
```

### 2. Code Example

```csharp
// Inject dependencies
public class MyService
{
    private readonly IFileImportService _fileImportService;
    
    public MyService(IFileImportService fileImportService)
    {
        _fileImportService = fileImportService;
    }
    
    public async Task UploadBomFile(string filePath)
    {
        // Validate file
        var isValid = await _fileImportService.ValidateFileFormatAsync(filePath);
        if (!isValid)
        {
            throw new Exception("Invalid file format");
        }
        
        // Import file (automatically logs to isBOMImportFileLog)
        var result = await _fileImportService.ImportFileAsync(filePath);
        
        // Result contains FileId
        dynamic fileInfo = result;
        Console.WriteLine($"File uploaded. FileId: {fileInfo.FileId}");
    }
}
```

### 3. Querying Import Logs

```csharp
// Get recent uploads
var recentLogs = await _fileLogRepository.GetRecentAsync(10);

// Get all logs
var allLogs = await _fileLogRepository.GetAllAsync();

// Get specific log
var log = await _fileLogRepository.GetByIdAsync(fileId);
```

## Sample BOM File Structure

Based on the provided Excel file (`Proposed Sage Upload Template_r5.xlsx`):

**Supported Formats**: `.csv`, `.xlsx`, `.xls`

**Processing Logic** (to be implemented):
1. Read Excel/CSV file
2. Parse headers
3. Extract BOM records
4. Validate against Sage items
5. Identify new items (buy/make)
6. Create BOM records and lines
7. Log file upload to isBOMImportFileLog

## Logging Integration

### What Gets Logged

**File Upload Events**:
- File name
- Upload timestamp
- Auto-generated FileId

**Application Logs**: `%APPDATA%\Aml.BOM.Import\Logs\`
- File validation results
- Import success/failure
- Exception details

**Database Logs**: `MAS_AML.dbo.isBOMImportFileLog`
- Simple tracking with 3 columns
- FileId, FileName, UploadDate

## Configuration

### Connection String

Update in Settings or `appsettings.json`:

```json
{
  "DatabaseConnectionString": "Server=localhost;Database=MAS_AML;Trusted_Connection=true;TrustServerCertificate=true;"
}
```

### File Upload Settings

**Supported Formats**: `.csv`, `.xlsx`, `.xls`
**Maximum File Size**: 50 MB (configurable in FileImportService)

## Error Handling

### File Validation Errors

```csharp
// File not found
FileNotFoundException ? logged via ILoggerService

// Unsupported format
Invalid extension ? ValidateFileFormatAsync returns false

// File too large
File > 50MB ? ValidateFileFormatAsync returns false
```

## Monitoring and Reporting

### Dashboard Queries

**Recent Uploads**:
```sql
SELECT TOP 10 FileId, FileName, UploadDate
FROM isBOMImportFileLog
ORDER BY UploadDate DESC;
```

**Upload Count by Date**:
```sql
SELECT 
    CAST(UploadDate AS DATE) AS UploadDay,
    COUNT(*) AS FileCount
FROM isBOMImportFileLog
GROUP BY CAST(UploadDate AS DATE)
ORDER BY UploadDay DESC;
```

**Total Uploads**:
```sql
SELECT COUNT(*) AS TotalFiles
FROM isBOMImportFileLog;
```

**Search by FileName**:
```sql
SELECT FileId, FileName, UploadDate
FROM isBOMImportFileLog
WHERE FileName LIKE '%searchterm%'
ORDER BY UploadDate DESC;
```

## Maintenance

### Cleanup Old Logs

```sql
-- Delete logs older than 90 days
DELETE FROM isBOMImportFileLog
WHERE UploadDate < DATEADD(DAY, -90, GETDATE());
```

### Archive Logs

```sql
-- Archive to history table (create if needed)
SELECT * INTO isBOMImportFileLog_Archive
FROM isBOMImportFileLog
WHERE UploadDate < DATEADD(DAY, -30, GETDATE());

-- Delete archived records
DELETE FROM isBOMImportFileLog
WHERE UploadDate < DATEADD(DAY, -30, GETDATE());
```

## Testing

### Manual Testing

1. **Upload Valid File**:
   ```csharp
   var filePath = @"C:\path\to\file.xlsx";
   var result = await _fileImportService.ImportFileAsync(filePath);
   ```

2. **Check Database**:
   ```sql
   SELECT * FROM isBOMImportFileLog
   ORDER BY UploadDate DESC;
   ```

3. **Verify Log Entry**:
   - FileId is auto-generated
   - FileName matches uploaded file
   - UploadDate is current timestamp

### Query Test Data

```sql
-- View all uploads
SELECT FileId, FileName, UploadDate
FROM isBOMImportFileLog
ORDER BY UploadDate DESC;

-- Count by file name
SELECT FileName, COUNT(*) AS UploadCount
FROM isBOMImportFileLog
GROUP BY FileName
ORDER BY UploadCount DESC;
```

## Troubleshooting

### Common Issues

**Issue**: Log entries not created
- **Check**: Database connection string
- **Check**: Table exists in MAS_AML database
- **Solution**: Run `CreateImportBOMFileLogTable.sql`

**Issue**: Reference errors during build
- **Check**: Project references in Shared project
- **Solution**: Ensure Shared project references Domain project

**Issue**: File not found errors
- **Check**: File path validity
- **Solution**: Use absolute paths, verify file exists

**Issue**: Permission errors
- **Check**: Database permissions
- **Solution**: Ensure user has INSERT rights on isBOMImportFileLog table

## Implementation Checklist

- [x] Database table created (isBOMImportFileLog)
- [x] Entity class created (ImportBomFileLog)
- [x] Repository interface created
- [x] Repository implementation created
- [x] File import service updated
- [x] Dependency injection configured
- [x] Logging integrated
- [ ] Excel parsing logic (TODO)
- [ ] UI for file upload (TODO)
- [ ] Import history view (TODO)

## Next Steps

1. Implement Excel file parsing
2. Create BOM data extraction logic
3. Add UI component for file upload
4. Create import history view
5. Add file re-processing capability

## Related Documentation

- [DATABASE_CONNECTION_GUIDE.md](DATABASE_CONNECTION_GUIDE.md)
- [LOGGING_SYSTEM_GUIDE.md](LOGGING_SYSTEM_GUIDE.md)
- [SQL_CONNECTION_IMPLEMENTATION_SUMMARY.md](SQL_CONNECTION_IMPLEMENTATION_SUMMARY.md)

---

**Database**: MAS_AML  
**Table**: isBOMImportFileLog  
**Columns**: FileId, FileName, UploadDate  
**Status**: ? Implemented and Ready for Use
