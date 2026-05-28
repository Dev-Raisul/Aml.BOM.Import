# Parent Item Count for Total Pending and Ready to Integrate - Implementation Guide

## Summary

This guide shows how to add unique parent item counts for "Total Pending" and "Ready to Integrate" statistics in the New BOMs View.

---

## Step 1: Add Methods to Repository Interface

**File**: `Aml.BOM.Import.Shared\Interfaces\IBomImportBillRepository.cs`

The interface has already been updated with these methods:

```csharp
/// <summary>
/// Gets the count of distinct parent items for all pending BOMs (excluding Integrated and Duplicate)
/// </summary>
Task<int> GetPendingParentItemCountAsync();

/// <summary>
/// Gets the count of distinct parent items with Validated status (ready to integrate)
/// </summary>
Task<int> GetValidatedParentItemCountAsync();
```

---

## Step 2: Implement Methods in Repository

**File**: `Aml.BOM.Import.Infrastructure\Repositories\BomImportBillRepository.cs`

**Add these two methods** right after `GetParentItemCountByStatusAsync()` method and before the `// Helper methods` comment:

```csharp
public async Task<int> GetPendingParentItemCountAsync()
{
    _logger.LogDebug("Getting pending parent item count");

    const string sql = @"
        SELECT COUNT(DISTINCT ParentItemCode)
        FROM isBOMImportBills
        WHERE Status NOT IN ('Integrated', 'Duplicate')
          AND ParentItemCode IS NOT NULL";

    try
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        using var command = new SqlCommand(sql, connection);

        return (int)(await command.ExecuteScalarAsync() ?? 0);
    }
    catch (Exception ex)
    {
        _logger.LogError("Failed to get pending parent item count", ex);
        throw;
    }
}

public async Task<int> GetValidatedParentItemCountAsync()
{
    _logger.LogDebug("Getting validated parent item count");

    const string sql = @"
        SELECT COUNT(DISTINCT ParentItemCode)
        FROM isBOMImportBills
        WHERE Status = 'Validated'
          AND ParentItemCode IS NOT NULL";

    try
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        using var command = new SqlCommand(sql, connection);

        return (int)(await command.ExecuteScalarAsync() ?? 0);
    }
    catch (Exception ex)
    {
        _logger.LogError("Failed to get validated parent item count", ex);
        throw;
    }
}
```

---

## Step 3: Add Properties to ViewModel

**File**: `Aml.BOM.Import.UI\ViewModels\NewBomsViewModel.cs`

Add two new properties after the existing parent count properties:

```csharp
[ObservableProperty]
private int _totalPendingBomsParentCount;

[ObservableProperty]
private int _validatedBomsParentCount;
```

**Location**: Add these after the existing `_duplicateBomsParentCount` property.

---

## Step 4: Update LoadBomStatisticsAsync Method

**File**: `Aml.BOM.Import.UI\ViewModels\NewBomsViewModel.cs`

Update the `LoadBomStatisticsAsync()` method to load the new parent counts.

**Find this section**:
```csharp
// Get parent item counts for each status
NewMakeItemsParentCount = await _bomBillRepository.GetParentItemCountByStatusAsync("NewMakeItem");
NewBuyItemsParentCount = await _bomBillRepository.GetParentItemCountByStatusAsync("NewBuyItem");
DuplicateBomsParentCount = await _bomBillRepository.GetParentItemCountByStatusAsync("Duplicate");
```

**Add these two lines** after the existing parent count calls:
```csharp
// Get parent item counts for each status
NewMakeItemsParentCount = await _bomBillRepository.GetParentItemCountByStatusAsync("NewMakeItem");
NewBuyItemsParentCount = await _bomBillRepository.GetParentItemCountByStatusAsync("NewBuyItem");
DuplicateBomsParentCount = await _bomBillRepository.GetParentItemCountByStatusAsync("Duplicate");

// Get parent counts for pending and validated
TotalPendingBomsParentCount = await _bomBillRepository.GetPendingParentItemCountAsync();
ValidatedBomsParentCount = await _bomBillRepository.GetValidatedParentItemCountAsync();
```

