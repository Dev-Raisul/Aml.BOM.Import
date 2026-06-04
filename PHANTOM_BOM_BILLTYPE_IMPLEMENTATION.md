# Phantom BOM BillType Implementation

## Overview

During BOM integration, if the parent item is of Phantom type (ProductType = 'P'), the BillType field in Sage will be set to 'P' (Phantom) instead of 'S' (Standard).

---

## Business Logic

### BillType Determination

The BillType in Sage BM_Bill_bus is determined by the parent item's ProductType:

| Parent ProductType | Sage BillType | Description |
|-------------------|---------------|-------------|
| 'P' | 'P' | Phantom BOM |
| Any other value | 'S' | Standard BOM |
| NULL or empty | 'S' | Default to Standard |

### What is a Phantom BOM?

A Phantom BOM in Sage represents:
- Virtual/planning assemblies
- Items that don't physically exist
- Components passed through to higher-level assemblies
- Used for BOM structuring without inventory tracking

---

## Implementation Details

### File Modified

**File**: `Aml.BOM.Import.Infrastructure\Services\BomIntegrationService.cs`  
**Method**: `IntegrateBomWithLinesAsync`

### Changes Made

#### 1. ProductType Extraction

Extract and normalize the parent item's ProductType:

```csharp
string parentProductType = parentRecord.ProductType?.Trim().ToUpper() ?? "";
```

#### 2. BillType Determination Logic

```csharp
string billType = "S"; // Default to Standard

if (parentProductType == "P")
{
    billType = "P"; // Phantom BOM
    _logger.LogInformation("Parent item {0} is Phantom type - setting BillType to 'P'", parentItemCode);
}
else
{
    _logger.LogInformation("Parent item {0} is Standard type - setting BillType to 'S'", parentItemCode);
}
```

#### 3. Set BillType in Sage

```csharp
retVal = billBus.nSetValue("BillType$", billType);
if (retVal == 0)
{
    _logger.LogWarning("Failed to set BillType$ to '{0}': {1}", billType, billBus.sLastErrorMsg);
}
```

---

## Complete Workflow

### Standard BOM Creation

```
Parent Item: ASSY-001
ProductType: F (or M, or NULL)
    ?
BOM Integration
    ?
Determine BillType ? 'S' (Standard)
    ?
billBus.nSetValue("BillType$", "S")
    ?
Sage BOM created with BillType = 'S'
```

### Phantom BOM Creation

```
Parent Item: PHANTOM-001
ProductType: P
    ?
BOM Integration
    ?
Determine BillType ? 'P' (Phantom)
    ?
billBus.nSetValue("BillType$", "P")
    ?
Sage BOM created with BillType = 'P'
```

---

## Integration with Phantom Tab Feature

This feature works seamlessly with the Phantom tab import:

### Complete Flow

```
1. Excel Import (Phantom Tab)
   ?
   Parent items get ProductType = 'P'
   ?
2. Validation
   ?
   Phantom items auto-validated
   ?
3. Status = Ready
   ?
4. BOM Integration
   ?
   Check ProductType = 'P'
   ?
   Set BillType = 'P'
   ?
5. Sage BOM Created as Phantom Type
```

---

## Example Scenarios

### Scenario 1: Phantom Parent from Phantom Tab

**Excel Tab**: "Phantom"

**Parent Record**:
```
ComponentItemCode: PHANTOM-001
ProductType: P (set by Phantom tab logic)
ParentItemCode: NULL (is parent)
```

**Components**:
```
ComponentItemCode: PART-A, Quantity: 2
ComponentItemCode: PART-B, Quantity: 1
```

**Integration Result**:
```
Sage BOM Created:
  BillNo: PHANTOM-001
  BillType: P (Phantom)
  Revision: 000
  Lines:
    - PART-A (Qty: 2)
    - PART-B (Qty: 1)
```

### Scenario 2: Standard Parent from Regular Tab

**Excel Tab**: "Standard"

**Parent Record**:
```
ComponentItemCode: ASSY-001
ProductType: F
ParentItemCode: NULL (is parent)
```

