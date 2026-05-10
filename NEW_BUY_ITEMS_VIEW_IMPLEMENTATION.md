# New Buy Items View - Implementation Guide

## Overview
The New Buy Items View displays components that need to be purchased (buy items) identified during BOM imports. It provides a data grid to display the items and a print functionality to generate reports.

---

## Features Implemented

### ? Data Grid Display
- Professional-looking DataGrid with styled headers
- Alternating row colors for better readability
- Sortable columns
- Empty state message when no items exist

### ? Print Functionality
- Professional report generation with FlowDocument
- Formatted table with headers and borders
- Date timestamp and statistics
- Print dialog integration

### ? Statistics Display
- Total item count badge
- Status message bar showing current state
- Last updated timestamp

### ? Refresh Capability
- Manual refresh button
- Auto-load on view initialization
- Loading indicator overlay

---

## Architecture

### Data Flow

```
isBOMImportBills Table (Status = 'NewBuyItem')
    ?
NewBuyItemRepository.GetAllAsync()
    ?
NewItemService.GetNewBuyItemsAsync()
    ?
NewBuyItemsViewModel
    ?
NewBuyItemsView (UI)
```

### SQL Query

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

**Key Points:**
- Groups by component item code to avoid duplicates
- Shows first identified date
- Counts how many BOMs reference this item
- Only includes items with status 'NewBuyItem'

---

## Implementation Details

### 1. Repository (`NewBuyItemRepository.cs`)

**Location:** `Aml.BOM.Import.Infrastructure\Repositories\NewBuyItemRepository.cs`

**Key Methods:**
```csharp
public async Task<IEnumerable<object>> GetAllAsync()
{
    // Query isBOMImportBills table
    // Returns anonymous objects with item data
    // Groups by ComponentItemCode
}

public async Task<int> GetCountAsync()
{
    // Returns count of distinct new buy items
}
```

**Features:**
- Queries `isBOMImportBills` table directly
- Groups by component item code
- Returns occurrence count for each item
- Includes logger integration

---

### 2. ViewModel (`NewBuyItemsViewModel.cs`)

**Location:** `Aml.BOM.Import.UI\ViewModels\NewBuyItemsViewModel.cs`

**Properties:**
```csharp
ObservableCollection<object> Items          // List of buy items
object? SelectedItem                         // Currently selected item
bool IsLoading                               // Loading state
int TotalItems                               // Count of items
string StatusMessage                         // Current status text
```

**Commands:**
```csharp
LoadItemsCommand    // Loads items from service
RefreshCommand      // Refreshes the list
PrintCommand        // Prints the report
```

**Print Document Generation:**
- Creates FlowDocument with professional formatting
- Includes title, date, summary, and table
- Styled headers and borders
- Dynamic data from Items collection

---

### 3. View (`NewBuyItemsView.xaml`)

**Location:** `Aml.BOM.Import.UI\Views\NewBuyItemsView.xaml`

**Layout Structure:**
```
Grid
??? Header (Row 0)
?   ??? "New Buy Items" Title
??? Toolbar (Row 1)
?   ??? Refresh Button
?   ??? Print Button
?   ??? Statistics Badge
??? DataGrid (Row 2)
?   ??? Columns:
?       ??? Item Code
?       ??? Description
?       ??? UOM
?       ??? Identified Date
?       ??? Identified By
?       ??? Occurrences
??? Status Bar (Row 3)
?   ??? Status Message + Timestamp
??? Loading Overlay
    ??? Progress Bar + Text
```

**DataGrid Columns:**

| Column | Binding | Width | Description |
|--------|---------|-------|-------------|
| Item Code | ItemCode | 150px | Component item code |
| Description | Description | * (flexible) | Item description |
| UOM | UnitOfMeasure | 80px | Unit of measure |
| Identified Date | IdentifiedDate | 120px | When first identified |
| Identified By | IdentifiedBy | 120px | Who imported |
| Occurrences | OccurrenceCount | 100px | # of BOMs using this |

