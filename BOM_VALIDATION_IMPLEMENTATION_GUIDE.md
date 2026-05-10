# BOM Validation and Duplicate Detection - Implementation Guide

## Overview

This document describes the complete validation system that automatically validates BOM imports against Sage CI_Item table and detects duplicate BOMs.

## Features Implemented

### ? Automatic Validation
- **On Upload**: New BOM files are automatically validated after import
- **Pending BOMs**: All previously uploaded pending BOMs are validated
- **Re-validation**: User can trigger re-validation via Revalidate button
- **Duplicate Detection**: Automatically detects and marks duplicate BOMs

### ? File Upload Protection
- **Duplicate File Prevention**: Rejects files that were already imported
- **Filename Check**: Validates against `isBOMImportFileLog` table

### ? Duplicate BOM Detection
- **Parent Item Check**: Verifies if parent item exists in Sage CI_Item
- **Previous Import Check**: Checks if BOM exists in `isBOMImportBills`
- **Automatic Marking**: All components of duplicate BOMs are marked as 'Duplicate'

---

## Validation Workflow

```
File Upload
     ?
1. Check if filename already exists in isBOMImportFileLog
   - YES ? Reject file with error message
   - NO ? Continue
     ?
2. Import Excel data to isBOMImportBills (Status='New')
     ?
3. Automatic Validation Process:
   a. Mark Duplicate BOMs
      - Check ParentItemCode in Sage CI_Item
      - Check ParentItemCode in isBOMImportBills
      - Mark all components as 'Duplicate'
   
   b. Validate Each Component
      - Check ComponentItemCode in Sage CI_Item
      - Exists ? Status='Validated'
      - Not Exists ? Status='NewBuyItem' or 'NewMakeItem'
      - Invalid ? Status='Failed'
     ?
4. Validate All Pending BOMs (from previous uploads)
     ?
5. Return Results
   - Total records
   - Validated count
   - Duplicate count
   - New items count
   - Failed count
```

---

## Status Values

| Status | Description | When Set |
|--------|-------------|----------|
| **New** | Newly imported, awaiting validation | On import |
| **Validated** | Component exists in Sage, ready for integration | After validation (component found) |
| **Duplicate** | BOM already exists in system | Parent found in Sage or previous imports |
| **NewBuyItem** | Component not in Sage, identified as buy item | After validation (component not found) |
| **NewMakeItem** | Component not in Sage, identified as make item | After validation (component not found) |
| **Failed** | Validation failed (e.g., invalid quantity) | After validation (validation errors) |
| **Integrated** | Successfully integrated to Sage | After integration |

---

## Database Schema Changes

### isBOMImportBills Table
**Status Constraint Updated**:
```sql
CONSTRAINT CK_isBOMImportBills_Status CHECK (
    Status IN ('New', 'Validated', 'Integrated', 'NewBuyItem', 'NewMakeItem', 'Failed', 'Duplicate')
)
```

### Sage CI_Item Table (External)
**Expected Schema** (adjust based on actual Sage database):
```sql
CI_Item (
    ItemCode NVARCHAR(50),
    ItemCodeDesc NVARCHAR(255),
    ProcurementType CHAR(1), -- 'B' = Buy, 'M' = Make
    Category1 NVARCHAR(50),
    StandardUnitCost DECIMAL(18,4)
)
```

---

## Components Created/Updated

### 1. IBomValidationService Interface
**File**: `Aml.BOM.Import.Shared\Interfaces\IBomValidationService.cs`

**Methods**:
```csharp
// Validate single bill
Task<ValidationResult> ValidateBillAsync(BomImportBill bill);

// Validate entire import file
Task<ImportValidationResult> ValidateImportFileAsync(string fileName);

// Validate all pending BOMs
Task<ImportValidationResult> ValidateAllPendingAsync();

// Re-validate all pending
Task<ImportValidationResult> RevalidateAllPendingAsync();

// Check if file already imported
Task<bool> IsFileAlreadyImportedAsync(string fileName);

// Check if BOM is duplicate
Task<bool> IsDuplicateBomAsync(string parentItemCode, string bomNumber);

// Mark duplicate bills
Task<int> MarkDuplicateBillsAsync(string fileName);
```

