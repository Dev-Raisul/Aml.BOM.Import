# NewMakeItemRepository - Quick Reference

## ?? What It Does

Retrieves and manages "New Make Items" from `isBOMImportBills` table where `Status = 'NewMakeItem'`.

---

## ?? Data Source

```sql
Table: isBOMImportBills
Filter: WHERE Status = 'NewMakeItem'
Strategy: GROUP BY ComponentItemCode (distinct items)
```

---

## ?? Methods

### GetAllAsync()
```csharp
var items = await repository.GetAllAsync();
// Returns: All distinct new make items
```

### GetByIdAsync(int id)
```csharp
var item = await repository.GetByIdAsync(123);
// Returns: Specific make item by ID
```

### AddAsync(object item)
```csharp
int id = await repository.AddAsync(newItem);
// Returns: New record ID
```

### UpdateAsync(object item)
```csharp
await repository.UpdateAsync(item);
// Updates: ALL records with same ItemCode
```

### DeleteAsync(int id)
```csharp
await repository.DeleteAsync(123);
// Deletes: Single record
```

### BulkUpdateFieldAsync()
```csharp
await repository.BulkUpdateFieldAsync(
    new[] { 1, 2, 3 },
    "ItemDescription",
    "New Description"
);
// Updates: Multiple records at once
```

### GetCountByImportFileAsync()
```csharp
int count = await repository.GetCountByImportFileAsync("BOMs_Jan.xlsx");
// Returns: Count of distinct items in file
```

### GetByImportFileAsync()
```csharp
var items = await repository.GetByImportFileAsync("BOMs_Jan.xlsx");
// Returns: All items from specific file
```

### MarkAsIntegratedAsync()
```csharp
await repository.MarkAsIntegratedAsync("PART-001", "BOMs_Jan.xlsx");
// Updates: Status to 'Integrated', sets date/user
```

---

## ?? Key Concepts

### Distinct Items

**Problem**: Same component in multiple BOMs
```
PART-001 in ASSY-001
PART-001 in ASSY-002
PART-001 in ASSY-003
```

**Solution**: GROUP BY ComponentItemCode
```
Result: ONE record for PART-001
```

### Update Consistency

**Problem**: User edits PART-001 description

**Solution**: Update ALL occurrences
```sql
UPDATE ... WHERE ComponentItemCode = 'PART-001'
```

**Result**: All BOMs get updated description

### Integration

**Problem**: After creating in Sage, hide from view

**Solution**: Change status
```sql
UPDATE ... SET Status = 'Integrated'
```

**Result**: No longer in `WHERE Status = 'NewMakeItem'`

---

## ?? Common Workflows

### Load Items
```
GetAllAsync()
  ?
Groups by ComponentItemCode
  ?
Returns distinct items
```

### Edit Description
```
UpdateAsync(item)
  ?
Updates ALL records with ItemCode
  ?
Keeps BOMs consistent
```

### Bulk Update
```
BulkUpdateFieldAsync(ids, field, value)
  ?
Single SQL statement
  ?
Updates multiple items
```

### Integration
```
Create in Sage
  ?
MarkAsIntegratedAsync(code, file)
  ?
Status ? 'Integrated'
  ?
Hidden from view
```

---

## ?? SQL Examples

### Get All
```sql
SELECT DISTINCT ComponentItemCode, ...
FROM isBOMImportBills
WHERE Status = 'NewMakeItem'
GROUP BY ComponentItemCode, ComponentDescription, ImportFileName, ImportDate
```

### Update All Uses
```sql
UPDATE isBOMImportBills
SET ComponentDescription = 'New Desc'
WHERE ComponentItemCode = 'PART-001'
  AND Status = 'NewMakeItem'
```

### Mark Integrated
```sql
UPDATE isBOMImportBills
SET Status = 'Integrated',
    DateIntegrated = GETDATE(),
    IntegratedBy = @User
WHERE ComponentItemCode = 'PART-001'
  AND Status = 'NewMakeItem'
```

---

## ?? Important Notes

### Duplicates Prevented
? GROUP BY ensures unique items  
? Same item from different files = separate records  
? No duplicate item creation  

### Updates Propagate
? UpdateAsync updates ALL occurrences  
? Keeps all BOMs in sync  
? Single source of truth  

### Status Lifecycle
```
NewMakeItem ? (edit) ? NewMakeItem
NewMakeItem ? (integrate) ? Integrated
```

### Performance
? Indexed on Status and ComponentItemCode  
? GROUP BY after WHERE (efficient)  
? Bulk operations = single SQL  

---

## ?? Data Mapping

| Database | Entity Property | Default |
|----------|----------------|---------|
| ComponentItemCode | ItemCode | (from BOM) |
| ComponentDescription | ItemDescription | (from BOM) |
| ImportFileName | ImportFileName | (from import) |
| ImportDate | ImportFileDate | (from import) |
| - | ProductLine | "" (empty) |
| - | ProductType | "F" |
| - | Procurement | "M" |
| - | StandardUnitOfMeasure | "EACH" |
| - | IsIntegrated | false |

---

## ?? Usage in View

### ViewModel
```csharp
// Load items
var items = await _repository.GetAllAsync();
Items = items.Cast<NewMakeItem>().ToList();

// Update item
await _repository.UpdateAsync(editedItem);

// Bulk update
await _repository.BulkUpdateFieldAsync(ids, field, value);

// Mark integrated
await _repository.MarkAsIntegratedAsync(code, file);
```

### Filtering
```csharp
// All new items (not integrated)
var all = await _repository.GetAllAsync();

// From specific file
var fileItems = await _repository.GetByImportFileAsync("BOMs_Jan.xlsx");

// Count
int count = await _repository.GetCountByImportFileAsync("BOMs_Jan.xlsx");
```

---

## ? Testing Checklist

- [ ] GetAllAsync returns distinct items
- [ ] Default values set correctly
- [ ] UpdateAsync updates all occurrences
- [ ] BulkUpdate is efficient
- [ ] MarkAsIntegrated works
- [ ] GROUP BY prevents duplicates
- [ ] Import file filtering works
- [ ] Integration hides from view

---

## ?? Quick Start

```csharp
// 1. Get all new make items
var items = await repository.GetAllAsync();

// 2. Edit and update
item.ItemDescription = "Updated";
await repository.UpdateAsync(item);

// 3. Bulk update
await repository.BulkUpdateFieldAsync(
    itemIds, 
    "ItemDescription", 
    "Bulk Updated"
);

// 4. Mark as integrated
await repository.MarkAsIntegratedAsync(
    "PART-001", 
    "BOMs_Jan.xlsx"
);
```

---

**Key Takeaway**: Groups by ComponentItemCode to get distinct items, updates ALL occurrences to keep BOMs consistent! ??

**Full Documentation**: [NEW_MAKE_ITEM_REPOSITORY_IMPLEMENTATION.md](NEW_MAKE_ITEM_REPOSITORY_IMPLEMENTATION.md)
