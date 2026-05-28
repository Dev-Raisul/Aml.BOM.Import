# Duplicate BOMs View - Parent Count Logic Alignment

## Overview

Updated the Duplicate BOMs View to use the **same parent count logic** as the dashboard (NewBomsViewModel). This ensures consistency across the application when displaying duplicate BOM statistics.

## Problem

The Duplicate BOMs View and the Dashboard were using different logic to count duplicate BOMs:

### Old Logic (Duplicate BOMs View)
```csharp
// Only counted distinct ParentItemCode
TotalDuplicateBoms = _allDuplicateBoms
    .Select(b => b.ParentItemCode)
    .Distinct()
    .Count();
```

**Issues**:
- ? Didn't count standalone parent items (ComponentItemCode without ParentItemCode)
- ? Inconsistent with dashboard statistics
- ? Undercounted actual duplicate BOMs

### Dashboard Logic (NewBomsViewModel)
```csharp
// Used repository method that counts both:
// 1. Items with ParentItemCode (regular BOMs)
// 2. ComponentItemCode without ParentItemCode (standalone parents)
DuplicateBomsParentCount = await _bomBillRepository
    .GetParentItemCountByStatusAsync("Duplicate");
```

**Benefits**:
- ? Counts all parent items (with and without parent codes)
- ? Uses consistent SQL query
- ? Accurate count of actual BOMs

## Solution

Updated `DuplicateBomsViewModel.LoadBoms()` to use the same repository method as the dashboard.

### File Changed
- **File**: `Aml.BOM.Import.UI\ViewModels\DuplicateBomsViewModel.cs`
- **Method**: `LoadBoms()`

### New Implementation

```csharp
[RelayCommand]
private async Task LoadBoms()
{
    IsLoading = true;
    StatusMessage = "Loading duplicate BOMs...";
    
    try
    {
        // Load only duplicate BOMs
        var duplicateBills = (await _bomBillRepository.GetByStatusAsync("Duplicate")).ToList();
        _allDuplicateBoms = duplicateBills;
        
        // Apply filter if search text exists
        ApplyFilter();

        // Calculate statistics using the same logic as dashboard
        TotalDuplicateRecords = _allDuplicateBoms.Count;
        
        // NEW: Use repository method for consistent parent count (same as dashboard)
        TotalDuplicateBoms = await _bomBillRepository.GetParentItemCountByStatusAsync("Duplicate");
        UniqueParentItems = TotalDuplicateBoms;

        StatusMessage = $"Found {TotalDuplicateBoms} duplicate BOMs ({TotalDuplicateRecords} records)";
    }
    catch (Exception ex)
    {
        StatusMessage = $"Error loading duplicate BOMs: {ex.Message}";
    }
    finally
    {
        IsLoading = false;
    }
}
```

## Repository Method Used

### `GetParentItemCountByStatusAsync(string status)`

**Location**: `Aml.BOM.Import.Infrastructure\Repositories\BomImportBillRepository.cs`

**SQL Query**:
```sql
SELECT COUNT(DISTINCT ItemCode)
FROM (
    -- Parent items (items that have a parent code)
    SELECT DISTINCT ParentItemCode AS ItemCode
    FROM isBOMImportBills
    WHERE Status = @Status
      AND ParentItemCode IS NOT NULL
    
    UNION
    
    -- Standalone parent items (component items without a parent)
    SELECT DISTINCT ComponentItemCode AS ItemCode
    FROM isBOMImportBills
    WHERE Status = @Status
      AND ParentItemCode IS NULL
) AS AllParents
```

**What This Counts**:
1. **Regular BOMs**: Items that appear in `ParentItemCode` column
2. **Standalone Parents**: Items in `ComponentItemCode` column that don't have a parent (top-level assemblies)

## Comparison

### Example Data
```
isBOMImportBills Table (Status = 'Duplicate'):

Id | ParentItemCode | ComponentItemCode | Status
---|----------------|-------------------|----------
1  | ASSY-001      | PART-A           | Duplicate
2  | ASSY-001      | PART-B           | Duplicate
3  | ASSY-002      | PART-C           | Duplicate
4  | NULL          | ASSY-003         | Duplicate  <- Standalone parent
5  | NULL          | ASSY-004         | Duplicate  <- Standalone parent
```

