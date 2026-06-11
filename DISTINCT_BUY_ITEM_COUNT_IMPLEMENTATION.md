# Get Distinct Buy Item Count - Implementation

## Overview

Added a new method `GetDistinctBuyItemCountAsync()` to count **unique/distinct buy items** that need to be created in Sage. This method counts both component items and parent items (standalone) that have "NewBuyItem" status.

---

## Problem

When importing BOMs, we need to know how many **unique buy items** need to be created, not just the total number of records. A single buy item might appear multiple times across different BOMs:

### Example Scenario

**Data**:
```
Id | ParentItemCode | ComponentItemCode | Status
---|----------------|-------------------|------------
1  | ASSY-001      | SCREW-M5         | NewBuyItem
2  | ASSY-001      | WASHER-10        | NewBuyItem
3  | ASSY-002      | SCREW-M5         | NewBuyItem  <- Same as row 1
4  | ASSY-003      | BOLT-M8          | NewBuyItem
5  | NULL          | BRACKET-100      | NewBuyItem  <- Standalone parent
```

**Old Method (`GetCountByStatusAsync("NewBuyItem")`)**:
- Returns: **5** (counts all rows)
- Problem: SCREW-M5 counted twice

**New Method (`GetDistinctBuyItemCountAsync()`)**:
- Returns: **4** (unique items: SCREW-M5, WASHER-10, BOLT-M8, BRACKET-100)
- Benefit: Each buy item counted only once

---

## Implementation

### Repository Method

**File**: `Aml.BOM.Import.Infrastructure\Repositories\BomImportBillRepository.cs`

```csharp
public async Task<int> GetDistinctBuyItemCountAsync()
{
    _logger.LogDebug("Getting distinct buy item count (distinct components + distinct parents with NewBuyItem status)");

    const string sql = @"
        SELECT COUNT(DISTINCT ItemCode)
        FROM (
            -- Distinct component items with NewBuyItem status
            SELECT DISTINCT ComponentItemCode AS ItemCode
            FROM isBOMImportBills
            WHERE Status = 'NewBuyItem'
              AND ComponentItemCode IS NOT NULL
            
            UNION
            
            -- Distinct parent items with NewBuyItem status (standalone parents)
            SELECT DISTINCT ComponentItemCode AS ItemCode
            FROM isBOMImportBills
            WHERE Status = 'NewBuyItem'
              AND ParentItemCode IS NULL
              AND ComponentItemCode IS NOT NULL
        ) AS AllBuyItems";

    try
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        using var command = new SqlCommand(sql, connection);

        return (int)(await command.ExecuteScalarAsync() ?? 0);
    }
    catch (Exception ex)
    {
        _logger.LogError("Failed to get distinct buy item count", ex);
        throw;
    }
}
```

### Interface Addition

**File**: `Aml.BOM.Import.Shared\Interfaces\IBomImportBillRepository.cs`

```csharp
// Statistics
Task<int> GetCountByStatusAsync(string status);
Task<int> GetDistinctBuyItemCountAsync();  // ? NEW
Task<int> GetCountByFileNameAsync(string fileName);
Task<Dictionary<string, int>> GetStatusSummaryAsync();
```

---

## How It Works

### Two-Part Query

The query combines two distinct sets using **UNION**:

#### Part 1: Component Items
```sql
SELECT DISTINCT ComponentItemCode AS ItemCode
FROM isBOMImportBills
WHERE Status = 'NewBuyItem'
  AND ComponentItemCode IS NOT NULL
```
- Gets all distinct component items that are NewBuyItem
- Example: SCREW-M5, WASHER-10, BOLT-M8

#### Part 2: Standalone Parent Items
```sql
SELECT DISTINCT ComponentItemCode AS ItemCode
FROM isBOMImportBills
WHERE Status = 'NewBuyItem'
  AND ParentItemCode IS NULL
  AND ComponentItemCode IS NOT NULL
```
- Gets standalone parent items (no ParentItemCode) that are NewBuyItem
- Example: BRACKET-100 (a buy item used as assembly parent)