**Result Classes**:
```csharp
// Single bill validation result
public class ValidationResult
{
    public bool IsValid { get; set; }
    public bool ParentExists { get; set; }
    public bool ComponentExists { get; set; }
    public string? ParentItemType { get; set; }
    public string? ComponentItemType { get; set; }
    public bool IsDuplicate { get; set; }
    public string? ValidationMessage { get; set; }
    public List<string> Warnings { get; set; }
    public List<string> Errors { get; set; }
}

// Import validation result
public class ImportValidationResult
{
    public int TotalRecords { get; set; }
    public int ValidatedRecords { get; set; }
    public int NewBuyItems { get; set; }
    public int NewMakeItems { get; set; }
    public int DuplicateBoms { get; set; }
    public int FailedRecords { get; set; }
    public List<string> Errors { get; set; }
    public List<string> Warnings { get; set; }
    public Dictionary<string, int> StatusSummary { get; set; }
}
```

### 2. ISageItemRepository Interface (Enhanced)
**File**: `Aml.BOM.Import.Shared\Interfaces\ISageItemRepository.cs`

**New Methods**:
```csharp
Task<bool> ItemExistsAsync(string itemCode);
Task<string?> GetItemTypeAsync(string itemCode);
Task<SageItemInfo?> GetItemInfoAsync(string itemCode);
Task<Dictionary<string, bool>> ItemsExistAsync(IEnumerable<string> itemCodes);
```

**SageItemInfo Class**:
```csharp
public class SageItemInfo
{
    public string ItemCode { get; set; }
    public string? Description { get; set; }
    public string? ItemType { get; set; } // Buy, Make
    public bool Exists { get; set; }
    public string? Category { get; set; }
    public decimal? StandardCost { get; set; }
}
```

### 3. SageItemRepository Implementation
**File**: `Aml.BOM.Import.Infrastructure\Repositories\SageItemRepository.cs`

**Features**:
- Direct SQL queries to Sage CI_Item table
- Batch item existence checking
- Item type determination (Buy/Make)
- Comprehensive error handling
- Logging integration

### 4. BomValidationService Implementation
**File**: `Aml.BOM.Import.Infrastructure\Services\BomValidationService.cs`

**Features**:
- Single bill validation
- Batch validation
- Duplicate detection
- Status management
- Comprehensive logging

### 5. FileImportService (Enhanced)
**File**: `Aml.BOM.Import.Infrastructure\Services\FileImportService.cs`

**Enhancements**:
- Duplicate file check before import
- Automatic validation after import
- Validation of all pending BOMs
- Enhanced result reporting

---

## Usage Examples

### 1. Upload and Validate New File

```csharp
// Inject service
private readonly IFileImportService _fileImportService;

// Import file (automatic validation included)
try
{
    var result = await _fileImportService.ImportFileAsync(@"C:\BOM\MyBOM.xlsx");
    
    dynamic info = result;
    Console.WriteLine($"Imported: {info.ImportedRecords} records");
    Console.WriteLine($"Validated: {info.ValidatedRecords}");
    Console.WriteLine($"Duplicates: {info.DuplicateBoms}");
    Console.WriteLine($"New Buy Items: {info.NewBuyItems}");
    Console.WriteLine($"Failed: {info.FailedRecords}");
}
catch (InvalidOperationException ex)
{
    // File already imported
    Console.WriteLine($"Error: {ex.Message}");
}
```

### 2. Manual Validation of Existing File

```csharp
private readonly IBomValidationService _validationService;

var result = await _validationService.ValidateImportFileAsync("MyBOM.xlsx");

Console.WriteLine($"Total Records: {result.TotalRecords}");
Console.WriteLine($"Validated: {result.ValidatedRecords}");
Console.WriteLine($"Duplicates: {result.DuplicateBoms}");
Console.WriteLine($"New Items: {result.NewBuyItems + result.NewMakeItems}");
```

### 3. Re-validate All Pending BOMs

```csharp
// User clicks "Revalidate" button
private readonly IBomValidationService _validationService;

var result = await _validationService.RevalidateAllPendingAsync();

Console.WriteLine($"Re-validated {result.TotalRecords} records");
Console.WriteLine($"Status Summary:");
foreach (var kvp in result.StatusSummary)
{
    Console.WriteLine($"  {kvp.Key}: {kvp.Value}");
}
```