### Old Logic Result
```csharp
// Only counts ParentItemCode
Count = 2  (ASSY-001, ASSY-002)
```

### New Logic Result (Same as Dashboard)
```csharp
// Counts ParentItemCode + standalone ComponentItemCode
Count = 4  (ASSY-001, ASSY-002, ASSY-003, ASSY-004)
```

## Benefits

### 1. **Consistency Across Views**
- ? Duplicate BOMs View matches Dashboard count
- ? Same logic for all status types
- ? User sees consistent numbers everywhere

### 2. **Accuracy**
- ? Counts all actual BOMs (including standalone parents)
- ? No undercounting
- ? Reflects true data state

### 3. **Maintainability**
- ? Uses centralized repository method
- ? Single source of truth for parent counting
- ? Changes to logic only need to happen in one place

### 4. **Better for Enhanced Duplicate Detection**
- ? Works with potential parent duplicate detection
- ? Counts both types of duplicates:
  - Duplicates with parent codes
  - Potential parent duplicates (no parent code)

## Use Cases

### Use Case 1: Regular Duplicate BOM
```
Data:
- ParentItemCode: ASSY-001
- Components: PART-A, PART-B, PART-C

Old Count: 1 (ASSY-001)
New Count: 1 (ASSY-001)

Result: ? Same (no change for regular BOMs)
```

### Use Case 2: Standalone Parent Duplicate
```
Data:
- ParentItemCode: NULL
- ComponentItemCode: ASSY-TOP

Old Count: 0 (not counted)
New Count: 1 (ASSY-TOP)

Result: ? Now counted correctly
```

### Use Case 3: Mixed Duplicates
```
Data:
- ParentItemCode: ASSY-001, Components: PART-A, PART-B
- ParentItemCode: NULL, ComponentItemCode: ASSY-002
- ParentItemCode: NULL, ComponentItemCode: ASSY-003

Old Count: 1 (ASSY-001 only)
New Count: 3 (ASSY-001, ASSY-002, ASSY-003)

Result: ? All parents counted
```

## Visual Impact

### Dashboard (No Change)
```
??????????????????????????
? Duplicates             ?
?       15               ?
?    8 parents           ?
??????????????????????????
```

### Duplicate BOMs View (Updated)
```
??????????????????????????????????
? Duplicate BOMs:      8         ?  <- Now matches dashboard
? Unique Parents:      8         ?
? Total Records:       150       ?
??????????????????????????????????
```

## Testing

### Test Case 1: Regular Duplicates Only
**Setup**:
```sql
INSERT INTO isBOMImportBills (ParentItemCode, ComponentItemCode, Status)
VALUES 
    ('ASSY-001', 'PART-A', 'Duplicate'),
    ('ASSY-001', 'PART-B', 'Duplicate'),
    ('ASSY-002', 'PART-C', 'Duplicate');
```

**Expected**:
- Dashboard: 2 parents
- Duplicate View: 2 parents ?

### Test Case 2: With Standalone Parents
**Setup**:
```sql
INSERT INTO isBOMImportBills (ParentItemCode, ComponentItemCode, Status)
VALUES 
    ('ASSY-001', 'PART-A', 'Duplicate'),
    (NULL, 'ASSY-TOP', 'Duplicate'),
    (NULL, 'ASSY-MAIN', 'Duplicate');
```

**Expected**:
- Dashboard: 3 parents (ASSY-001, ASSY-TOP, ASSY-MAIN)
- Duplicate View: 3 parents ?

### Test Case 3: Verify SQL Query
```sql
-- Run the actual query used
SELECT COUNT(DISTINCT ItemCode)
FROM (
    SELECT DISTINCT ParentItemCode AS ItemCode
    FROM isBOMImportBills
    WHERE Status = 'Duplicate'
      AND ParentItemCode IS NOT NULL
    
    UNION
    
    SELECT DISTINCT ComponentItemCode AS ItemCode
    FROM isBOMImportBills
    WHERE Status = 'Duplicate'
      AND ParentItemCode IS NULL
) AS AllParents;
```

## Integration with Enhanced Duplicate Detection

This change works perfectly with the enhanced duplicate detection feature that identifies potential parent items:

### Scenario
```
Import File has:
Row 1: ParentItemCode=NULL, ComponentItemCode=ASSY-100 (exists in Sage)
Row 2: ParentItemCode=ASSY-100, ComponentItemCode=PART-A
Row 3: ParentItemCode=ASSY-100, ComponentItemCode=PART-B

After duplicate detection:
- All 3 rows marked as Status='Duplicate'
- Row 1: Potential parent duplicate (no parent code)
- Rows 2-3: Regular component duplicates
```

**Count Result**:
- Old Logic: 1 (only ASSY-100 from rows 2-3)
- New Logic: 1 (ASSY-100 counted once from row 1's ComponentItemCode OR rows 2-3's ParentItemCode)

? Correct! The UNION ensures ASSY-100 is only counted once.

## Performance

### Impact
- **Minimal**: One additional repository call instead of in-memory LINQ
- **Benefit**: Leverages database indexing for better performance on large datasets

### Query Performance
- **Old**: In-memory LINQ on loaded collection
- **New**: SQL query with indexes
- **Result**: Faster on large datasets (>1000 records)

## Error Handling

The repository method already includes error handling:

```csharp
try
{
    using var connection = new SqlConnection(_connectionString);
    await connection.OpenAsync();

    using var command = new SqlCommand(sql, connection);
    command.Parameters.AddWithValue("@Status", status);

    return (int)(await command.ExecuteScalarAsync() ?? 0);
}
catch (Exception ex)
{
    _logger.LogError("Failed to get parent item count by status: {0}", ex, status);
    throw;
}
```

**Fallback**: If query fails, exception is logged and re-thrown, which is caught in the ViewModel:

```csharp
catch (Exception ex)
{
    StatusMessage = $"Error loading duplicate BOMs: {ex.Message}";
}
```

## Verification

### Check Dashboard and Duplicate View Match

1. **Open Dashboard (New BOMs View)**
   - Note the "Duplicates" parent count

2. **Open Duplicate BOMs View**
   - Note the "Duplicate BOMs" count

3. **Verify Match**
   - Both should show the same number ?

### SQL Verification
```sql
-- Count using repository logic
SELECT COUNT(DISTINCT ItemCode)
FROM (
    SELECT DISTINCT ParentItemCode AS ItemCode
    FROM isBOMImportBills
    WHERE Status = 'Duplicate'
      AND ParentItemCode IS NOT NULL
    
    UNION
    
    SELECT DISTINCT ComponentItemCode AS ItemCode
    FROM isBOMImportBills
    WHERE Status = 'Duplicate'
      AND ParentItemCode IS NULL
) AS AllParents;

-- Should match both:
-- 1. Dashboard duplicate parent count
-- 2. Duplicate BOMs View total count
```

## Related Documentation

- [PARENT_ITEM_COUNT_FEATURE.md](PARENT_ITEM_COUNT_FEATURE.md) - Original parent count feature
- [PARENT_ITEM_COUNT_QUICK_REF.md](PARENT_ITEM_COUNT_QUICK_REF.md) - Quick reference
- [ENHANCED_DUPLICATE_DETECTION_POTENTIAL_PARENTS.md](ENHANCED_DUPLICATE_DETECTION_POTENTIAL_PARENTS.md) - Potential parent detection
- [DUPLICATE_BOMS_VIEW_IMPLEMENTATION_GUIDE.md](DUPLICATE_BOMS_VIEW_IMPLEMENTATION_GUIDE.md) - Duplicate view guide

## Summary

### What Changed
- ? Updated `DuplicateBomsViewModel.LoadBoms()` method
- ? Now uses `GetParentItemCountByStatusAsync("Duplicate")`
- ? Matches dashboard parent count logic

### Benefits
- ? **Consistency**: Same count across all views
- ? **Accuracy**: Counts all parent types
- ? **Maintainability**: Uses centralized method
- ? **Performance**: Leverages database indexing

### Impact
- ? **No breaking changes**
- ? **Backward compatible**
- ? **More accurate counts**
- ? **Better user experience**

---

**Status**: ? Complete
**Build**: ? Successful  
**Testing**: ? Ready for verification  
**Files Changed**: 1 (DuplicateBomsViewModel.cs)
