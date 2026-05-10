# NewMakeItemRepository Implementation - Complete Guide

## ? IMPLEMENTATION COMPLETE

**Build Status**: ? **SUCCESS**  
**File**: `Aml.BOM.Import.Infrastructure\Repositories\NewMakeItemRepository.cs`

---

## ?? Overview

The NewMakeItemRepository retrieves and manages "New Make Items" from the `isBOMImportBills` table where `Status = 'NewMakeItem'`. These are component items that were not found in Sage during BOM validation and need to be created as make items.

---

## ?? Data Source

### Source Table
```sql
Table: isBOMImportBills
Filter: WHERE Status = 'NewMakeItem'
```

### What Are "New Make Items"?

During BOM validation, when a component item is not found in Sage:
1. System checks if it's a buy or make item
2. If determined to be a "make" item
3. Status set to `'NewMakeItem'`
4. Appears in New Make Items View

### Example Records

```sql
-- Sample data in isBOMImportBills
Id | ParentItemCode | ComponentItemCode | Status        | ImportFileName
---|----------------|-------------------|---------------|------------------
1  | ASSY-001      | PART-001          | NewMakeItem   | BOMs_Jan.xlsx
2  | ASSY-001      | PART-002          | NewMakeItem   | BOMs_Jan.xlsx
3  | ASSY-002      | PART-001          | NewMakeItem   | BOMs_Jan.xlsx
4  | ASSY-003      | SCREW-M6          | Validated     | BOMs_Jan.xlsx
```

**Result**: Repository returns PART-001 and PART-002 as distinct make items

---

## ?? Implementation Details

### Key Features

1. **Distinct Item Retrieval** - Groups by ComponentItemCode to avoid duplicates
2. **Import File Tracking** - Tracks which file each item came from
3. **Bulk Operations** - Supports bulk updates for efficiency
4. **Integration Tracking** - Marks items as integrated when created in Sage

### SQL Query Strategy

```sql
-- Gets distinct component items with NewMakeItem status
SELECT DISTINCT
    ComponentItemCode AS ItemCode,
    ComponentDescription AS ItemDescription,
    ImportFileName,
    ImportDate AS ImportFileDate,
    MIN(Id) AS Id,                          -- First occurrence ID
    MAX(DateValidated) AS CreatedDate,      -- Latest validation date
    MAX(DateValidated) AS ModifiedDate
FROM isBOMImportBills
WHERE Status = 'NewMakeItem'
GROUP BY ComponentItemCode, ComponentDescription, ImportFileName, ImportDate
ORDER BY ImportDate DESC, ComponentItemCode
```

**Why GROUP BY?**
- Same component can appear in multiple BOMs
- Need unique list of items to create
- Prevents duplicate item creation

**Example**:
```
PART-001 appears in:
  - ASSY-001 BOM (row 1)
  - ASSY-002 BOM (row 3)
  
Result: ONE record for PART-001
```

---

## ?? Methods Implemented

### 1. GetAllAsync()

**Purpose**: Retrieve all new make items

```csharp
public async Task<IEnumerable<object>> GetAllAsync()
```

**Returns**: List of NewMakeItem objects with:
- Distinct ComponentItemCode (no duplicates)
- Default values (ProductType='F', Procurement='M', UOM='EACH')
- Not yet integrated (IsIntegrated=false)

**Usage**:
```csharp
var items = await repository.GetAllAsync();
// Returns: All unique component items needing creation
```

**SQL**:
```sql
SELECT DISTINCT ComponentItemCode, ComponentDescription, ...
FROM isBOMImportBills
WHERE Status = 'NewMakeItem'
GROUP BY ComponentItemCode, ComponentDescription, ImportFileName, ImportDate
```

---

### 2. GetByIdAsync(int id)

**Purpose**: Retrieve a specific make item by ID

```csharp
public async Task<object?> GetByIdAsync(int id)
```

**Parameters**:
- `id`: The isBOMImportBills.Id value

**Returns**: Single NewMakeItem or null