---

## Step 5: Update the XAML View

**File**: `Aml.BOM.Import.UI\Views\NewBomsView.xaml`

### Update "Total Pending" Section

**Find**:
```xml
<!-- Total Pending -->
<StackPanel Grid.Column="0" HorizontalAlignment="Center">
    <TextBlock Text="Total Pending" 
              FontSize="12" 
              FontWeight="SemiBold" 
              Foreground="{StaticResource SecondaryTextBrush}" 
              HorizontalAlignment="Center"/>
    <TextBlock Text="{Binding TotalPendingBoms}" 
              FontSize="28" 
              FontWeight="Bold" 
              Foreground="{StaticResource PrimaryTextBrush}" 
              HorizontalAlignment="Center" 
              Margin="0,5,0,0"/>
</StackPanel>
```

**Replace with**:
```xml
<!-- Total Pending -->
<StackPanel Grid.Column="0" HorizontalAlignment="Center">
    <TextBlock Text="Total Pending" 
              FontSize="12" 
              FontWeight="SemiBold" 
              Foreground="{StaticResource SecondaryTextBrush}" 
              HorizontalAlignment="Center"/>
    <TextBlock Text="{Binding TotalPendingBoms}" 
              FontSize="28" 
              FontWeight="Bold" 
              Foreground="{StaticResource PrimaryTextBrush}" 
              HorizontalAlignment="Center" 
              Margin="0,5,0,0"/>
    <TextBlock HorizontalAlignment="Center" 
              Margin="0,5,0,0">
        <Run Text="{Binding TotalPendingBomsParentCount, Mode=OneWay}" 
             FontSize="11" 
             FontWeight="SemiBold" 
             Foreground="{StaticResource PrimaryTextBrush}"/>
        <Run Text=" parents" 
             FontSize="11" 
             Foreground="{StaticResource SecondaryTextBrush}"/>
    </TextBlock>
</StackPanel>
```

### Update "Ready to Integrate" Section

**Find**:
```xml
<!-- Ready to Integrate -->
<StackPanel Grid.Column="1" HorizontalAlignment="Center">
    <TextBlock Text="Ready to Integrate" 
              FontSize="12" 
              FontWeight="SemiBold" 
              Foreground="{StaticResource SecondaryTextBrush}" 
              HorizontalAlignment="Center" 
              TextWrapping="Wrap" 
              TextAlignment="Center"/>
    <TextBlock Text="{Binding ValidatedBomsCount}" 
              FontSize="28" 
              FontWeight="Bold" 
              Foreground="#4CAF50" 
              HorizontalAlignment="Center" 
              Margin="0,5,0,0"/>
</StackPanel>
```

**Replace with**:
```xml
<!-- Ready to Integrate -->
<StackPanel Grid.Column="1" HorizontalAlignment="Center">
    <TextBlock Text="Ready to Integrate" 
              FontSize="12" 
              FontWeight="SemiBold" 
              Foreground="{StaticResource SecondaryTextBrush}" 
              HorizontalAlignment="Center" 
              TextWrapping="Wrap" 
              TextAlignment="Center"/>
    <TextBlock Text="{Binding ValidatedBomsCount}" 
              FontSize="28" 
              FontWeight="Bold" 
              Foreground="#4CAF50" 
              HorizontalAlignment="Center" 
              Margin="0,5,0,0"/>
    <TextBlock HorizontalAlignment="Center" 
              Margin="0,5,0,0">
        <Run Text="{Binding ValidatedBomsParentCount, Mode=OneWay}" 
             FontSize="11" 
             FontWeight="SemiBold" 
             Foreground="#4CAF50"/>
        <Run Text=" parents" 
             FontSize="11" 
             Foreground="{StaticResource SecondaryTextBrush}"/>
    </TextBlock>
</StackPanel>
```

