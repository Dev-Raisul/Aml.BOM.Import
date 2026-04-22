# BOM Import Bills - Quick Reference

## Table Structure: isBOMImportBills

### Essential Columns (27 Total)

**Import Metadata** (4 columns)
- `ImportFileName` - Excel file name
- `ImportDate` - When imported
- `ImportWindowsUser` - Who imported
- `TabName` - Excel worksheet name

**Status** (3 columns)
- `Status` - New, Validated, Integrated, NewBuyItem, NewMakeItem, Failed
- `DateValidated` - When validated
- `DateIntegrated` - When integrated to Sage

**BOM Data** (16 columns)
- Parent: ItemCode, Description, BOMLevel, BOMNumber
- Component: ItemCode, Description, Quantity, UOM, Reference, Notes
- Additional: Category, Type, UnitCost, ExtendedCost
- Validation: ItemExists, ItemType, ValidationMessage

**Audit** (2 columns)
- `CreatedDate`, `ModifiedDate`

**Key** (1 column)
- `Id` (Primary Key, Auto-increment)

---

## Quick Start

### 1. Run Database Scripts

```sql
-- 1. Create database
USE master;
GO
-- Run: Database\CreateMAS_AML_Database.sql

-- 2. Create schema
USE MAS_AML;
GO
-- Run: Database\CreateMAS_AML_Schema.sql

-- 3. Create file log table
-- Run: Database\CreateImportBOMFileLogTable.sql

-- 4. Create BOM import table
-- Run: Aml.BOM.Import.Shared\Resources\Scripts\Tables\CreateisBOMImportBillsTable.sql
```

### 2. Import Excel File

```csharp
// Inject service
private readonly IFileImportService _fileImportService;

// Import file
var result = await _fileImportService.ImportFileAsync(@"C:\BOM\MyBOM.xlsx");

// Result contains:
// - FileId: Log entry ID
// - FileName: File name
// - ImportedRecords: Total records imported
// - Tabs: Number of tabs processed
// - Message: Success message
```

### 3. Query Imported Data

```csharp
// Get by file name
var records = await _bomBillRepository.GetByFileNameAsync("MyBOM.xlsx");

// Get by status
var newRecords = await _bomBillRepository.GetByStatusAsync("New");

// Get status summary
var summary = await _bomBillRepository.GetStatusSummaryAsync();
// Returns: Dictionary<string, int>
```

---

## Excel File Structure

### Required Column
- **Component/Item Code** (must have value)

### Recognized Column Names

| Field | Column Names |
|-------|-------------|
| Parent Item | "Parent Item", "Parent Item Code", "Parent Part" |
| Component | "Component", "Item Code", "Part Number" |
| Description | "Description", "Component Description" |
| Quantity | "Quantity", "Qty" |
| UOM | "UOM", "Unit", "Unit of Measure" |
| Level | "Level", "BOM Level" |
| BOM Number | "BOM Number", "BOM#", "BOM No" |

**Note**: Column matching is case-insensitive and tries multiple variations.

---

## Common Queries

### View Recent Imports
```sql
SELECT TOP 10 
    ImportFileName, TabName, ComponentItemCode, 
    Quantity, Status, ImportDate
FROM isBOMImportBills
ORDER BY ImportDate DESC;
```

### Count Records by File
```sql
SELECT 
    ImportFileName,
    COUNT(*) AS TotalRecords,
    COUNT(DISTINCT TabName) AS TabCount
FROM isBOMImportBills
GROUP BY ImportFileName;
```

### Status Breakdown
```sql
SELECT Status, COUNT(*) AS Count
FROM isBOMImportBills
GROUP BY Status
ORDER BY Count DESC;
```

### Find Missing Items
```sql
SELECT DISTINCT ComponentItemCode
FROM isBOMImportBills
WHERE ItemExists = 0
ORDER BY ComponentItemCode;
```

---

## Status Values

| Status | Meaning |
|--------|---------|
| **New** | Freshly imported, not validated |
| **Validated** | Passed validation |
| **Integrated** | Successfully integrated to Sage |
| **NewBuyItem** | Component is new buy item |
| **NewMakeItem** | Component is new make item |
| **Failed** | Import/validation/integration failed |

---

## Repository Methods

### Create
```csharp
// Single record
int id = await _bomBillRepository.CreateAsync(bill);

// Batch (uses transaction)
int count = await _bomBillRepository.CreateBatchAsync(bills);
```

### Read
```csharp
// By ID
var bill = await _bomBillRepository.GetByIdAsync(1);

// By file name
var bills = await _bomBillRepository.GetByFileNameAsync("MyBOM.xlsx");

// By status
var bills = await _bomBillRepository.GetByStatusAsync("New");

// By tab
var bills = await _bomBillRepository.GetByTabNameAsync("Sheet1");

// By file + tab
var bills = await _bomBillRepository.GetByFileAndTabAsync("MyBOM.xlsx", "Sheet1");

// By component
var bills = await _bomBillRepository.GetByComponentItemCodeAsync("PART-001");

// By parent
var bills = await _bomBillRepository.GetByParentItemCodeAsync("ASSY-001");

// Recent
var bills = await _bomBillRepository.GetRecentAsync(100);

// All
var bills = await _bomBillRepository.GetAllAsync();
```