#### Final Count
```sql
SELECT COUNT(DISTINCT ItemCode)
FROM (Part1 UNION Part2) AS AllBuyItems
```
- UNION automatically removes duplicates
- COUNT(DISTINCT) ensures unique items only

---

## Example Scenarios

### Scenario 1: Regular Component Buy Items

**Data**:
```
ParentItemCode | ComponentItemCode | Status
---------------|-------------------|------------
ASSY-001       | SCREW-M5         | NewBuyItem
ASSY-001       | WASHER-10        | NewBuyItem
ASSY-002       | SCREW-M5         | NewBuyItem  <- Duplicate
ASSY-003       | BOLT-M8          | NewBuyItem
```

**Result**:
```csharp
GetCountByStatusAsync("NewBuyItem")          // 4 records
GetDistinctBuyItemCountAsync()               // 3 unique items (SCREW-M5, WASHER-10, BOLT-M8)
```

? **SCREW-M5 counted only once** even though it appears in 2 BOMs

### Scenario 2: With Standalone Parent Buy Items

**Data**:
```
ParentItemCode | ComponentItemCode | Status
---------------|-------------------|------------
ASSY-001       | SCREW-M5         | NewBuyItem
ASSY-001       | WASHER-10        | NewBuyItem
NULL           | BRACKET-100      | NewBuyItem  <- Standalone parent
NULL           | PANEL-200        | NewBuyItem  <- Standalone parent
```

**Result**:
```csharp
GetCountByStatusAsync("NewBuyItem")          // 4 records
GetDistinctBuyItemCountAsync()               // 4 unique items
```

? **Includes standalone parents** that are buy items

### Scenario 3: Mixed Duplicate Items

**Data**:
```
ParentItemCode | ComponentItemCode | Status
---------------|-------------------|------------
ASSY-001       | SCREW-M5         | NewBuyItem
ASSY-002       | SCREW-M5         | NewBuyItem  <- Duplicate
ASSY-003       | SCREW-M5         | NewBuyItem  <- Duplicate
NULL           | SCREW-M5         | NewBuyItem  <- Duplicate (as parent)
ASSY-001       | BOLT-M8          | NewBuyItem
```

**Result**:
```csharp
GetCountByStatusAsync("NewBuyItem")          // 5 records
GetDistinctBuyItemCountAsync()               // 2 unique items (SCREW-M5, BOLT-M8)
```

? **SCREW-M5 counted only once** even though it appears 4 times (3 as component, 1 as parent)

---

## SQL Breakdown

### Full Query Explained

```sql
SELECT COUNT(DISTINCT ItemCode)          -- Final count of unique items
FROM (
    -- Part 1: Component items
    SELECT DISTINCT ComponentItemCode AS ItemCode
    FROM isBOMImportBills
    WHERE Status = 'NewBuyItem'
      AND ComponentItemCode IS NOT NULL
    
    UNION                                 -- Combines and removes duplicates
    
    -- Part 2: Standalone parent items
    SELECT DISTINCT ComponentItemCode AS ItemCode
    FROM isBOMImportBills
    WHERE Status = 'NewBuyItem'
      AND ParentItemCode IS NULL
      AND ComponentItemCode IS NOT NULL
) AS AllBuyItems
```

### Why UNION?

**UNION automatically removes duplicates**, so if SCREW-M5 appears in both:
- Part 1 (as component): SCREW-M5
- Part 2 (as parent): SCREW-M5

The UNION will keep only **one instance** of SCREW-M5.

### NULL Filtering

`AND ComponentItemCode IS NOT NULL` ensures we don't count NULL values.

---

## Usage Examples

### Example 1: Count Unique Buy Items

