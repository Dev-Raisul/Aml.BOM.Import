# Excel Import Column Mapping Update - Implementation Summary

## Overview

Updated the BOM import process to correctly map Excel columns to the `isBOMImportBills` database table, including new fields for Product Line, Product Type, Procurement Type, Sub Product Family, and boolean flags.

## Excel File Structure

### Column Names in Excel File
```
1. Parent Item
2. Component Item
3. Item Description
4. Standard Unit of Measure
5. Qty Per
6. Product Line
7. Product Type
8. Procurement Type
9. Sub Product Family
10. Staged Item
11. Coated
12. Golden Standard
```

## Database Table Structure

### New Fields Added to isBOMImportBills
```sql
[ProductLine] [nvarchar](50) NULL
[ProductType] [nvarchar](50) NULL
[ProcurementType] [nvarchar](50) NULL
[SubProductFamily] [nvarchar](100) NULL
[StagedItem] [bit] NULL
[Coated] [bit] NULL
[GoldenStandard] [bit] NULL
```

## Implementation Changes

### 1. Updated BomImportBill Entity

**File**: `Aml.BOM.Import.Domain\Entities\BomImportBill.cs`

**Added Properties**:
```csharp
// New fields from Excel file
public string? ProductLine { get; set; }
public string? ProductType { get; set; }
public string? ProcurementType { get; set; }
public string? SubProductFamily { get; set; }
public bool? StagedItem { get; set; }
public bool? Coated { get; set; }
public bool? GoldenStandard { get; set; }
```

### 2. Updated FileImportService Mapping

**File**: `Aml.BOM.Import.Infrastructure\Services\FileImportService.cs`

**Updated Column Mapping**:
```csharp
var bill = new BomImportBill
{
    // Parent Item mapping
    ParentItemCode = GetCellValue(row, columnMap, "Parent Item", ...),
    ParentDescription = null, // Not in Excel file
    
    // Component Item mapping
    ComponentItemCode = GetCellValue(row, columnMap, "Component Item", ...),
    ComponentDescription = GetCellValue(row, columnMap, "Item Description", ...),
    
    // Quantity mapping
    Quantity = ParseDecimal(GetCellValue(row, columnMap, "Qty Per", ...)),
    UnitOfMeasure = GetCellValue(row, columnMap, "Standard Unit of Measure", ...),
    
    // New fields from Excel
    ProductLine = GetCellValue(row, columnMap, "Product Line"),
    ProductType = GetCellValue(row, columnMap, "Product Type"),
    ProcurementType = GetCellValue(row, columnMap, "Procurement Type"),
    SubProductFamily = GetCellValue(row, columnMap, "Sub Product Family"),
    StagedItem = ParseBoolean(GetCellValue(row, columnMap, "Staged Item")),
    Coated = ParseBoolean(GetCellValue(row, columnMap, "Coated")),
    GoldenStandard = ParseBoolean(GetCellValue(row, columnMap, "Golden Standard")),
};
```

**Added ParseBoolean Method**:
```csharp
private bool? ParseBoolean(string? value)
{
    if (string.IsNullOrWhiteSpace(value))
        return null;

    if (bool.TryParse(value, out bool result))
        return result;

    var upperValue = value.Trim().ToUpper();
    
    // True values: YES, Y, 1, TRUE
    if (upperValue == "YES" || upperValue == "Y" || 
        upperValue == "1" || upperValue == "TRUE")
        return true;

    // False values: NO, N, 0, FALSE
    if (upperValue == "NO" || upperValue == "N" || 
        upperValue == "0" || upperValue == "FALSE")
        return false;

    return null;
}
```

### 3. Updated Repository SQL Statements

**File**: `Aml.BOM.Import.Infrastructure\Repositories\BomImportBillRepository.cs`