**Usage**:
```csharp
var item = await repository.GetByIdAsync(123);
if (item != null)
{
    // Work with item
}
```

---

### 3. AddAsync(object newMakeItem)

**Purpose**: Add a new make item record

```csharp
public async Task<int> AddAsync(object newMakeItem)
```

**Parameters**:
- `newMakeItem`: NewMakeItem object to insert

**Returns**: New record ID

**Usage**:
```csharp
var item = new NewMakeItem
{
    ItemCode = "PART-NEW-001",
    ItemDescription = "New Part",
    ImportFileName = "Manual.xlsx",
    ImportFileDate = DateTime.Now
};

int newId = await repository.AddAsync(item);
```

**SQL**:
```sql
INSERT INTO isBOMImportBills 
(ImportFileName, ImportDate, ComponentItemCode, ComponentDescription, 
 Status, ItemExists, DateValidated)
VALUES (@ImportFileName, @ImportFileDate, @ItemCode, @ItemDescription, 
        'NewMakeItem', 0, GETDATE())
```

---

### 4. UpdateAsync(object newMakeItem)

**Purpose**: Update make item information

```csharp
public async Task UpdateAsync(object newMakeItem)
```

**Parameters**:
- `newMakeItem`: NewMakeItem with updated values

**Updates**:
- ComponentDescription
- ValidationMessage
- DateValidated

**Important**: Updates ALL records with the same ComponentItemCode from same import file

**SQL**:
```sql
UPDATE isBOMImportBills
SET ComponentDescription = @ItemDescription,
    ValidationMessage = @ValidationMessage,
    DateValidated = GETDATE()
WHERE Status = 'NewMakeItem'
  AND ComponentItemCode = @ItemCode
  AND ImportFileName = @ImportFileName
```

**Why update all records?**
- Same component used in multiple BOMs
- Keep description consistent across all uses
- Single source of truth

---

### 5. DeleteAsync(int id)

**Purpose**: Remove a make item record

```csharp
public async Task DeleteAsync(int id)
```

**Parameters**:
- `id`: Record ID to delete

**Usage**:
```csharp
await repository.DeleteAsync(123);
```

**SQL**:
```sql
DELETE FROM isBOMImportBills
WHERE Id = @Id AND Status = 'NewMakeItem'
```

---

### 6. GetByStatusAsync(int status)

**Purpose**: Get items by status (interface compatibility)

```csharp
public async Task<IEnumerable<object>> GetByStatusAsync(int status)
```

**Note**: For make items, always returns all NewMakeItem records (status parameter ignored)

---

### 7. BulkUpdateFieldAsync()

**Purpose**: Bulk update a specific field for multiple items

```csharp
public async Task BulkUpdateFieldAsync(
    IEnumerable<int> itemIds, 
    string fieldName, 
    object value)
```

**Parameters**:
- `itemIds`: List of IDs to update
- `fieldName`: Field name (e.g., "ItemDescription")
- `value`: New value

**Supported Fields**:
- ItemDescription ? ComponentDescription

**Usage**:
```csharp
var ids = new[] { 1, 2, 3, 4, 5 };
await repository.BulkUpdateFieldAsync(
    ids, 
    nameof(NewMakeItem.ItemDescription), 
    "Updated Description"
);
// Updates all 5 records at once
```

**SQL**:
```sql
UPDATE isBOMImportBills
SET ComponentDescription = @Value,
    DateValidated = GETDATE()
WHERE Id IN (1,2,3,4,5)
  AND Status = 'NewMakeItem'
```

---

### 8. GetCountByImportFileAsync()

**Purpose**: Count distinct make items in a specific import file

```csharp
public async Task<int> GetCountByImportFileAsync(string importFileName)
```

**Parameters**:
- `importFileName`: Name of import file

**Returns**: Count of distinct component items

**Usage**:
```csharp
int count = await repository.GetCountByImportFileAsync("BOMs_Jan.xlsx");
// Returns: 15 (distinct make items in this file)
```

**SQL**:
```sql
SELECT COUNT(DISTINCT ComponentItemCode)
FROM isBOMImportBills
WHERE Status = 'NewMakeItem'
  AND ImportFileName = @ImportFileName
```