```csharp
var totalRecords = await _bomBillRepository
    .GetCountByStatusAsync("NewBuyItem");

var uniqueItems = await _bomBillRepository
    .GetDistinctBuyItemCountAsync();

Console.WriteLine($"Total NewBuyItem records: {totalRecords}");
Console.WriteLine($"Unique buy items to create: {uniqueItems}");
Console.WriteLine($"Duplicate occurrences: {totalRecords - uniqueItems}");
```

**Output**:
```
Total NewBuyItem records: 25
Unique buy items to create: 18
Duplicate occurrences: 7
```

### Example 2: ViewModel Statistics

```csharp
public class NewBomsViewModel : ObservableObject
{
    [ObservableProperty]
    private int _newBuyItemsCount;          // Total records
    
    [ObservableProperty]
    private int _uniqueBuyItemsCount;       // Unique items
    
    private async Task LoadStatistics()
    {
        var statusSummary = await _bomBillRepository.GetStatusSummaryAsync();
        
        // Total buy item records
        NewBuyItemsCount = statusSummary.ContainsKey("NewBuyItem") 
            ? statusSummary["NewBuyItem"] : 0;
        
        // Unique buy items to create
        UniqueBuyItemsCount = await _bomBillRepository
            .GetDistinctBuyItemCountAsync();
    }
}
```

### Example 3: UI Display

```xml
<StackPanel>
    <TextBlock Text="New Buy Items"/>
    <TextBlock Text="{Binding NewBuyItemsCount}" FontSize="24"/>
    <TextBlock>
        <Run Text="{Binding UniqueBuyItemsCount}"/>
        <Run Text=" unique items"/>
    </TextBlock>
</StackPanel>
```

**Displays**:
```
New Buy Items
     25
18 unique items
```

### Example 4: Validation Before Creation

```csharp
public async Task<bool> ValidateBeforeBuyItemCreation()
{
    var uniqueCount = await _bomBillRepository.GetDistinctBuyItemCountAsync();
    
    if (uniqueCount == 0)
    {
        MessageBox.Show("No buy items to create.");
        return false;
    }
    
    var result = MessageBox.Show(
        $"Ready to create {uniqueCount} unique buy items in Sage.\n\nContinue?",
        "Create Buy Items",
        MessageBoxButton.YesNo);
    
    return result == MessageBoxResult.Yes;
}
```

---

## Comparison: Total vs Distinct

| Method | What It Counts | Use Case |
|--------|----------------|----------|
| `GetCountByStatusAsync("NewBuyItem")` | All NewBuyItem rows | Total occurrences across all BOMs |
| `GetDistinctBuyItemCountAsync()` | Unique buy items | How many items to create in Sage |

### Real-World Example

**Importing 3 BOMs**:
```
BOM 1: ASSY-001
  - SCREW-M5 (qty: 10)
  - WASHER-10 (qty: 10)

BOM 2: ASSY-002
  - SCREW-M5 (qty: 5)   <- Same as BOM 1
  - BOLT-M8 (qty: 2)

BOM 3: ASSY-003
  - SCREW-M5 (qty: 8)   <- Same as BOM 1 & 2
  - NUT-M5 (qty: 8)
```

**Results**:
```csharp
GetCountByStatusAsync("NewBuyItem")     // 6 records (3 SCREW-M5 + 3 others)
GetDistinctBuyItemCountAsync()          // 4 unique items (SCREW-M5, WASHER-10, BOLT-M8, NUT-M5)
```

**Action**: Create **4 buy items** in Sage, not 6.

---

## Performance

### Query Optimization

1. **DISTINCT in subqueries**: Reduces data before UNION
2. **UNION**: Automatically eliminates duplicates (efficient)
3. **COUNT(DISTINCT)**: Final deduplication (minimal overhead)

### Expected Performance

| Records | Unique Items | Query Time |
|---------|--------------|------------|
| 100 | 50 | < 50ms |
| 1,000 | 500 | < 100ms |
| 10,000 | 2,000 | < 500ms |

