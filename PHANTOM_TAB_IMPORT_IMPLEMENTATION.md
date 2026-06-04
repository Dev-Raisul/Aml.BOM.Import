# Phantom Tab Import Implementation

## Overview

When importing a BOM Excel file, if a worksheet/tab is named "Phantom", all component items that don't have a parent (meaning they are parent items themselves) will automatically have their `ProductType` set to 'P' (Phantom), regardless of what's in the Excel file.

---

## Business Logic

### Phantom Tab Definition

A tab is considered a "Phantom" tab when:
- The worksheet name equals "Phantom" (case-insensitive)
- Examples: "Phantom", "phantom", "PHANTOM", "PhAnToM" all qualify

### Parent Item Identification

A record is considered a parent item when:
- `ParentItemCode` is NULL or empty/whitespace
- These are top-level items in the BOM structure

### Phantom Type Assignment

For items in a Phantom tab that are parent items:
- `ProductType` is automatically set to 'P'
- This overrides any value that may exist in the Excel file
- Component items (those with a ParentItemCode) retain their original ProductType

---

## Implementation Details

### File Modified

**File**: `Aml.BOM.Import.Infrastructure\Services\FileImportService.cs`  
**Method**: `ParseWorksheet`

### Changes Made

1. **Tab Detection**: Added check at the beginning of `ParseWorksheet` method
```csharp
var isPhantomTab = tabName.Trim().Equals("Phantom", StringComparison.OrdinalIgnoreCase);
```

2. **Logging**: Added informational log when processing Phantom tab
```csharp
if (isPhantomTab)
{
    _logger.LogInformation("Processing Phantom tab - parent items will be set to ProductType 'P'");
}
```

3. **ProductType Override**: After creating the `BomImportBill` object, check if it's a parent item in a Phantom tab
```csharp
if (isPhantomTab && string.IsNullOrWhiteSpace(bill.ParentItemCode))
{
    bill.ProductType = "P";
    _logger.LogDebug("Set ProductType='P' for parent item {0} in Phantom tab", bill.ComponentItemCode);
}
```

---

## Example Scenarios

### Scenario 1: Phantom Tab with Parent Items

**Excel Tab**: "Phantom"

| Parent Item | Component Item | Product Type (Excel) |
|-------------|----------------|---------------------|
| (empty)     | PHANTOM-001    | F                   |
| (empty)     | PHANTOM-002    | (empty)             |
| PHANTOM-001 | PART-ABC       | F                   |

**Result in Database**:
- PHANTOM-001: ProductType = 'P' ? (overridden from 'F')
- PHANTOM-002: ProductType = 'P' ? (set to 'P')
- PART-ABC: ProductType = 'F' (unchanged, has parent)

### Scenario 2: Regular Tab with Parent Items

**Excel Tab**: "Standard"

| Parent Item | Component Item | Product Type (Excel) |
|-------------|----------------|---------------------|
| (empty)     | ITEM-001       | F                   |
| (empty)     | ITEM-002       | M                   |
| ITEM-001    | PART-XYZ       | F                   |

**Result in Database**:
- ITEM-001: ProductType = 'F' (not a Phantom tab, use Excel value)
- ITEM-002: ProductType = 'M' (not a Phantom tab, use Excel value)
- PART-XYZ: ProductType = 'F' (unchanged)

### Scenario 3: Mixed Phantom Tab

**Excel Tab**: "PHANTOM" (uppercase)

| Parent Item | Component Item | Product Type (Excel) |
|-------------|----------------|---------------------|
| (empty)     | PHANTOM-ASSY   | M                   |
| PHANTOM-ASSY| SCREW-M6       | F                   |
| PHANTOM-ASSY| WASHER-M6      | F                   |

**Result in Database**:
- PHANTOM-ASSY: ProductType = 'P' ? (parent in Phantom tab)
- SCREW-M6: ProductType = 'F' (component, Excel value retained)
- WASHER-M6: ProductType = 'F' (component, Excel value retained)

---

