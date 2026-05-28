# Duplicate BOMs View - Parent Count Alignment - Quick Reference

## What Was Changed

Updated the Duplicate BOMs View to use the **same parent count logic** as the Dashboard for consistency.

## The Problem

**Before**: Different logic in different views
```
Dashboard:     Uses GetParentItemCountByStatusAsync() ? 8 parents
Duplicate View: Uses LINQ Count(ParentItemCode) ? 5 parents
```
? Inconsistent numbers confuse users

## The Solution

**After**: Same logic everywhere
```
Dashboard:     Uses GetParentItemCountByStatusAsync() ? 8 parents
Duplicate View: Uses GetParentItemCountByStatusAsync() ? 8 parents
```
? Consistent numbers across all views

## Code Change

**File**: `Aml.BOM.Import.UI\ViewModels\DuplicateBomsViewModel.cs`

**Old**:
```csharp
// Only counted ParentItemCode
TotalDuplicateBoms = _allDuplicateBoms
    .Select(b => b.ParentItemCode)
    .Distinct()
    .Count();
```

**New**:
```csharp
// Use repository method (same as dashboard)
TotalDuplicateBoms = await _bomBillRepository
    .GetParentItemCountByStatusAsync("Duplicate");
```

## What Gets Counted

### Both Types of Parent Items
1. **Regular BOMs**: Items in `ParentItemCode` column
2. **Standalone Parents**: Items in `ComponentItemCode` without parent

### Example
```
Row 1: ParentItemCode=ASSY-001, ComponentItemCode=PART-A
Row 2: ParentItemCode=NULL, ComponentItemCode=ASSY-002

Old Count: 1 (ASSY-001 only)
New Count: 2 (ASSY-001, ASSY-002) ?
```

## SQL Query Used

```sql
SELECT COUNT(DISTINCT ItemCode)
FROM (
    -- Parent items with parent codes
    SELECT DISTINCT ParentItemCode AS ItemCode
    FROM isBOMImportBills
    WHERE Status = 'Duplicate'
      AND ParentItemCode IS NOT NULL
    
    UNION
    
    -- Standalone parent items (no parent code)
    SELECT DISTINCT ComponentItemCode AS ItemCode
    FROM isBOMImportBills
    WHERE Status = 'Duplicate'
      AND ParentItemCode IS NULL
) AS AllParents
```

## Benefits

? **Consistency** - Same count in Dashboard and Duplicate View
? **Accuracy** - Counts all parent types
? **Centralized** - Uses repository method
? **Performance** - Database query instead of LINQ

## Verification

### Quick Test
```sql
-- Run this query
SELECT COUNT(DISTINCT ItemCode)
FROM (
    SELECT DISTINCT ParentItemCode AS ItemCode
    FROM isBOMImportBills
    WHERE Status = 'Duplicate' AND ParentItemCode IS NOT NULL
    UNION
    SELECT DISTINCT ComponentItemCode AS ItemCode
    FROM isBOMImportBills
    WHERE Status = 'Duplicate' AND ParentItemCode IS NULL
) AS AllParents;
```

**Result should match**:
- Dashboard "Duplicates" parent count
- Duplicate View "Duplicate BOMs" count

## Visual Result

### Before (Inconsistent)
```
Dashboard:         Duplicate View:
????????????????   ????????????????
? Duplicates   ?   ? Duplicate    ?
?      15      ?   ? BOMs: 10     ? ? Different!
?   8 parents  ?   ????????????????
????????????????
```

### After (Consistent)
```
Dashboard:         Duplicate View:
????????????????   ????????????????
? Duplicates   ?   ? Duplicate    ?
?      15      ?   ? BOMs: 8      ? ? Same!
?   8 parents  ?   ????????????????
????????????????
```

## Related Features

- **Parent Count Feature**: Shows parent counts in Dashboard
- **Enhanced Duplicate Detection**: Detects potential parent duplicates
- **Duplicate BOMs View**: Lists all duplicate BOMs

## Testing Checklist

- [ ] Import file with regular duplicates
- [ ] Import file with standalone parent duplicates
- [ ] Check Dashboard duplicate parent count
- [ ] Check Duplicate BOMs View count
- [ ] Verify both match ?

## Files Changed

1. ? `DuplicateBomsViewModel.cs` - Updated LoadBoms() method

**Total Files**: 1  
**Build Status**: ? Successful  
**Ready for Testing**: ? Yes

---

**Status**: ? Complete  
**Impact**: Consistency improvement  
**Risk**: Low (non-breaking change)