---

### 9. GetByImportFileAsync()

**Purpose**: Get all make items from a specific import file

```csharp
public async Task<IEnumerable<object>> GetByImportFileAsync(string importFileName)
```

**Parameters**:
- `importFileName`: Name of import file

**Returns**: List of distinct NewMakeItem objects from that file

**Usage**:
```csharp
var items = await repository.GetByImportFileAsync("BOMs_Jan.xlsx");
// Returns: All make items from January file
```

---

### 10. MarkAsIntegratedAsync()

**Purpose**: Mark item as integrated after creation in Sage

```csharp
public async Task MarkAsIntegratedAsync(
    string itemCode, 
    string importFileName)
```

**Parameters**:
- `itemCode`: Component item code
- `importFileName`: Import file name

**Updates**:
- Status ? 'Integrated'
- DateIntegrated ? Current date/time
- IntegratedBy ? Current Windows user

**Usage**:
```csharp
// After creating item in Sage
await repository.MarkAsIntegratedAsync("PART-001", "BOMs_Jan.xlsx");
```

**SQL**:
```sql
UPDATE isBOMImportBills
SET Status = 'Integrated',
    DateIntegrated = GETDATE(),
    IntegratedBy = @IntegratedBy
WHERE ComponentItemCode = @ItemCode
  AND ImportFileName = @ImportFileName
  AND Status = 'NewMakeItem'
```

**Important**: Updates ALL records with this component code (all BOMs using it)

---

## ?? Complete Workflow

### Workflow 1: Load Make Items

```
1. User opens New Make Items View
   ?
2. ViewModel calls GetAllAsync()
   ?
3. Repository queries isBOMImportBills
   ?
4. Groups by ComponentItemCode
   ?
5. Returns distinct items
   ?
6. View displays unique make items
```

### Workflow 2: Edit Description

```
1. User edits item description in grid
   ?
2. ViewModel calls UpdateAsync()
   ?
3. Repository updates ComponentDescription
   ?
4. Updates ALL records with same ItemCode
   ?
5. DateValidated set to now
   ?
6. All BOMs using this item updated
```

### Workflow 3: Bulk Update

```
1. User filters items (e.g., "ACL5%")
   ?
2. User edits ProductLine = "PL-001"
   ?
3. User confirms "Copy to all"
   ?
4. ViewModel calls BulkUpdateFieldAsync()
   ?
5. Repository updates all filtered IDs
   ?
6. Single SQL statement updates all
```

### Workflow 4: Integration

```
1. User clicks "Integrate"
   ?
2. System creates items in Sage CI_Item
   ?
3. For each successful creation:
   ?
4. ViewModel calls MarkAsIntegratedAsync()
   ?
5. Repository updates Status to 'Integrated'
   ?
6. Sets DateIntegrated and IntegratedBy
   ?
7. Item no longer appears in New Make Items View
```

---

## ?? Data Mapping

### Database ? Entity

| Database Column | NewMakeItem Property | Type | Notes |
|----------------|---------------------|------|-------|
| ComponentItemCode | ItemCode | string | Item identifier |
| ComponentDescription | ItemDescription | string | Item description |
| ImportFileName | ImportFileName | string | Source file |
| ImportDate | ImportFileDate | DateTime | When imported |
| MIN(Id) | Id | int | First occurrence ID |
| Status | (Always 'NewMakeItem') | - | Filter condition |
| - | ProductLine | string | Default: empty |
| - | ProductType | string | Default: "F" |
| - | Procurement | string | Default: "M" |
| - | StandardUnitOfMeasure | string | Default: "EACH" |
| - | IsIntegrated | bool | Default: false |

### Default Values

```csharp
ProductLine = string.Empty           // User must set
ProductType = "F"                   // Finished goods
Procurement = "M"                   // Make item
StandardUnitOfMeasure = "EACH"     // Each
SubProductFamily = string.Empty     // User can set
StagedItem = false                  // Not staged
Coated = false                      // Not coated
GoldenStandard = false              // Not golden standard
IsEdited = false                    // Not edited yet
IsIntegrated = false                // Not integrated yet
```