### 4. Check if File Already Imported

```csharp
private readonly IBomValidationService _validationService;

var fileName = "MyBOM.xlsx";
var exists = await _validationService.IsFileAlreadyImportedAsync(fileName);

if (exists)
{
    MessageBox.Show($"File '{fileName}' has already been imported.");
}
```

---

## Validation Logic Details

### Parent Item Validation

```csharp
// Check in Sage CI_Item
var parentExists = await _sageItemRepository.ItemExistsAsync(parentItemCode);

if (parentExists)
{
    // Duplicate BOM - parent already in Sage
    Status = "Duplicate";
}
else
{
    // Check in previous imports
    var existingBills = await _billRepository.GetByParentItemCodeAsync(parentItemCode);
    if (existingBills.Any())
    {
        // Duplicate BOM - parent in previous import
        Status = "Duplicate";
    }
}
```

### Component Item Validation

```csharp
// Check in Sage CI_Item
var componentInfo = await _sageItemRepository.GetItemInfoAsync(componentItemCode);

if (componentInfo?.Exists == true)
{
    // Component exists in Sage
    Status = "Validated";
    ItemExists = true;
    ItemType = componentInfo.ItemType; // Buy or Make
}
else
{
    // Component not in Sage - new item needed
    Status = "NewBuyItem"; // or NewMakeItem based on logic
    ItemExists = false;
}
```

### Quantity Validation

```csharp
if (bill.Quantity <= 0)
{
    Status = "Failed";
    ValidationMessage = "Quantity must be greater than zero";
}
```

---

## Queries and Reports

### View Validation Summary

```sql
SELECT 
    Status,
    COUNT(*) AS RecordCount,
    COUNT(DISTINCT ImportFileName) AS FileCount
FROM isBOMImportBills
GROUP BY Status
ORDER BY RecordCount DESC;
```

### Find Duplicate BOMs

```sql
SELECT 
    ImportFileName,
    TabName,
    ParentItemCode,
    ParentDescription,
    COUNT(*) AS ComponentCount,
    MIN(ImportDate) AS FirstImported
FROM isBOMImportBills
WHERE Status = 'Duplicate'
GROUP BY ImportFileName, TabName, ParentItemCode, ParentDescription
ORDER BY FirstImported DESC;
```

### Find New Items Needed

```sql
SELECT DISTINCT
    ComponentItemCode,
    ComponentDescription,
    Status,
    COUNT(*) AS OccurrenceCount
FROM isBOMImportBills
WHERE ItemExists = 0 AND Status IN ('NewBuyItem', 'NewMakeItem')
GROUP BY ComponentItemCode, ComponentDescription, Status
ORDER BY Status, ComponentItemCode;
```

### View Pending BOMs

```sql
SELECT 
    ImportFileName,
    TabName,
    ParentItemCode,
    ComponentItemCode,
    Quantity,
    Status,
    ValidationMessage,
    ImportDate
FROM isBOMImportBills
WHERE Status = 'New'
ORDER BY ImportFileName, TabName, LineNumber;
```

### Validation Statistics by File

```sql
SELECT 
    ImportFileName,
    ImportDate,
    COUNT(*) AS TotalRecords,
    SUM(CASE WHEN Status = 'Validated' THEN 1 ELSE 0 END) AS Validated,
    SUM(CASE WHEN Status = 'Duplicate' THEN 1 ELSE 0 END) AS Duplicates,
    SUM(CASE WHEN Status IN ('NewBuyItem', 'NewMakeItem') THEN 1 ELSE 0 END) AS NewItems,
    SUM(CASE WHEN Status = 'Failed' THEN 1 ELSE 0 END) AS Failed
FROM isBOMImportBills
GROUP BY ImportFileName, ImportDate
ORDER BY ImportDate DESC;
```

---

## Error Handling

### File Already Imported