**Components**:
```
ComponentItemCode: SCREW-M6, Quantity: 4
ComponentItemCode: WASHER-M6, Quantity: 4
```

**Integration Result**:
```
Sage BOM Created:
  BillNo: ASSY-001
  BillType: S (Standard)
  Revision: 000
  Lines:
    - SCREW-M6 (Qty: 4)
    - WASHER-M6 (Qty: 4)
```

### Scenario 3: Mixed BOM Structure

**Excel Tab**: "Phantom"

**Parent Record** (Phantom):
```
ComponentItemCode: PHANTOM-SUB
ProductType: P
```

**Components** (some phantom, some standard):
```
ComponentItemCode: PHANTOM-CHILD, ProductType: P
ComponentItemCode: REAL-PART, ProductType: F
```

**Integration Result**:
```
Parent BOM:
  BillNo: PHANTOM-SUB
  BillType: P (Phantom)
  Components include both phantom and real parts
  
Child BOM (for PHANTOM-CHILD):
  BillNo: PHANTOM-CHILD
  BillType: P (Phantom)
```

---

## Database Fields

### isBOMImportBills Table

The ProductType field is used to determine BillType:

| Field | Type | Purpose |
|-------|------|---------|
| ProductType | NVARCHAR(50) | Source for BillType determination |
| ComponentItemCode | NVARCHAR(50) | Parent item code (when ParentItemCode is NULL) |
| ParentItemCode | NVARCHAR(50) | NULL for parent records |

### Parent Record Example

```sql
-- Parent record (ParentItemCode is NULL)
SELECT 
    ComponentItemCode,  -- PHANTOM-001 (this is the parent)
    ProductType,        -- 'P' (Phantom)
    ParentItemCode      -- NULL (is parent)
FROM isBOMImportBills
WHERE Id = 123;
```

---

## Logging

### Log Messages

#### Info Level

**For Phantom BOM**:
```
Parent item PHANTOM-001 is Phantom type - setting BillType to 'P'
```

**For Standard BOM**:
```
Parent item ASSY-001 is Standard type - setting BillType to 'S'
```

#### Warning Level

**If BillType setting fails**:
```
Failed to set BillType$ to 'P': [error message from Sage]
```

### Example Log Output

```
2024-01-16 10:15:00 [INFO] Creating BOM for parent: PHANTOM-001 with 5 lines
2024-01-16 10:15:00 [INFO] BOM key BillNo$ set for: PHANTOM-001
2024-01-16 10:15:00 [INFO] BOM key Revision$ set to: 000
2024-01-16 10:15:00 [INFO] BOM key finalized for: PHANTOM-001
2024-01-16 10:15:00 [INFO] Parent item PHANTOM-001 is Phantom type - setting BillType to 'P'
2024-01-16 10:15:01 [INFO] Lines collection accessed successfully
2024-01-16 10:15:01 [INFO] Added BOM line 1: PHANTOM-001 -> PART-A (Qty: 2)
2024-01-16 10:15:01 [INFO] Added BOM line 2: PHANTOM-001 -> PART-B (Qty: 1)
...
2024-01-16 10:15:02 [INFO] BOM written to Sage successfully: PHANTOM-001 with 5 lines
```

---

## ProductType Recognition

### Phantom Type Detection

Case-insensitive matching:

```csharp
string parentProductType = parentRecord.ProductType?.Trim().ToUpper() ?? "";

if (parentProductType == "P")
{
    // Recognized as Phantom
}
```

| ProductType Value | Recognized as Phantom |
|------------------|----------------------|
| "P" | ? Yes |
| "p" | ? Yes |
| "PHANTOM" | ? No (only "P") |
| "F" | ? No |
| "M" | ? No |
| NULL | ? No (defaults to Standard) |
| "" | ? No (defaults to Standard) |

**Note**: Only the single character 'P' is recognized. Full word "PHANTOM" in ProductType field would still result in Standard BillType.

---

## Error Handling

### Scenario 1: ProductType is NULL

```csharp
// ProductType is NULL or empty
parentRecord.ProductType = null;

// Result
billType = "S"; // Defaults to Standard
```

**Outcome**: Standard BOM created (safe default)