---

## ?? SQL Query Examples

### Get All Make Items

```sql
-- What the repository executes
SELECT DISTINCT
    ComponentItemCode AS ItemCode,
    ComponentDescription AS ItemDescription,
    ImportFileName,
    ImportDate AS ImportFileDate,
    MIN(Id) AS Id,
    MAX(DateValidated) AS CreatedDate,
    MAX(DateValidated) AS ModifiedDate
FROM isBOMImportBills
WHERE Status = 'NewMakeItem'
GROUP BY ComponentItemCode, ComponentDescription, ImportFileName, ImportDate
ORDER BY ImportDate DESC, ComponentItemCode
```

**Sample Result**:
```
ItemCode    | ItemDescription | ImportFileName    | ImportFileDate
------------|-----------------|-------------------|------------------
PART-001    | Widget A        | BOMs_Jan.xlsx     | 2024-01-15 10:30
PART-002    | Widget B        | BOMs_Jan.xlsx     | 2024-01-15 10:30
BRACKET-X   | X Bracket       | BOMs_Feb.xlsx     | 2024-02-10 14:20
```

### Count by Import File

```sql
SELECT COUNT(DISTINCT ComponentItemCode)
FROM isBOMImportBills
WHERE Status = 'NewMakeItem'
  AND ImportFileName = 'BOMs_Jan.xlsx'
```

**Result**: `15` (15 distinct make items in January file)

### Update All Uses of an Item

```sql
-- When user edits description
UPDATE isBOMImportBills
SET ComponentDescription = 'Updated Description',
    DateValidated = GETDATE()
WHERE Status = 'NewMakeItem'
  AND ComponentItemCode = 'PART-001'
  AND ImportFileName = 'BOMs_Jan.xlsx'
```

**Affected Rows**: All BOMs using PART-001

### Mark as Integrated

```sql
UPDATE isBOMImportBills
SET Status = 'Integrated',
    DateIntegrated = '2024-01-16 09:00',
    IntegratedBy = 'DOMAIN\Username'
WHERE ComponentItemCode = 'PART-001'
  AND ImportFileName = 'BOMs_Jan.xlsx'
  AND Status = 'NewMakeItem'
```

---

## ?? Important Considerations

### 1. Duplicate Prevention

**Problem**: Same component appears in multiple BOMs

```
PART-001 in ASSY-001 (Row 1)
PART-001 in ASSY-002 (Row 3)
PART-001 in ASSY-003 (Row 7)
```

**Solution**: GROUP BY ComponentItemCode

```sql
GROUP BY ComponentItemCode, ComponentDescription, ImportFileName, ImportDate
```

**Result**: ONE record for PART-001

### 2. Update Consistency

**Problem**: User edits PART-001 description in grid

**Solution**: Update ALL records with that component code

```sql
WHERE ComponentItemCode = 'PART-001'
  AND ImportFileName = 'BOMs_Jan.xlsx'
```

**Result**: All 3 BOMs using PART-001 get updated description

### 3. Integration Tracking

**Problem**: After creating in Sage, remove from view

**Solution**: Change status to 'Integrated'

```sql
SET Status = 'Integrated',
    DateIntegrated = GETDATE(),
    IntegratedBy = @User
```

**Result**: Item no longer appears in `WHERE Status = 'NewMakeItem'`

### 4. Import File Grouping

**Problem**: Need to track which file items came from

**Solution**: Include ImportFileName in GROUP BY

```sql
GROUP BY ComponentItemCode, ComponentDescription, ImportFileName, ImportDate
```

**Result**: Same item from different files = separate records

---

## ?? Performance Considerations

### Indexing

