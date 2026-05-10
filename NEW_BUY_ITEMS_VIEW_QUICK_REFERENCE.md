# New Buy Items View - Quick Reference

## ? Implementation Complete

### Features
- ?? **Data Grid** - Displays buy items in sortable columns
- ??? **Print** - Generate and print professional reports
- ?? **Refresh** - Reload data on demand
- ?? **Statistics** - Real-time item count
- ? **Loading States** - Visual feedback during operations

---

## Quick Access

### Files Modified/Created

| File | Purpose |
|------|---------|
| `NewBuyItemRepository.cs` | Queries `isBOMImportBills` for NewBuyItem status |
| `NewBuyItemsViewModel.cs` | Handles UI logic and commands |
| `NewBuyItemsView.xaml` | UI layout and styling |
| `INewBuyItemRepository.cs` | Added `GetCountAsync()` method |
| `App.xaml.cs` | Added logger parameter to repository |

---

## SQL Query

```sql
SELECT DISTINCT
    ComponentItemCode as ItemCode,
    ComponentDescription as Description,
    UnitOfMeasure,
    MIN(ImportDate) as IdentifiedDate,
    MIN(ImportWindowsUser) as IdentifiedBy,
    COUNT(*) as OccurrenceCount
FROM isBOMImportBills
WHERE Status = 'NewBuyItem'
GROUP BY ComponentItemCode, ComponentDescription, UnitOfMeasure
ORDER BY ComponentItemCode
```

---

## UI Layout

```
???????????????????????????????????????????????????
? New Buy Items                                   ?
???????????????????????????????????????????????????
? [?? Refresh] [??? Print]    ? Total Items: 15 ? ?
???????????????????????????????????????????????????
? Item Code ? Description ? UOM ? Date ? Count ? ?
???????????????????????????????????????????????? ?
? COMP-001  ? Widget A    ? EA  ? 1/15 ?  3    ? ?
? COMP-002  ? Widget B    ? EA  ? 1/16 ?  1    ? ?
???????????????????????????????????????????????????
? Status: Loaded 15 items  ?  Last Updated: ... ? ?
???????????????????????????????????????????????????
```

---

## How to Use

### View Buy Items
1. Click "New Buy Items" tab in MainWindow
2. View automatically loads items
3. Items sorted by Item Code

### Refresh Data
1. Click "?? Refresh" button
2. Wait for loading indicator
3. View updates with latest data

### Print Report
1. Click "??? Print" button
2. Select printer in dialog
3. Confirm to print

### Sort Data
- Click any column header to sort
- Click again to reverse sort order

### Select Item
- Click any row to select
- Selection stored in `SelectedItem` property

---

## Data Columns

| Column | Description | Width |
|--------|-------------|-------|
| **Item Code** | Component item code | 150px |
| **Description** | Item description | Flexible |
| **UOM** | Unit of measure | 80px |
| **Identified Date** | When first found | 120px |
| **Identified By** | Who imported | 120px |
| **Occurrences** | # of BOMs using | 100px |

---

## Commands

### LoadItemsCommand
- **Trigger:** Automatic on view load
- **Action:** Queries database for new buy items
- **Updates:** Items, TotalItems, StatusMessage

### RefreshCommand
- **Trigger:** User clicks "Refresh" button
- **Action:** Calls LoadItemsCommand

### PrintCommand
- **Trigger:** User clicks "Print" button
- **Action:** Generates report and shows print dialog
- **Validation:** Checks if items exist

---

## Testing

### Verify Data Loads
```sql
-- Check for new buy items
SELECT * FROM isBOMImportBills WHERE Status = 'NewBuyItem';
```

### Create Test Data
```sql
-- Mark some items as NewBuyItem
UPDATE isBOMImportBills
SET Status = 'NewBuyItem'
WHERE Id IN (1, 2, 3);
```

### Verify Print Works
1. Load items
2. Click Print
3. Check print preview
4. Print to PDF/printer

---

## Common Tasks

### View Item Count
- Look at blue badge in toolbar
- Shows total unique items

### Find Specific Item
- Click column header to sort
- Scroll to find item
- Or wait for search feature

### Export Data
- Use Print button
- Print to PDF printer
- Or wait for Excel export feature

---

## Troubleshooting

### No Items Display
```sql
-- Verify records exist
SELECT COUNT(DISTINCT ComponentItemCode) 
FROM isBOMImportBills 
WHERE Status = 'NewBuyItem';
```

### Print Fails
- Check printer is installed
- Try printing to PDF
- Check error logs

### Slow Loading
- Check database connection
- Verify SQL Server is running
- Check logs for SQL errors

---

## Dependencies

### Required Services
- `NewItemService`
- `INewBuyItemRepository`
- `ILoggerService`

### Required Tables
- `isBOMImportBills`

### Connection String
From `appsettings.json`:
```json
"DatabaseConnectionString": "Server=...;Database=MAS_AML;..."
```

---

## Integration

### MainWindow
```xml
<TabItem Header="New Buy Items">
    <views:NewBuyItemsView />
</TabItem>
```

### DI Registration (App.xaml.cs)
```csharp
services.AddSingleton<INewBuyItemRepository>(sp => 
    new NewBuyItemRepository(
        GetConnectionString(), 
        sp.GetRequiredService<ILoggerService>()));
services.AddSingleton<NewItemService>();
services.AddTransient<NewBuyItemsViewModel>();
```

---

## Performance

- **Query:** Optimized with GROUP BY
- **Loading:** Async with visual indicator
- **Memory:** Lightweight anonymous objects
- **UI:** Responsive during operations

---

## Status Indicators

### Loading
- Black overlay with progress bar
- "Loading new buy items..." message

### Empty
- ?? Icon displayed
- "No new buy items found" message

### Error
- MessageBox with error details
- Status bar shows error message

### Success
- Status bar: "Loaded X items"
- Grid populated with data

---

## Keyboard Shortcuts

| Key | Action |
|-----|--------|
| F5 | Refresh (if implemented) |
| Ctrl+P | Print (if implemented) |
| Arrow Keys | Navigate grid |
| Enter | Select item |

---

## Future Features

- ?? Search/Filter
- ?? Export to Excel
- ?? Charts/Analytics
- ?? Inline editing
- ??? Delete items
- ? Bulk selection

---

## Related Documentation

- `NEW_BUY_ITEMS_VIEW_IMPLEMENTATION.md` - Full guide
- `BOM_VALIDATION_IMPLEMENTATION_GUIDE.md` - Validation logic
- `LOGGING_SYSTEM_GUIDE.md` - Logging details

---

## Quick Commands

### Check Item Status
```sql
SELECT Status, COUNT(*) FROM isBOMImportBills GROUP BY Status;
```

### Find Buy Items
```sql
SELECT * FROM isBOMImportBills WHERE Status = 'NewBuyItem';
```

### Clear Buy Items
```sql
UPDATE isBOMImportBills SET Status = 'Validated' 
WHERE Status = 'NewBuyItem';
```

---

## Support

### Logs Location
```
%APPDATA%\Aml.BOM.Import\Logs\
```

### Check Logs
```csharp
_logger.LogInformation("Retrieved {0} new buy items", items.Count);
_logger.LogError("Failed to retrieve new buy items", ex);
```

---

**Status:** ? Complete and Tested
**Build:** ? Successful
**Ready for:** Production Use