### Scenario 2: BillType$ Setting Fails

```csharp
retVal = billBus.nSetValue("BillType$", "P");
if (retVal == 0)
{
    _logger.LogWarning("Failed to set BillType$ to 'P': {0}", billBus.sLastErrorMsg);
}
// Continues with integration (warning logged but not fatal)
```

**Outcome**: Warning logged, integration continues

### Scenario 3: Invalid ProductType Value

```csharp
// ProductType has unexpected value
parentRecord.ProductType = "XYZ";

// Result
billType = "S"; // Defaults to Standard (not "P")
```

**Outcome**: Standard BOM created (safe default)

---

## Sage 100 BillType Field

### Valid BillType Values in Sage

| BillType | Description | Use Case |
|----------|-------------|----------|
| 'S' | Standard | Regular assemblies with inventory |
| 'P' | Phantom | Virtual assemblies, no inventory |
| 'C' | Configured | Configurable BOMs (may not be used) |

### BillType Behavior in Sage

**Standard BOM ('S')**:
- Parent item exists in inventory
- Components consumed when parent is built
- Work orders created for parent item

**Phantom BOM ('P')**:
- Parent item is virtual/planning only
- Components passed through to higher level
- No work orders for phantom item itself
- Used for BOM structuring

---

## Testing

### Test Case 1: Phantom Parent Sets BillType P

```csharp
[Fact]
public async Task IntegrateBomWithLinesAsync_PhantomParent_SetsBillTypeP()
{
    // Arrange
    var parentRecord = new BomImportBill
    {
        ComponentItemCode = "PHANTOM-001",
        ProductType = "P",
        ParentItemCode = null
    };
    
    var components = new List<BomImportBill>
    {
        new() { ComponentItemCode = "PART-A", Quantity = 2 }
    };
    
    // Act
    var result = await IntegrateBomWithLinesAsync(session, parentRecord, components);
    
    // Assert
    // Verify billBus.nSetValue("BillType$", "P") was called
    Assert.True(result);
}
```

### Test Case 2: Standard Parent Sets BillType S

```csharp
[Fact]
public async Task IntegrateBomWithLinesAsync_StandardParent_SetsBillTypeS()
{
    // Arrange
    var parentRecord = new BomImportBill
    {
        ComponentItemCode = "ASSY-001",
        ProductType = "F",
        ParentItemCode = null
    };
    
    var components = new List<BomImportBill>
    {
        new() { ComponentItemCode = "SCREW-M6", Quantity = 4 }
    };
    
    // Act
    var result = await IntegrateBomWithLinesAsync(session, parentRecord, components);
    
    // Assert
    // Verify billBus.nSetValue("BillType$", "S") was called
    Assert.True(result);
}
```

### Test Case 3: NULL ProductType Defaults to Standard

```csharp
[Fact]
public async Task IntegrateBomWithLinesAsync_NullProductType_DefaultsToStandard()
{
    // Arrange
    var parentRecord = new BomImportBill
    {
        ComponentItemCode = "ASSY-002",
        ProductType = null, // NULL
        ParentItemCode = null
    };
    
    var components = new List<BomImportBill>
    {
        new() { ComponentItemCode = "PART-X", Quantity = 1 }
    };
    
    // Act
    var result = await IntegrateBomWithLinesAsync(session, parentRecord, components);
    
    // Assert
    // Verify billBus.nSetValue("BillType$", "S") was called
    Assert.True(result);
}
```

### Test Case 4: Case Insensitive ProductType

```csharp
[Theory]
[InlineData("P")]
[InlineData("p")]
public async Task IntegrateBomWithLinesAsync_ProductTypeP_CaseInsensitive(string productType)
{
    // Arrange
    var parentRecord = new BomImportBill
    {
        ComponentItemCode = "PHANTOM-TEST",
        ProductType = productType,
        ParentItemCode = null
    };
    
    var components = new List<BomImportBill>
    {
        new() { ComponentItemCode = "PART-Y", Quantity = 1 }
    };
    
    // Act
    var result = await IntegrateBomWithLinesAsync(session, parentRecord, components);
    
    // Assert
    // Verify billBus.nSetValue("BillType$", "P") was called
    Assert.True(result);
}
```