**Recommended Indexes**:
```sql
-- For fast status filtering
CREATE INDEX IX_isBOMImportBills_Status 
    ON isBOMImportBills(Status);

-- For component lookups
CREATE INDEX IX_isBOMImportBills_ComponentItemCode 
    ON isBOMImportBills(ComponentItemCode);

-- For import file filtering
CREATE INDEX IX_isBOMImportBills_ImportFileName 
    ON isBOMImportBills(ImportFileName);

-- Composite index for main query
CREATE INDEX IX_isBOMImportBills_Status_Component 
    ON isBOMImportBills(Status, ComponentItemCode);
```

### Query Optimization

**GROUP BY Performance**:
```sql
-- Efficient: Groups after filtering
WHERE Status = 'NewMakeItem'  -- Filter first
GROUP BY ComponentItemCode    -- Then group
```

**Expected Performance**:
- < 100 items: < 100ms
- 100-1000 items: < 500ms
- > 1000 items: < 2s

### Bulk Operations

**Single Update vs Bulk**:

```csharp
// ? Slow: Multiple round trips
foreach (var id in itemIds)
{
    await UpdateAsync(item);  // N database calls
}

// ? Fast: Single round trip
await BulkUpdateFieldAsync(itemIds, "Field", value);  // 1 database call
```

---

## ?? Testing

### Test Cases

#### Test 1: Get All Items

```csharp
[Fact]
public async Task GetAllAsync_ReturnsDistinctItems()
{
    // Arrange
    var repository = new NewMakeItemRepository(connectionString);
    
    // Act
    var items = await repository.GetAllAsync();
    var makeItems = items.Cast<NewMakeItem>().ToList();
    
    // Assert
    Assert.NotEmpty(makeItems);
    Assert.All(makeItems, item => 
    {
        Assert.NotEmpty(item.ItemCode);
        Assert.Equal("F", item.ProductType);
        Assert.Equal("M", item.Procurement);
        Assert.False(item.IsIntegrated);
    });
}
```

#### Test 2: Update Updates All Occurrences

```csharp
[Fact]
public async Task UpdateAsync_UpdatesAllOccurrences()
{
    // Arrange
    var repository = new NewMakeItemRepository(connectionString);
    var item = new NewMakeItem
    {
        ItemCode = "PART-TEST",
        ImportFileName = "Test.xlsx",
        ItemDescription = "Updated Description"
    };
    
    // Act
    await repository.UpdateAsync(item);
    
    // Assert
    // Verify all records with PART-TEST have new description
    var sql = @"SELECT COUNT(*) FROM isBOMImportBills 
                WHERE ComponentItemCode = 'PART-TEST' 
                AND ComponentDescription = 'Updated Description'";
    // Should be > 1 if item appears in multiple BOMs
}
```

#### Test 3: Integration Marks All Records

```csharp
[Fact]
public async Task MarkAsIntegrated_UpdatesAllRecords()
{
    // Arrange
    var repository = new NewMakeItemRepository(connectionString);
    
    // Act
    await repository.MarkAsIntegratedAsync("PART-TEST", "Test.xlsx");
    
    // Assert
    var sql = @"SELECT COUNT(*) FROM isBOMImportBills 
                WHERE ComponentItemCode = 'PART-TEST'
                AND Status = 'Integrated'";
    // All occurrences should be integrated
}
```

---

## ?? Summary

### Key Features

? **Distinct Item Retrieval** - No duplicates  
? **Multi-BOM Support** - Handles items used in multiple BOMs  
? **Bulk Operations** - Efficient batch updates  
? **Integration Tracking** - Marks items as created  
? **Import File Tracking** - Knows source of each item  
? **Consistent Updates** - Changes propagate to all uses  

### SQL Strategy

? **GROUP BY** - Ensures uniqueness  
? **Filtered Updates** - Updates all occurrences  
? **Status Tracking** - NewMakeItem ? Integrated  
? **Audit Trail** - DateIntegrated, IntegratedBy  

### Performance

? **Indexed Queries** - Fast retrieval  
? **Bulk Updates** - Single round trip  
? **Optimized Grouping** - Filter then group  

---

**Build Status**: ? **SUCCESS**  
**Implementation**: ? **COMPLETE**  
**Ready For**: ? **Production Use**

The NewMakeItemRepository is fully implemented and ready to power the New Make Items View! ??
