# BOM Duplicate Detection Logic

## Overview
The BOM import system includes duplicate detection logic to prevent importing BOMs that already exist in the Sage system. This document explains how duplicate detection works.

## Duplicate Detection Criteria

### Primary Check: BM_BillHeader Table
The main duplicate detection check queries the **BM_BillHeader** table to see if a BOM already exists in Sage.

**SQL Query:**
```sql
SELECT ib.ComponentItemCode, b.BillNo 
FROM isBOMImportBills ib
LEFT JOIN BM_BillHeader b ON ib.ComponentItemCode = b.BillNo
WHERE ib.ParentItemCode IS NULL 
  AND b.BillNo IS NOT NULL
```

**Key Logic:**
- The `ParentItemCode` field in `isBOMImportBills` represents the **BillNo** in Sage
- If a record with matching `BillNo` exists in `BM_BillHeader`, it's considered a duplicate
- ALL components under that parent item are marked as duplicates

### Secondary Check: Previous Imports
If the BillNo doesn't exist in BM_BillHeader, the system checks previous imports to prevent duplicate uploads within the import system itself.

## Implementation Details

### 1. Repository Method
**File:** `Aml.BOM.Import.Infrastructure\Repositories\SageItemRepository.cs`

```csharp
public async Task<bool> BillExistsInBomHeaderAsync(string billNo)
{
    const string sql = @"
        SELECT COUNT(1) 
        FROM BM_BillHeader 
        WHERE BillNo = @BillNo";
    
    // Returns true if BillNo exists in BM_BillHeader
}
```

### 2. Validation Service
**File:** `Aml.BOM.Import.Infrastructure\Services\BomValidationService.cs`

```csharp
public async Task<bool> IsDuplicateBomAsync(string parentItemCode, string bomNumber)
{
    // Step 1: Check BM_BillHeader table
    var billExists = await _sageItemRepository.BillExistsInBomHeaderAsync(parentItemCode);
    if (billExists)
    {
        return true; // Duplicate found in Sage
    }
    
    // Step 2: Check previous imports
    var existingBills = await _billRepository.GetByParentItemCodeAsync(parentItemCode);
    var hasDuplicate = existingBills.Any(b => b.Status != "Duplicate");
    
    return hasDuplicate;
}
```

### 3. Marking Duplicates
**Method:** `MarkDuplicateBillsAsync(string fileName)`

**Process:**
1. Get all bills from the imported file
2. Group bills by `ParentItemCode`
3. For each parent group:
   - Check if it's a duplicate using `IsDuplicateBomAsync`
   - If duplicate, mark ALL bills in that group with status = "Duplicate"
   - Update validation message: "Duplicate BOM - Parent item already exists in BM_BillHeader table"

## Validation Flow

### On File Import
```
1. File is uploaded and parsed
   ?
2. All records saved with Status = "New"
   ?
3. MarkDuplicateBillsAsync() called
   ?
4. For each unique ParentItemCode:
   - Check BM_BillHeader table
   - If exists ? Mark all components as "Duplicate"
   ?
5. Remaining "New" records validated against CI_Item
```

### Status After Validation
- **"Duplicate"** - Parent exists in BM_BillHeader or previous import
- **"Validated"** - Component exists in CI_Item, ready for integration
- **"NewBuyItem"** - Component doesn't exist in CI_Item, needs creation
- **"Failed"** - Validation errors

## Database Schema

### isBOMImportBills Table
```sql
- ParentItemCode NVARCHAR(50)    -- This is the BillNo to check in BM_BillHeader
- ComponentItemCode NVARCHAR(50) -- The component item
- Status NVARCHAR(50)            -- Can be 'Duplicate'
- ValidationMessage NVARCHAR(500) -- Contains duplicate message
```

### BM_BillHeader Table (Sage)
```sql
- BillNo NVARCHAR(50) -- Primary key for BOMs in Sage
```

## Example Scenario

### Excel File Contains:
```
ParentItemCode | ComponentItemCode | Quantity
ABC-001       | COMP-001          | 2
ABC-001       | COMP-002          | 5
ABC-001       | COMP-003          | 1
```

### Duplicate Detection:
1. Check: Does `ABC-001` exist in `BM_BillHeader.BillNo`?
2. **If YES:**
   - All 3 records marked as "Duplicate"
   - ValidationMessage = "Duplicate BOM - Parent item already exists in BM_BillHeader table"
3. **If NO:**
   - Proceed to validate each component against CI_Item

## User Interface Impact

### New BOMs View
- Shows count of duplicate BOMs
- Statistics display duplicate count

### Duplicate BOMs View
- Displays all records with Status = "Duplicate"
- Grouped by ParentItemCode
- Shows validation message explaining why it's duplicate

## Testing Duplicate Detection

### Test Case 1: Duplicate in Sage
```sql
-- Insert test BOM in BM_BillHeader
INSERT INTO BM_BillHeader (BillNo) VALUES ('TEST-BOM-001');

-- Import Excel with ParentItemCode = 'TEST-BOM-001'
-- Expected: All components marked as Duplicate
```

### Test Case 2: Duplicate in Previous Import
```sql
-- Import file once (creates records with ParentItemCode = 'NEW-BOM-001')
-- Import same file again
-- Expected: Second import marked as Duplicate
```

### Verification Query
```sql
-- Check duplicate detection
SELECT 
    ParentItemCode,
    COUNT(*) as ComponentCount,
    Status,
    ValidationMessage
FROM isBOMImportBills
WHERE Status = 'Duplicate'
GROUP BY ParentItemCode, Status, ValidationMessage;
```

## Important Notes

1. **ParentItemCode = BillNo**: The ParentItemCode in import bills represents the BillNo in Sage's BM_BillHeader table

2. **All Components Marked**: When a duplicate BOM is detected, ALL component items under that parent are marked as duplicates

3. **Non-Blocking**: Duplicate detection doesn't stop the import process; it marks duplicates and continues validating other records

4. **Revalidation**: Running revalidation will re-check BM_BillHeader table for any newly integrated BOMs

## Configuration

No configuration required. Duplicate detection is automatic and uses:
- Same database connection as Sage queries
- Standard SQL queries against BM_BillHeader table

## Troubleshooting

### Issue: False Positives (Valid BOMs marked as duplicate)
**Check:**
- Verify BM_BillHeader table structure
- Confirm ParentItemCode matches BillNo format
- Check database connection string

### Issue: False Negatives (Duplicates not detected)
**Check:**
- Verify ParentItemCode is populated in Excel
- Check BM_BillHeader table has correct BillNo values
- Review validation service logs

## Related Files
- `ISageItemRepository.cs` - Interface definition
- `SageItemRepository.cs` - Implementation with BM_BillHeader query
- `BomValidationService.cs` - Duplicate detection logic
- `FileImportService.cs` - Import and validation orchestration

## Future Enhancements
1. Option to override duplicate detection
2. Detailed duplicate comparison report
3. Ability to update existing BOMs instead of rejecting
4. Duplicate detection settings in UI