```csharp
try
{
    await _fileImportService.ImportFileAsync(filePath);
}
catch (InvalidOperationException ex)
{
    // ex.Message: "The file 'MyBOM.xlsx' has already been imported..."
    MessageBox.Show(ex.Message, "Duplicate File", MessageBoxButton.OK, MessageBoxImage.Warning);
}
```

### Sage Database Connection Failure

```csharp
// Validation service logs error and continues
// Items marked as not existing if Sage query fails
// Check logs for connection issues
```

### Validation Errors

```csharp
var result = await _validationService.ValidateImportFileAsync("MyBOM.xlsx");

if (result.Errors.Any())
{
    Console.WriteLine("Validation Errors:");
    foreach (var error in result.Errors)
    {
        Console.WriteLine($"  - {error}");
    }
}

if (result.Warnings.Any())
{
    Console.WriteLine("Validation Warnings:");
    foreach (var warning in result.Warnings)
    {
        Console.WriteLine($"  - {warning}");
    }
}
```

---

## Configuration

### Sage Database Connection

The system uses the same connection string as MAS_AML database.  
**Note**: Ensure Sage CI_Item table is accessible from the same SQL Server.

**Connection String**: 
```json
{
  "DatabaseConnectionString": "Server=localhost;Database=MAS_AML;Trusted_Connection=true;TrustServerCertificate=true;"
}
```

### Sage Column Mapping

Update `SageItemRepository.cs` if Sage CI_Item schema differs:

```csharp
// Current mapping (adjust as needed)
ItemCode         ? ItemCode
ItemCodeDesc     ? Description
ProcurementType  ? 'B' = Buy, 'M' = Make
Category1        ? Category
StandardUnitCost ? StandardCost
```

---

## Performance Considerations

### Batch Operations

**Efficient**:
```csharp
// Check multiple items at once
var itemCodes = bills.Select(b => b.ComponentItemCode).Distinct();
var existenceMap = await _sageItemRepository.ItemsExistAsync(itemCodes);
```

**Avoid**:
```csharp
// Individual checks in loop
foreach (var bill in bills)
{
    var exists = await _sageItemRepository.ItemExistsAsync(bill.ComponentItemCode);
}
```

### Large Imports

| Records | Validation Time (est) |
|---------|----------------------|
| 100 | <5 seconds |
| 1,000 | ~30 seconds |
| 10,000 | ~5 minutes |

**Optimization**: Batch validation runs in background, user can continue working.

---

## Testing Scenarios

### Test 1: Upload New File
1. Select Excel file with new BOM
2. Click Import
3. **Expected**: File imports, validation runs, status shows validated/new items

### Test 2: Upload Duplicate File
1. Upload file "BOM_001.xlsx"
2. Try to upload same file again
3. **Expected**: Error message "File has already been imported"

### Test 3: Duplicate BOM Detection
1. Upload BOM with ParentItemCode = "ASSY-001"
2. ParentItemCode exists in Sage
3. **Expected**: All components marked as "Duplicate"

### Test 4: New Item Detection
1. Upload BOM with ComponentItemCode not in Sage
2. **Expected**: Status = "NewBuyItem" or "NewMakeItem"

### Test 5: Re-validation
1. Upload several BOMs
2. Add new items to Sage CI_Item
3. Click "Revalidate"
4. **Expected**: Previously failed items now validated

---

## Troubleshooting

| Issue | Cause | Solution |
|-------|-------|----------|
| All items marked as "not exists" | Sage database not accessible | Check connection string, verify CI_Item table access |
| Duplicates not detected | Parent item code mismatch | Verify ParentItemCode column populated in Excel |
| Validation very slow | Large file, no batch operations | Check batch methods being used |
| File rejected incorrectly | Filename case mismatch | Filename comparison is case-insensitive |

---

## Next Steps

### UI Implementation (TODO)

1. **Pending BOMs View**:
   - Grid showing all pending BOMs
   - Status column with color coding
   - Revalidate button

2. **Validation Results Dialog**:
   - Show validation summary
   - List of duplicates
   - List of new items needed

3. **New Items Management**:
   - View for NewBuyItems
   - View for NewMakeItems
   - Ability to create items in Sage

---

**Status**: ? Fully Implemented  
**Database**: MAS_AML  
**Sage Integration**: CI_Item table  
**Build**: ? Successful