---

## Visual Result

### Before:
```
????????????????????????????????????????????????????????????????????????????????
?Total Pending ?Ready to Integrate?New Make Items  ?New Buy Items ?Duplicates  ?
?     150      ?        30        ?       10       ?      5       ?     20     ?
?              ?                  ?   3 parents    ?   1 parents  ?  8 parents ?
????????????????????????????????????????????????????????????????????????????????
```

### After:
```
????????????????????????????????????????????????????????????????????????????????
?Total Pending ?Ready to Integrate?New Make Items  ?New Buy Items ?Duplicates  ?
?     150      ?        30        ?       10       ?      5       ?     20     ?
?  45 parents  ?   12 parents     ?   3 parents    ?   1 parents  ?  8 parents ?
????????????????????????????????????????????????????????????????????????????????
```

---

## How It Works

### Total Pending Parents
- **Query**: Counts distinct `ParentItemCode` where Status is NOT 'Integrated' or 'Duplicate'
- **Meaning**: How many unique parent BOMs are still pending (waiting for processing/integration)

### Ready to Integrate Parents
- **Query**: Counts distinct `ParentItemCode` where Status = 'Validated'
- **Meaning**: How many unique parent BOMs are ready to be integrated into Sage

### Example Scenario

**Database**:
```
ParentItemCode | Status
---------------|------------
ASSY-001      | Validated
ASSY-001      | Validated  (multiple lines for same parent)
ASSY-002      | Validated
ASSY-003      | NewMakeItem
ASSY-004      | NewMakeItem
ASSY-005      | Duplicate
```

**Results**:
- Total Pending: 150 lines (all statuses except Integrated/Duplicate)
- Total Pending Parents: **3** (ASSY-001, ASSY-002, ASSY-003, ASSY-004 = 4 unique parents, but ASSY-005 is excluded because it's Duplicate)
- Ready to Integrate: 30 lines (Status = Validated)
- Ready to Integrate Parents: **2** (ASSY-001, ASSY-002)

---

## Testing Checklist

- [ ] Add methods to repository interface
- [ ] Implement methods in repository
- [ ] Add properties to ViewModel
- [ ] Update LoadBomStatisticsAsync in ViewModel
- [ ] Update XAML for Total Pending
- [ ] Update XAML for Ready to Integrate
- [ ] Build solution (should succeed)
- [ ] Run application
- [ ] Import a BOM file
- [ ] Verify parent counts display correctly
- [ ] Verify colors match parent statistics
- [ ] Click Refresh - counts should update

---

## Files to Modify

| File | Changes | Lines |
|------|---------|-------|
| `IBomImportBillRepository.cs` | ? Already done | +8 |
| `BomImportBillRepository.cs` | Add 2 methods | +50 |
| `NewBomsViewModel.cs` | Add 2 properties + update method | +8 |
| `NewBomsView.xaml` | Update 2 sections | +20 |

**Total**: 4 files, ~86 lines added

---

## Build & Test

1. **Build Solution**: Should succeed with no errors
2. **Run Application**
3. **Import BOM file** with nested BOMs
4. **Navigate to New BOMs View**
5. **Verify** parent counts show below Total Pending and Ready to Integrate
6. **Click Refresh** - verify counts update

---

## Summary

This implementation adds unique parent item counts to the "Total Pending" and "Ready to Integrate" statistics, providing users with insight into how many unique parent BOMs exist in each category.

? **2 new repository methods** for counting unique parents  
? **2 new ViewModel properties** for binding  
? **2 XAML updates** for displaying counts  
? **Color-coded** to match parent statistics  
? **Auto-refreshes** with other statistics  

---

**Status**: Ready for implementation  
**Complexity**: Low  
**Estimated Time**: 15-20 minutes
