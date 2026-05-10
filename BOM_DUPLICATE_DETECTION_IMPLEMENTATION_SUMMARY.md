# BOM Duplicate Detection - Implementation Summary

## Changes Made ?

### 1. Interface Update
**File:** `Aml.BOM.Import.Shared\Interfaces\ISageItemRepository.cs`

Added new method:
```csharp
/// <summary>
/// Checks if a BillNo exists in BM_BillHeader table (for duplicate BOM detection)
/// </summary>
Task<bool> BillExistsInBomHeaderAsync(string billNo);
```

### 2. Repository Implementation  
**File:** `Aml.BOM.Import.Infrastructure\Repositories\SageItemRepository.cs`

Implemented method:
```csharp
public async Task<bool> BillExistsInBomHeaderAsync(string billNo)
{
    const string sql = @"
        SELECT COUNT(1) 
        FROM BM_BillHeader 
        WHERE BillNo = @BillNo";
    
    using var connection = new SqlConnection(_connectionString);
    await connection.OpenAsync();
    
    using var command = new SqlCommand(sql, connection);
    command.Parameters.AddWithValue("@BillNo", billNo);
    
    var count = (int)(await command.ExecuteScalarAsync() ?? 0);
    return count > 0;
}
```

### 3. Validation Service Update
**File:** `Aml.BOM.Import.Infrastructure\Services\BomValidationService.cs`

**Updated `IsDuplicateBomAsync()` method:**

**Old Logic (WRONG):**
```csharp
// Check if parent exists in CI_Item (would indicate BOM already exists there)
var parentExists = await _sageItemRepository.ItemExistsAsync(parentItemCode);
if (parentExists)
{
    _logger.LogInformation("Duplicate BOM detected - Parent exists in Sage: {0}", parentItemCode);
    return true;
}
```

**New Logic (CORRECT):**
```csharp
// Check if parent item (BillNo) exists in BM_BillHeader table
// This indicates the BOM already exists in the system
var billExists = await _sageItemRepository.BillExistsInBomHeaderAsync(parentItemCode);
if (billExists)
{
    _logger.LogInformation("Duplicate BOM detected - BillNo exists in BM_BillHeader: {0}", parentItemCode);
    return true;
}
```

**Updated validation messages:**
- Changed: `"Duplicate BOM - Parent item already exists in system"`
- To: `"Duplicate BOM - Parent item already exists in BM_BillHeader table"`

## How It Works

### Data Flow
```
Excel Import
    ?
isBOMImportBills Table
    ?
Validation: Check ParentItemCode against BM_BillHeader.BillNo
    ?
    ?? Found in BM_BillHeader ? Status = "Duplicate"
    ?? Not found ? Check CI_Item for components
    ?? Component validation continues...
```

### Database Tables Involved

#### isBOMImportBills (Import staging)
```sql
CREATE TABLE isBOMImportBills (
    Id INT PRIMARY KEY,
    ParentItemCode NVARCHAR(50),    -- This is the BillNo to check
    ComponentItemCode NVARCHAR(50),  -- Component item
    Status NVARCHAR(50),             -- Can be 'Duplicate'
    ValidationMessage NVARCHAR(500)
    -- ... other fields
);
```

#### BM_BillHeader (Sage BOM header table)
```sql
-- Sage table structure (assumed)
CREATE TABLE BM_BillHeader (
    BillNo NVARCHAR(50) PRIMARY KEY,
    -- ... other fields
);
```

## SQL Verification Queries

### 1. Check for Duplicates (Your Original Query)
```sql
SELECT ib.ComponentItemCode, b.BillNo 
FROM isBOMImportBills ib
LEFT JOIN BM_BillHeader b ON ib.ParentItemCode = b.BillNo
WHERE ib.ParentItemCode IS NULL 
  AND b.BillNo IS NOT NULL;
```

### 2. View All Duplicate BOMs
```sql
SELECT 
    ib.ParentItemCode,
    ib.ParentDescription,
    COUNT(*) as ComponentCount,
    ib.Status,
    ib.ValidationMessage,
    ib.ImportFileName,
    ib.ImportDate
FROM isBOMImportBills ib
WHERE ib.Status = 'Duplicate'
GROUP BY 
    ib.ParentItemCode,
    ib.ParentDescription,
    ib.Status,
    ib.ValidationMessage,
    ib.ImportFileName,
    ib.ImportDate
ORDER BY ib.ImportDate DESC;
```