**Updated INSERT Statements**:
```sql
INSERT INTO isBOMImportBills 
(ImportFileName, ImportDate, ImportWindowsUser, TabName, Status, 
 ParentItemCode, ParentDescription, BOMLevel, BOMNumber,
 LineNumber, ComponentItemCode, ComponentDescription, Quantity, UnitOfMeasure, 
 Reference, Notes, Category, Type, UnitCost, ExtendedCost,
 ItemExists, ItemType, ValidationMessage, 
 ProductLine, ProductType, ProcurementType, SubProductFamily, 
 StagedItem, Coated, GoldenStandard,
 CreatedDate, ModifiedDate)
VALUES 
(...)
```

**Updated AddBillParameters Method**:
```csharp
command.Parameters.AddWithValue("@ProductLine", (object?)bill.ProductLine ?? DBNull.Value);
command.Parameters.AddWithValue("@ProductType", (object?)bill.ProductType ?? DBNull.Value);
command.Parameters.AddWithValue("@ProcurementType", (object?)bill.ProcurementType ?? DBNull.Value);
command.Parameters.AddWithValue("@SubProductFamily", (object?)bill.SubProductFamily ?? DBNull.Value);
command.Parameters.AddWithValue("@StagedItem", (object?)bill.StagedItem ?? DBNull.Value);
command.Parameters.AddWithValue("@Coated", (object?)bill.Coated ?? DBNull.Value);
command.Parameters.AddWithValue("@GoldenStandard", (object?)bill.GoldenStandard ?? DBNull.Value);
```

**Updated MapFromReader Method**:
```csharp
ProductLine = reader.IsDBNull(reader.GetOrdinal("ProductLine")) 
    ? null : reader.GetString(reader.GetOrdinal("ProductLine")),
ProductType = reader.IsDBNull(reader.GetOrdinal("ProductType")) 
    ? null : reader.GetString(reader.GetOrdinal("ProductType")),
ProcurementType = reader.IsDBNull(reader.GetOrdinal("ProcurementType")) 
    ? null : reader.GetString(reader.GetOrdinal("ProcurementType")),
SubProductFamily = reader.IsDBNull(reader.GetOrdinal("SubProductFamily")) 
    ? null : reader.GetString(reader.GetOrdinal("SubProductFamily")),
StagedItem = reader.IsDBNull(reader.GetOrdinal("StagedItem")) 
    ? null : reader.GetBoolean(reader.GetOrdinal("StagedItem")),
Coated = reader.IsDBNull(reader.GetOrdinal("Coated")) 
    ? null : reader.GetBoolean(reader.GetOrdinal("Coated")),
GoldenStandard = reader.IsDBNull(reader.GetOrdinal("GoldenStandard")) 
    ? null : reader.GetBoolean(reader.GetOrdinal("GoldenStandard")),
```

## Column Mapping Details

### Excel ? Database Mapping

| Excel Column | Database Column | Type | Notes |
|--------------|-----------------|------|-------|
| Parent Item | ParentItemCode | nvarchar(50) | Parent BOM item |
| Component Item | ComponentItemCode | nvarchar(50) | Component/child item |
| Item Description | ComponentDescription | nvarchar(255) | Component description |
| Standard Unit of Measure | UnitOfMeasure | nvarchar(20) | UOM (EACH, LB, etc.) |
| Qty Per | Quantity | decimal(18,4) | Quantity per parent |
| Product Line | ProductLine | nvarchar(50) | Product line classification |
| Product Type | ProductType | nvarchar(50) | Product type (P for Phantom) |
| Procurement Type | ProcurementType | nvarchar(50) | B (Buy), M (Make), P (Phantom) |
| Sub Product Family | SubProductFamily | nvarchar(100) | Product family grouping |
| Staged Item | StagedItem | bit | Boolean flag |
| Coated | Coated | bit | Boolean flag |
| Golden Standard | GoldenStandard | bit | Boolean flag |

### Notes on ParentDescription
- **Not in Excel file**: ParentDescription column is not present in the current Excel format
- Set to `null` during import
- Can be populated from CI_Item table during validation if needed

## Boolean Field Parsing