## Validation Impact

### Phantom Item Validation

Since the ProductType is set to 'P' during import, the validation service will recognize these items as phantoms:

```csharp
// In BomValidationService.ValidateBillAsync()
string componentType = bill.Type?.Trim().ToUpper() ?? "";
bool isPhantom = componentType == "P" || componentType == "PHANTOM";

if (isPhantom)
{
    // Auto-validate - no CI_Item check needed
    bill.Status = "Validated";
    bill.ItemType = "Phantom";
    bill.ItemExists = true;
    bill.ValidationMessage = "Phantom item - automatically validated";
    return result;
}
```

**Benefits**:
1. Phantom parent items don't appear in "New Buy Items"
2. No unnecessary CI_Item lookups
3. Automatically validated during import processing
4. Correct classification from the start

---

## Workflow

### Complete Import Flow for Phantom Tab

```
1. User imports Excel file with "Phantom" tab
   ?
2. FileImportService.ParseWorksheet() detects tab name
   ?
3. For each row:
   a. Parse all columns from Excel
   b. Check if ParentItemCode is NULL/empty
   c. If yes (is parent) ? Set ProductType = 'P'
   ?
4. Save to isBOMImportBills table
   ?
5. BomValidationService validates the file
   ?
6. Phantom items (ProductType='P') auto-validated
   ?
7. Result: Phantom parents are "Validated" status
```

---

## Database Impact

### isBOMImportBills Table

Records from Phantom tab will have:

| Field | Parent Items | Component Items |
|-------|-------------|-----------------|
| TabName | "Phantom" | "Phantom" |
| ParentItemCode | NULL | (actual parent) |
| ProductType | 'P' (forced) | (from Excel) |
| Status | "Validated" (after validation) | (validation result) |
| ItemType | "Phantom" (after validation) | (validation result) |

---

## Testing

### Test Case 1: Phantom Tab Detection

```csharp
[Theory]
[InlineData("Phantom")]
[InlineData("phantom")]
[InlineData("PHANTOM")]
[InlineData("PhAnToM")]
public void ParseWorksheet_PhantomTab_DetectsCorrectly(string tabName)
{
    // Arrange: Create worksheet with given tab name
    // Act: Parse worksheet
    // Assert: isPhantomTab should be true
}
```

### Test Case 2: Parent Item ProductType Override

```csharp
[Fact]
public void ParseWorksheet_PhantomTabParentItem_SetsProductTypeP()
{
    // Arrange: 
    // - Tab name: "Phantom"
    // - Row with empty ParentItemCode
    // - Excel ProductType: "F"
    
    // Act: Parse worksheet
    
    // Assert:
    // - bill.ProductType should be "P"
    // - Not "F" from Excel
}
```

### Test Case 3: Component Items Unchanged

```csharp
[Fact]
public void ParseWorksheet_PhantomTabComponentItem_RetainsExcelProductType()
{
    // Arrange:
    // - Tab name: "Phantom"
    // - Row with ParentItemCode = "PHANTOM-001"
    // - Excel ProductType: "F"
    
    // Act: Parse worksheet
    
    // Assert:
    // - bill.ProductType should be "F"
    // - Excel value retained
}
```

### Test Case 4: Non-Phantom Tab Unchanged

```csharp
[Fact]
public void ParseWorksheet_RegularTab_DoesNotOverrideProductType()
{
    // Arrange:
    // - Tab name: "Standard"
    // - Row with empty ParentItemCode
    // - Excel ProductType: "F"
    
    // Act: Parse worksheet
    
    // Assert:
    // - bill.ProductType should be "F"
    // - Excel value used, not overridden
}
```

---

## Logging

### Log Messages

**Info Level**:
```
Processing Phantom tab - parent items will be set to ProductType 'P'
```

**Debug Level** (for each parent item):
```
Set ProductType='P' for parent item {ComponentItemCode} in Phantom tab
```

### Example Log Output