### Update
```csharp
// Full update
await _bomBillRepository.UpdateAsync(bill);

// Status only
await _bomBillRepository.UpdateStatusAsync(
    id: 1, 
    status: "Validated",
    validatedDate: DateTime.Now);

// Validation fields
await _bomBillRepository.UpdateValidationAsync(
    id: 1,
    itemExists: true,
    itemType: "Buy",
    validationMessage: "Item found in Sage");

// Batch status update
await _bomBillRepository.UpdateBatchStatusAsync(
    ids: new[] { 1, 2, 3 },
    status: "Validated");
```

### Delete
```csharp
// Single record
bool deleted = await _bomBillRepository.DeleteAsync(1);

// All records from file
int count = await _bomBillRepository.DeleteByFileNameAsync("MyBOM.xlsx");
```

### Statistics
```csharp
// Count by status
int count = await _bomBillRepository.GetCountByStatusAsync("New");

// Count by file
int count = await _bomBillRepository.GetCountByFileNameAsync("MyBOM.xlsx");

// Status summary
var summary = await _bomBillRepository.GetStatusSummaryAsync();
// Example: { "New": 100, "Validated": 50, "Integrated": 25 }
```

---

## Validation & Error Handling

### File Validation
```csharp
var isValid = await _fileImportService.ValidateFileFormatAsync(filePath);
// Checks:
// - File exists
// - Extension (.xlsx or .xls)
// - Size (<50MB)
// - Valid Excel format
```

### Import Error Handling
```csharp
try
{
    var result = await _fileImportService.ImportFileAsync(filePath);
}
catch (FileNotFoundException ex)
{
    // File not found
}
catch (Exception ex)
{
    // Other errors (corrupted file, database issues, etc.)
}
```

---

## Performance Tips

### Batch Operations
? **Good**: Use batch insert for multiple records
```csharp
await _bomBillRepository.CreateBatchAsync(bills); // Single transaction
```

? **Avoid**: Loop with individual inserts
```csharp
foreach (var bill in bills)
{
    await _bomBillRepository.CreateAsync(bill); // Multiple transactions
}
```

### Large Files
| Records | Time (est) | Recommendation |
|---------|-----------|----------------|
| <1,000 | <2 sec | Normal processing |
| 1,000-10,000 | <30 sec | Monitor progress |
| >10,000 | >1 min | Consider chunking |

---

## Configuration

### File Size Limit
**Default**: 50 MB  
**Change**: Modify `maxFileSize` in `FileImportService.ValidateFileFormatAsync()`

### Supported File Types
- `.xlsx` (Excel 2007+)
- `.xls` (Excel 97-2003)

### Database Connection
**Location**: App settings (`appsettings.json`)
```json
{
  "DatabaseConnectionString": "Server=localhost;Database=MAS_AML;..."
}
```

---

## Troubleshooting

| Issue | Solution |
|-------|----------|
| No records imported | Check Excel has data rows with ComponentItemCode |
| Wrong columns mapped | Ensure header is row 1, use recognized column names |
| Batch insert fails | Verify table exists, check permissions |
| File too large | Split file or increase size limit |
| Corrupted file | Re-export from original source |

---

## Indexes

8 indexes for optimal performance:
1. ImportFileName
2. ImportDate
3. Status
4. TabName
5. ComponentItemCode
6. ParentItemCode
7. BOMNumber
8. Composite (ImportFileName + Status + ImportDate)

---

## Integration with Other Tables

### isBOMImportFileLog
- Tracks file upload
- Links via ImportFileName

### SageItems
- Validates ComponentItemCode exists
- Determines ItemType (Buy/Make)

### NewBuyItems / NewMakeItems
- Created when ItemExists = false
- Links via ComponentItemCode

---

## File Locations

**SQL Scripts**:
- `Aml.BOM.Import.Shared\Resources\Scripts\Tables\CreateisBOMImportBillsTable.sql`

**Entity**:
- `Aml.BOM.Import.Domain\Entities\BomImportBill.cs`

**Repository Interface**:
- `Aml.BOM.Import.Shared\Interfaces\IBomImportBillRepository.cs`

**Repository Implementation**:
- `Aml.BOM.Import.Infrastructure\Repositories\BomImportBillRepository.cs`

**Service**:
- `Aml.BOM.Import.Infrastructure\Services\FileImportService.cs`

**Documentation**:
- `BOM_IMPORT_BILLS_IMPLEMENTATION_GUIDE.md`

---

**Status**: ? Fully Implemented  
**Database**: MAS_AML  
**Table**: isBOMImportBills (27 columns, 8 indexes)  
**Package**: ClosedXML 0.102.3 (Excel parsing)