### Recommended Indexes

```sql
CREATE INDEX IX_isBOMImportBills_Status_ComponentItemCode
    ON isBOMImportBills(Status, ComponentItemCode)
    WHERE Status = 'NewBuyItem';

CREATE INDEX IX_isBOMImportBills_Status_ParentItemCode
    ON isBOMImportBills(Status, ParentItemCode)
    WHERE Status = 'NewBuyItem';
```

---

## Benefits

### ? **Accurate Item Count**
- Know exactly how many unique items to create
- No duplicate counting

### ? **Procurement Planning**
- Better purchasing decisions
- Accurate vendor orders

### ? **Time Savings**
- Don't create same item multiple times
- Single creation per unique item

### ? **Data Quality**
- Identify duplicate usage across BOMs
- Understand item reuse

### ? **User Communication**
- Clear messaging: "Create 18 unique items" vs "25 records"
- Set proper expectations

---

## Testing

### Test Case 1: No Duplicates

**Setup**:
```sql
INSERT INTO isBOMImportBills (ParentItemCode, ComponentItemCode, Status)
VALUES 
    ('ASSY-001', 'ITEM-A', 'NewBuyItem'),
    ('ASSY-001', 'ITEM-B', 'NewBuyItem'),
    ('ASSY-002', 'ITEM-C', 'NewBuyItem');
```

**Expected**:
```csharp
GetCountByStatusAsync("NewBuyItem")     // 3
GetDistinctBuyItemCountAsync()          // 3
```

### Test Case 2: With Duplicates

**Setup**:
```sql
INSERT INTO isBOMImportBills (ParentItemCode, ComponentItemCode, Status)
VALUES 
    ('ASSY-001', 'SCREW-M5', 'NewBuyItem'),
    ('ASSY-002', 'SCREW-M5', 'NewBuyItem'),  -- Duplicate
    ('ASSY-003', 'BOLT-M8', 'NewBuyItem');
```

**Expected**:
```csharp
GetCountByStatusAsync("NewBuyItem")     // 3
GetDistinctBuyItemCountAsync()          // 2 (SCREW-M5, BOLT-M8)
```

### Test Case 3: With Standalone Parents

**Setup**:
```sql
INSERT INTO isBOMImportBills (ParentItemCode, ComponentItemCode, Status)
VALUES 
    ('ASSY-001', 'SCREW-M5', 'NewBuyItem'),
    (NULL, 'BRACKET-100', 'NewBuyItem');  -- Standalone parent
```

**Expected**:
```csharp
GetCountByStatusAsync("NewBuyItem")     // 2
GetDistinctBuyItemCountAsync()          // 2 (SCREW-M5, BRACKET-100)
```

### Test Case 4: Item as Both Component and Parent

**Setup**:
```sql
INSERT INTO isBOMImportBills (ParentItemCode, ComponentItemCode, Status)
VALUES 
    ('ASSY-001', 'PANEL-A', 'NewBuyItem'),  -- As component
    (NULL, 'PANEL-A', 'NewBuyItem');        -- Same item as parent
```

**Expected**:
```csharp
GetCountByStatusAsync("NewBuyItem")     // 2
GetDistinctBuyItemCountAsync()          // 1 (PANEL-A counted once)
```

? **UNION ensures PANEL-A counted only once**

---

## Error Handling

```csharp
try
{
    return (int)(await command.ExecuteScalarAsync() ?? 0);
}
catch (Exception ex)
{
    _logger.LogError("Failed to get distinct buy item count", ex);
    throw;
}
```

**Handles**:
- Database connection errors
- SQL execution errors
- NULL results (returns 0)
- Logs all errors with context

---

## UI Integration Example

### Statistics Panel