```
2024-01-15 10:30:00 [INFO] Processing tab: Phantom
2024-01-15 10:30:00 [INFO] Processing Phantom tab - parent items will be set to ProductType 'P'
2024-01-15 10:30:00 [DEBUG] Set ProductType='P' for parent item PHANTOM-001 in Phantom tab
2024-01-15 10:30:00 [DEBUG] Set ProductType='P' for parent item PHANTOM-002 in Phantom tab
2024-01-15 10:30:01 [INFO] Processed 15 records from tab: Phantom
```

---

## Edge Cases

### Case 1: Tab Name Variations
? **Handled**: Case-insensitive comparison
- "Phantom", "phantom", "PHANTOM" all work

### Case 2: Whitespace in Tab Name
? **Handled**: Trim() applied before comparison
- " Phantom ", "Phantom\t" treated as "Phantom"

### Case 3: Empty ProductType in Excel
? **Handled**: Override sets to 'P' regardless
- NULL or empty becomes 'P' for parent items

### Case 4: Multiple Phantom Tabs
? **Handled**: Each tab processed independently
- File can have multiple Phantom tabs

### Case 5: No Parent Items in Phantom Tab
? **Handled**: Only component items exist
- No ProductType overrides occur (correct behavior)

---

## Benefits

### For Users
? **Automatic Classification**: No manual ProductType entry for phantom parents  
? **Consistency**: All phantom parents get type 'P'  
? **Clarity**: Clear which items are phantoms by tab organization  

### For System
? **Auto-Validation**: Phantom items validated immediately  
? **No CI_Item Lookups**: Skip unnecessary database queries  
? **Correct Statistics**: Phantom items don't inflate "New Buy Items" count  

### For Data Quality
? **Override Excel Errors**: Corrects incorrect ProductType values  
? **Enforced Standards**: Phantom tab = Phantom type  
? **Audit Trail**: Logs show ProductType was set by system  

---

## Configuration

### No Configuration Required

This is a convention-based feature:
- No app settings needed
- No database changes required
- Works automatically when tab is named "Phantom"

### Customization Options (Future)

If needed, could add settings:
```json
{
  "PhantomTabNames": ["Phantom", "Virtual", "Planning"],
  "EnablePhantomTabProcessing": true
}
```

---

## Related Features

### Integration with Phantom Validation

This feature works together with the phantom validation logic in `BomValidationService.cs`:

1. **Import**: FileImportService sets ProductType='P' for parent items in Phantom tab
2. **Validation**: BomValidationService detects ProductType='P' and auto-validates
3. **Result**: Seamless processing of phantom items

### BOM Integration

When integrating BOMs:
- Phantom parent items (ProductType='P') are handled specially
- May not require CI_Item creation (depends on Sage version)
- BOM structure reflects phantom nature

---

## Troubleshooting

### Issue: Parent items not getting ProductType='P'

**Possible Causes**:
1. Tab name is not exactly "Phantom" (check for extra characters)
2. ParentItemCode column has spaces instead of NULL
3. Row parsing failed (check logs)

**Solution**:
- Check log for "Processing Phantom tab" message
- Verify ParentItemCode is truly empty in Excel
- Review import logs for parsing errors

### Issue: Component items getting ProductType='P'

**Possible Causes**:
1. ParentItemCode is empty when it shouldn't be
2. BOM structure incorrect in Excel

**Solution**:
- Verify Excel file structure
- Ensure component rows have ParentItemCode filled in
- Check if these should actually be parent items

---

## Summary

### What Changed
? Added tab name detection for "Phantom"  
? Added ProductType override for parent items in Phantom tab  
? Added logging for transparency  

### What Stays the Same
? Component items retain Excel ProductType  
? Regular tabs unchanged  
? Validation logic unchanged  

### Impact
? Cleaner data classification  
? Automatic phantom handling  
? Better user experience  

---

**Status**: ? Implemented  
**Build**: ? Successful  
**Testing**: ?? Ready for testing  
**Documentation**: ? Complete  

The Phantom tab import feature is now live and ready to use! ??