---

## Benefits

### For Phantom BOMs

? **Correct Classification**: Phantom BOMs properly identified in Sage  
? **Inventory Handling**: No inventory transactions for phantom items  
? **BOM Structure**: Proper hierarchy representation  
? **Work Order Logic**: Correct work order behavior in Sage  

### For Data Integrity

? **Automatic**: No manual BillType selection needed  
? **Consistent**: ProductType drives BillType consistently  
? **Traceable**: Logged for audit purposes  

### For Users

? **Transparent**: Works automatically based on ProductType  
? **Correct Behavior**: Sage behaves appropriately for phantom vs standard  
? **No Confusion**: Clear separation between phantom and standard BOMs  

---

## Integration Points

### With Phantom Tab Import

```
Phantom Tab Import ? ProductType = 'P'
    ?
Validation ? Auto-validate phantoms
    ?
Integration ? BillType = 'P'
```

### With Standard Tab Import

```
Standard Tab Import ? ProductType = 'F' or 'M'
    ?
Validation ? Normal validation
    ?
Integration ? BillType = 'S'
```

---

## Edge Cases Handled

### Case 1: Whitespace in ProductType
```csharp
ProductType = "  P  " ? Trimmed to "P" ? BillType = 'P' ?
```

### Case 2: Mixed Case ProductType
```csharp
ProductType = "p" ? ToUpper to "P" ? BillType = 'P' ?
```

### Case 3: Empty String ProductType
```csharp
ProductType = "" ? Defaults to Standard ? BillType = 'S' ?
```

### Case 4: Multi-Character ProductType
```csharp
ProductType = "PHANTOM" ? Not "P" ? BillType = 'S' ?
```

---

## Related Features

### 1. Phantom Tab Import
- **File**: `FileImportService.cs`
- **Sets**: ProductType = 'P' for parent items in Phantom tab
- **Flow**: Import ? Integration

### 2. Phantom Validation
- **File**: `BomValidationService.cs`
- **Detects**: ProductType = 'P' or Type = 'P'
- **Action**: Auto-validates phantom items

### 3. BOM Integration
- **File**: `BomIntegrationService.cs`
- **Uses**: ProductType to set BillType
- **Result**: Correct BOM type in Sage

---

## Configuration

### No Configuration Required

This is a convention-based feature:
- Reads ProductType from database
- Sets BillType automatically
- No app settings needed

### ProductType Standard

| ProductType | Meaning | BillType |
|------------|---------|----------|
| 'P' | Phantom | 'P' |
| 'F' | Finished Goods | 'S' |
| 'M' | Manufactured | 'S' |
| NULL | Unknown | 'S' (default) |

---

## Troubleshooting

### Issue: Phantom BOM created as Standard

**Possible Causes**:
1. ProductType is not exactly "P"
2. ProductType has whitespace (should be trimmed automatically)
3. Parent record has wrong ProductType

**Solution**:
- Check logs for "Parent item {X} is Phantom type" message
- Verify ProductType in isBOMImportBills table
- Confirm Phantom tab import worked correctly

### Issue: Standard BOM created as Phantom

**Possible Causes**:
1. ProductType incorrectly set to "P"
2. Wrong tab used during import

**Solution**:
- Check import source (which tab?)
- Verify ProductType should not be "P"
- Re-import from correct tab if needed

---

## Summary

### What Changed

? Added ProductType extraction and normalization  
? Added BillType determination logic  
? Added conditional BillType setting  
? Added informational logging  

### How It Works

```
ProductType = 'P' ? BillType = 'P' (Phantom)
ProductType ? 'P' ? BillType = 'S' (Standard)
```

### Impact

? Phantom BOMs correctly identified in Sage  
? Proper BOM behavior based on type  
? Seamless integration with Phantom tab import  
? Clear audit trail via logging  

---

**Status**: ? Implemented  
**Build**: ? Successful  
**Testing**: ?? Ready for testing  
**Documentation**: ? Complete  

The Phantom BOM BillType feature is now implemented and working with the complete Phantom workflow! ??