### 3. Find Which BOMs Are Duplicates
```sql
SELECT DISTINCT
    ib.ParentItemCode as ImportedBillNo,
    b.BillNo as ExistingBillNo,
    CASE 
        WHEN b.BillNo IS NOT NULL THEN 'Duplicate - Exists in Sage'
        ELSE 'New BOM'
    END as DuplicateStatus
FROM isBOMImportBills ib
LEFT JOIN BM_BillHeader b ON ib.ParentItemCode = b.BillNo
WHERE ib.ParentItemCode IS NOT NULL
ORDER BY DuplicateStatus DESC, ib.ParentItemCode;
```

### 4. Count Statistics
```sql
SELECT 
    Status,
    COUNT(DISTINCT ParentItemCode) as UniqueBOMs,
    COUNT(*) as TotalComponents
FROM isBOMImportBills
GROUP BY Status
ORDER BY Status;
```

### 5. Test Duplicate Detection
```sql
-- Test: Insert a test BOM in BM_BillHeader
INSERT INTO BM_BillHeader (BillNo) 
VALUES ('TEST-DUPLICATE-BOM-001');

-- Import a file with ParentItemCode = 'TEST-DUPLICATE-BOM-001'
-- Expected: All components marked as Duplicate

-- Verify:
SELECT * FROM isBOMImportBills 
WHERE ParentItemCode = 'TEST-DUPLICATE-BOM-001'
  AND Status = 'Duplicate';
  
-- Cleanup:
DELETE FROM BM_BillHeader WHERE BillNo = 'TEST-DUPLICATE-BOM-001';
```

## Validation Process

### Step-by-Step
1. **File Upload** - Excel file imported, records saved with Status = "New"
2. **Mark Duplicates** - `MarkDuplicateBillsAsync()` called
   - Groups bills by ParentItemCode
   - For each group, checks BM_BillHeader
   - If found, marks all components as "Duplicate"
3. **Component Validation** - Remaining "New" records validated
   - Check component in CI_Item
   - Set status: Validated, NewBuyItem, or Failed

### Status Transitions
```
Initial: "New"
    ?
Duplicate Check
    ?
    ?? BillNo in BM_BillHeader ? "Duplicate"
    ?? BillNo NOT in BM_BillHeader
        ?
        Component Validation
        ?
        ?? Component in CI_Item ? "Validated"
        ?? Component NOT in CI_Item ? "NewBuyItem"
        ?? Validation Error ? "Failed"
```

## Expected Behavior

### Scenario 1: New BOM (Not Duplicate)
```
ParentItemCode: "NEW-ASSEMBLY-001"
BM_BillHeader check: NOT FOUND
Result: Continue to component validation
```

### Scenario 2: Duplicate BOM
```
ParentItemCode: "EXISTING-ASSEMBLY-001"
BM_BillHeader check: FOUND
Result: 
  - Status = "Duplicate"
  - ValidationMessage = "Duplicate BOM - Parent item already exists in BM_BillHeader table"
  - ALL components for this parent marked as Duplicate
```

## Testing Checklist

- [x] Code compiles successfully
- [x] Interface method added
- [x] Repository implementation added
- [x] Validation service updated
- [x] Validation messages updated
- [x] Build passes
- [ ] Manual testing with known duplicate BillNo
- [ ] Verify UI shows correct duplicate count
- [ ] Verify validation messages display correctly

## Documentation Created

1. ? `BOM_DUPLICATE_DETECTION_LOGIC.md` - Detailed documentation
2. ? `BOM_DUPLICATE_DETECTION_QUICK_REFERENCE.md` - Quick reference
3. ? `BOM_DUPLICATE_DETECTION_IMPLEMENTATION_SUMMARY.md` - This file

## Next Steps for Testing

1. **Database Setup:**
   ```sql
   -- Ensure BM_BillHeader table exists and has data
   SELECT TOP 10 BillNo FROM BM_BillHeader;
   ```

2. **Import Test File:**
   - Create Excel file with ParentItemCode matching existing BillNo
   - Import and verify Status = "Duplicate"

3. **Verify in UI:**
   - Check "New BOMs" view statistics
   - Check "Duplicate BOMs" view shows correct records

4. **Log Verification:**
   - Check application logs for duplicate detection messages
   - Verify correct SQL queries being executed

## Rollback Plan (If Needed)

If issues occur, revert changes in:
1. `ISageItemRepository.cs` - Remove `BillExistsInBomHeaderAsync` method
2. `SageItemRepository.cs` - Remove implementation
3. `BomValidationService.cs` - Restore original `IsDuplicateBomAsync` logic

## Support

For issues or questions:
1. Check application logs in `%APPDATA%\Aml.BOM.Import\Logs`
2. Run SQL verification queries above
3. Review `BOM_DUPLICATE_DETECTION_LOGIC.md` for detailed explanation

---

**Implementation Date:** {Current Date}
**Build Status:** ? Successful
**Tests Required:** Manual testing with duplicate BillNo data