### Recognized True Values
- `true` (case-insensitive)
- `TRUE`
- `yes` (case-insensitive)
- `YES`, `Yes`, `Y`, `y`
- `1`

### Recognized False Values
- `false` (case-insensitive)
- `FALSE`
- `no` (case-insensitive)
- `NO`, `No`, `N`, `n`
- `0`

### Empty/Null Handling
- Empty cells ? `null`
- Whitespace ? `null`
- Unrecognized values ? `null`

## Example Data

### Excel Row Example
```
Parent Item: MAIN-ASSY
Component Item: SUB-COMP-001
Item Description: Sub Component Part 1
Standard Unit of Measure: EACH
Qty Per: 2.5
Product Line: ACL
Product Type: P
Procurement Type: B
Sub Product Family: ACL5
Staged Item: YES
Coated: NO
Golden Standard: 1
```

### Database Record Result
```sql
ParentItemCode: 'MAIN-ASSY'
ComponentItemCode: 'SUB-COMP-001'
ComponentDescription: 'Sub Component Part 1'
UnitOfMeasure: 'EACH'
Quantity: 2.5000
ProductLine: 'ACL'
ProductType: 'P'
ProcurementType: 'B'
SubProductFamily: 'ACL5'
StagedItem: 1 (true)
Coated: 0 (false)
GoldenStandard: 1 (true)
```

## Validation Impact

### Phantom Item Detection

With ProductType column now available:

```csharp
// In BomValidationService
string componentType = bill.Type?.Trim().ToUpper() ?? "";
bool isPhantom = componentType == "P" || componentType == "PHANTOM";

// OR using ProductType
string productType = bill.ProductType?.Trim().ToUpper() ?? "";
bool isPhantom = productType == "P" || productType == "PHANTOM";
```

**Note**: The validation service currently uses the `Type` field. Consider updating to use `ProductType` if that's the primary phantom indicator.

## Testing

### Test Case 1: Import with New Fields

**Excel Data**:
```
Parent Item: ASSY-001
Component Item: PART-001
Product Line: ACL
Product Type: P
Staged Item: YES
Coated: NO
```

**Expected Database**:
```
ProductLine: 'ACL'
ProductType: 'P'
StagedItem: 1 (true)
Coated: 0 (false)
```

### Test Case 2: Empty Optional Fields

**Excel Data**:
```
Parent Item: ASSY-002
Component Item: PART-002
Product Line: (empty)
Staged Item: (empty)
```

**Expected Database**:
```
ProductLine: NULL
StagedItem: NULL
```

### Test Case 3: Boolean Variations

**Excel Values** ? **Database Result**:
- `YES` ? `1` (true)
- `Y` ? `1` (true)
- `1` ? `1` (true)
- `NO` ? `0` (false)
- `N` ? `0` (false)
- `0` ? `0` (false)
- (empty) ? `NULL`
- `MAYBE` ? `NULL`

## Files Modified

| File | Changes |
|------|---------|
| `BomImportBill.cs` | Added 7 new properties |
| `FileImportService.cs` | Updated column mapping + added ParseBoolean |
| `BomImportBillRepository.cs` | Updated SQL + parameters + reader mapping |

**Total**: 3 files modified

## Benefits

1. **? Correct Mapping**: Excel columns now map to correct database fields
2. **? Additional Data**: Product Line, Product Type, and other metadata captured
3. **? Boolean Support**: Flexible boolean parsing (YES/NO, 1/0, etc.)
4. **? Phantom Detection**: ProductType field available for phantom identification
5. **? Null Handling**: Empty fields properly stored as NULL
6. **? Backward Compatible**: Existing imports continue to work

## Summary

- **Excel Columns**: 12 columns mapped
- **New Database Fields**: 7 fields added
- **Column Mapping**: Updated to match actual Excel structure
- **Boolean Parsing**: Added flexible boolean parser
- **Status**: ? Complete and tested

---

**Build**: ? Successful  
**Files Changed**: 3  
**Production Ready**: ? Yes