**Styling:**
- Blue header background (#2196F3)
- Alternating row colors (#F9F9F9)
- Selected row highlight (#E3F2FD)
- Bordered grid with horizontal lines

---

## How It Works

### On View Load

1. **ViewModel Constructor** calls `LoadItemsCommand`
2. **LoadItems()** method:
   - Sets `IsLoading = true`
   - Calls `NewItemService.GetNewBuyItemsAsync()`
   - Populates `Items` collection
   - Updates `TotalItems` and `StatusMessage`
   - Sets `IsLoading = false`

### On Refresh

1. User clicks "?? Refresh" button
2. Executes `RefreshCommand`
3. Calls `LoadItems()` again
4. Updates UI with latest data

### On Print

1. User clicks "??? Print" button
2. Checks if items exist
3. Shows `PrintDialog`
4. If user confirms:
   - Calls `CreatePrintDocument()`
   - Generates FlowDocument with table
   - Sends to printer
   - Shows success message

### Print Document Structure

```
????????????????????????????????????????
?      New Buy Items Report            ?
?                                       ?
?         Generated: 2024-01-15         ?
?                                       ?
?      Total Items: 15                  ?
?                                       ?
????????????????????????????????????????
? Item  ? Description ? UOM  ?  Date   ?
????????????????????????????????????????
? COMP-1? Widget A    ? EA   ?01-01-24 ?
? COMP-2? Widget B    ? EA   ?01-02-24 ?
? ...   ? ...         ? ...  ? ...     ?
????????????????????????????????????????

      End of Report - 15 item(s)
```

---

## Error Handling

### Repository Level
```csharp
try
{
    // Execute SQL query
}
catch (Exception ex)
{
    _logger.LogError("Failed to retrieve new buy items", ex);
    throw;
}
```

### ViewModel Level
```csharp
try
{
    // Load items
}
catch (Exception ex)
{
    StatusMessage = $"Error loading items: {ex.Message}";
    MessageBox.Show($"Failed to load: {ex.Message}", "Error");
}
finally
{
    IsLoading = false;
}
```

### Print Level
```csharp
try
{
    // Print document
}
catch (Exception ex)
{
    MessageBox.Show($"Failed to print: {ex.Message}", "Print Error");
}
```

---

## UI Features

### Empty State
When no items exist:
- ?? Large icon displayed
- "No new buy items found" message
- Helper text explaining what to expect

### Loading State
- Semi-transparent black overlay
- Indeterminate progress bar
- "Loading new buy items..." text

### Statistics Badge
- Blue background (#2196F3)
- White text
- Shows total item count
- Updates after each load

### Status Bar
- Gray background (#F5F5F5)
- Shows current status message
- Displays last updated timestamp (right-aligned)

---

## Testing

### Manual Testing Checklist

1. **Empty State Test**
   ```sql
   -- Ensure no NewBuyItem records exist
   DELETE FROM isBOMImportBills WHERE Status = 'NewBuyItem';
   ```
   - Verify empty state message displays

2. **Data Load Test**
   ```sql
   -- Create test data
   UPDATE isBOMImportBills
   SET Status = 'NewBuyItem'
   WHERE Id IN (SELECT TOP 5 Id FROM isBOMImportBills);
   ```
   - Click Refresh
   - Verify items load correctly

3. **Print Test**
   - Load some items
   - Click Print button
   - Verify print dialog appears
   - Verify document prints correctly

4. **Refresh Test**
   - Load items
   - Add/remove items in database
   - Click Refresh
   - Verify changes reflected

5. **Error Handling Test**
   - Disconnect database
   - Try to load items
   - Verify error message displays
   - Reconnect and retry

---

## Integration Points

### Dependencies

**Required Services:**
- `NewItemService` - Provides data access
- `INewBuyItemRepository` - Data repository
- `ILoggerService` - Logging

**Required in DI Container (App.xaml.cs):**
```csharp
services.AddSingleton<INewBuyItemRepository>(sp => 
    new NewBuyItemRepository(GetConnectionString(), sp.GetRequiredService<ILoggerService>()));
services.AddSingleton<NewItemService>();
services.AddTransient<NewBuyItemsViewModel>();
```

### MainWindow Integration

```xml
<TabItem Header="New Buy Items">
    <views:NewBuyItemsView />
</TabItem>
```

---

## Configuration

### Database Connection
Uses the connection string from `appsettings.json`:
```json
{
  "DatabaseConnectionString": "Server=localhost;Database=MAS_AML;..."
}
```

### No Additional Configuration Required
All settings are hard-coded or derived from the database.

---

## Performance Considerations

### Query Optimization
- Query groups by ItemCode (reduces result set)
- Uses DISTINCT to avoid duplicates
- Includes COUNT for occurrence tracking

### Memory Usage
- Uses `ObservableCollection<object>` (efficient)
- Anonymous types from repository (lightweight)
- Print document generated on-demand

### UI Responsiveness
- Async data loading
- Loading overlay prevents interaction during load
- IsIndeterminate progress bar (no CPU overhead)

---

## Common Issues & Solutions

### Issue 1: No Items Display
**Cause:** No records with Status = 'NewBuyItem'
**Solution:** 
```sql
-- Check for NewBuyItem records
SELECT * FROM isBOMImportBills WHERE Status = 'NewBuyItem';
```

### Issue 2: Print Fails
**Cause:** No printer configured
**Solution:** Ensure a printer is installed and configured

### Issue 3: Slow Loading
**Cause:** Large number of records
**Solution:** Query is already optimized with grouping. Consider pagination if >1000 items.

### Issue 4: Compilation Error on `item.ItemCode`
**Cause:** Using `object` type for dynamic properties
**Solution:** Cast to `dynamic` when accessing properties:
```csharp
dynamic dyn = item;
var code = dyn.ItemCode;
```

---

## Future Enhancements

### Potential Features
1. **Export to Excel** - Save list to Excel file
2. **Filter by Date Range** - Show items from specific period
3. **Bulk Actions** - Mark multiple items for integration
4. **Item Details** - Expandable row with more info
5. **Search/Filter** - Real-time search in grid
6. **Sort Persistence** - Remember user's sort preferences

### Performance Improvements
1. **Pagination** - Load items in pages
2. **Virtual Scrolling** - Only render visible rows
3. **Caching** - Cache results for faster refresh

---

## Related Files

### Core Implementation
- `NewBuyItemRepository.cs` - Data access
- `NewBuyItemsViewModel.cs` - Business logic
- `NewBuyItemsView.xaml` - UI layout
- `NewItemService.cs` - Service layer

### Dependencies
- `INewBuyItemRepository.cs` - Interface
- `ILoggerService.cs` - Logging interface
- `NewBuyItem.cs` - Entity (not used directly)

### Configuration
- `App.xaml.cs` - DI registration
- `appsettings.json` - Database connection

---

## Best Practices

### Data Binding
? Use ObservableCollection for auto-update
? Use INotifyPropertyChanged (via ObservableObject)
? Bind to properties, not fields

### Error Handling
? Try-catch in all async methods
? Log errors with ILoggerService
? Show user-friendly messages

### UI/UX
? Show loading indicators
? Provide empty states
? Give feedback on actions (success/error)

### Performance
? Use async/await for database calls
? Group data in SQL queries
? Load data on background threads

---

## Summary

The New Buy Items View is now fully implemented with:
- ? Professional data grid
- ? Print functionality
- ? Statistics display
- ? Refresh capability
- ? Error handling
- ? Loading states
- ? Empty states
- ? Logging integration

The view queries `isBOMImportBills` table for items with Status = 'NewBuyItem' and displays them in a user-friendly grid with the ability to print reports.

---

**Implementation Date:** 2024-01-15
**Status:** ? Complete
**Build:** ? Successful
