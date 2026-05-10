# BOM Duplicate Detection - Quick Reference

## TL;DR
**BOMs are marked as duplicates if their ParentItemCode (BillNo) already exists in the BM_BillHeader table.**

## Key Query
```sql
SELECT COUNT(1) 
FROM BM_BillHeader 
WHERE BillNo = @BillNo
```

## Validation Logic
```
ParentItemCode in isBOMImportBills = BillNo in BM_BillHeader
If match found ? Status = "Duplicate"
```

## Files Modified
1. ? `ISageItemRepository.cs` - Added `BillExistsInBomHeaderAsync()` method
2. ? `SageItemRepository.cs` - Implemented BM_BillHeader query
3. ? `BomValidationService.cs` - Updated `IsDuplicateBomAsync()` to check BM_BillHeader

## What Changed
### Before:
```csharp
// Checked CI_Item table for parent item
var parentExists = await _sageItemRepository.ItemExistsAsync(parentItemCode);
```

### After:
```csharp
// Checks BM_BillHeader table for BillNo
var billExists = await _sageItemRepository.BillExistsInBomHeaderAsync(parentItemCode);
```

## Testing
```sql
-- 1. Check if duplicate detection works
SELECT ib.ParentItemCode, b.BillNo, ib.Status
FROM isBOMImportBills ib
LEFT JOIN BM_BillHeader b ON ib.ParentItemCode = b.BillNo
WHERE b.BillNo IS NOT NULL;

-- 2. View all duplicates
SELECT ParentItemCode, COUNT(*) as Count
FROM isBOMImportBills
WHERE Status = 'Duplicate'
GROUP BY ParentItemCode;
```

## Status Values
- **"Duplicate"** - BillNo exists in BM_BillHeader
- **"Validated"** - Component validated, ready for integration  
- **"NewBuyItem"** - Component needs to be created
- **"Failed"** - Validation failed

## Validation Message
```
"Duplicate BOM - Parent item already exists in BM_BillHeader table"
```

## Build Status
? **Build Successful** - All changes compile without errors