```xml
<StackPanel Orientation="Vertical">
    <TextBlock Text="New Buy Items" FontWeight="Bold"/>
    <StackPanel Orientation="Horizontal">
        <TextBlock Text="{Binding NewBuyItemsCount}" FontSize="28"/>
        <TextBlock Text=" records" Margin="5,0,0,0" VerticalAlignment="Bottom"/>
    </StackPanel>
    <TextBlock Foreground="Gray">
        <Run Text="{Binding UniqueBuyItemsCount}"/>
        <Run Text=" unique items to create"/>
    </TextBlock>
</StackPanel>
```

**Displays**:
```
New Buy Items
  25 records
15 unique items to create
```

### Message Box

```csharp
var uniqueCount = await _bomBillRepository.GetDistinctBuyItemCountAsync();
var totalCount = await _bomBillRepository.GetCountByStatusAsync("NewBuyItem");

MessageBox.Show(
    $"Buy Items Summary:\n\n" +
    $"Total occurrences: {totalCount}\n" +
    $"Unique items: {uniqueCount}\n" +
    $"Duplicates: {totalCount - uniqueCount}\n\n" +
    $"You will create {uniqueCount} buy items in Sage.",
    "Buy Items",
    MessageBoxButton.OK);
```

---

## Related Methods

| Method | Purpose | Returns |
|--------|---------|---------|
| `GetCountByStatusAsync("NewBuyItem")` | Total NewBuyItem records | 25 |
| `GetDistinctBuyItemCountAsync()` | Unique buy items ? NEW | 18 |
| `GetParentItemCountByStatusAsync("NewBuyItem")` | Distinct parents | 5 |

---

## Future Enhancements

### Phase 1: Detailed Buy Item Analysis

```csharp
// Get list of distinct buy items with usage count
Task<Dictionary<string, int>> GetBuyItemUsageCountAsync();

// Example return:
{
    "SCREW-M5": 5,      // Used in 5 different BOMs
    "WASHER-10": 2,     // Used in 2 different BOMs
    "BOLT-M8": 1        // Used in 1 BOM
}
```

### Phase 2: Buy Item Details

```csharp
// Get distinct buy items with full details
Task<IEnumerable<BuyItemDetail>> GetDistinctBuyItemsAsync();

public class BuyItemDetail
{
    public string ItemCode { get; set; }
    public string Description { get; set; }
    public int UsageCount { get; set; }          // How many BOMs use it
    public List<string> UsedInBOMs { get; set; } // Which BOMs use it
}
```

---

## Summary

### What Was Added

? **New Method**: `GetDistinctBuyItemCountAsync()`
- Counts unique buy items (components + standalone parents)
- Uses UNION to combine and deduplicate
- No parameters needed (always uses "NewBuyItem" status)
- Handles NULL values properly

### Key Features

1. **Deduplication**: Same item counted once even if used in multiple BOMs
2. **Complete Coverage**: Includes both component and parent buy items
3. **Efficient Query**: Single SQL query with UNION
4. **NULL Safe**: Filters out NULL values
5. **Logging**: Debug and error logging

### Use Cases

- **Statistics Display**: Show unique vs total counts
- **Procurement**: Know exact number of items to purchase
- **UI Messaging**: "Create 18 unique buy items"
- **Validation**: Verify before creating items in Sage
- **Reporting**: Accurate buy item metrics

### Formula

```
Unique Buy Items = COUNT(DISTINCT(Component Buy Items ? Parent Buy Items))
```

Where:
- Component Buy Items: ComponentItemCode with Status='NewBuyItem'
- Parent Buy Items: ComponentItemCode with Status='NewBuyItem' AND ParentItemCode IS NULL
- ? (Union): Combines and deduplicates automatically

---

**Status**: ? Complete  
**Build**: ? Successful  
**Files Modified**: 2
- BomImportBillRepository.cs
- IBomImportBillRepository.cs

**Ready**: ? For Production Use

**Replaces**: `GetDistinctCountByStatusAsync(string status)` - More specific and accurate for buy items
