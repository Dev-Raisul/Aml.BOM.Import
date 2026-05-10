# New Buy Items View - Testing Checklist

## Pre-Testing Setup

### 1. Verify Database
- [ ] SQL Server is running
- [ ] MAS_AML database exists
- [ ] isBOMImportBills table exists
- [ ] Can connect from application

### 2. Verify Build
- [ ] Solution builds successfully
- [ ] No compilation errors
- [ ] No warnings (except nullable reference warnings)

### 3. Application Setup
- [ ] Connection string configured in appsettings.json
- [ ] Application launches successfully
- [ ] MainWindow displays

---

## Test Case 1: Empty State

### Setup
```sql
-- Clear all NewBuyItem records
UPDATE isBOMImportBills 
SET Status = 'Validated' 
WHERE Status = 'NewBuyItem';
```

### Steps
1. [ ] Launch application
2. [ ] Click "New Buy Items" tab
3. [ ] Observe empty state

### Expected Results
- [ ] ?? Icon displays
- [ ] "No new buy items found" message shows
- [ ] Helper text displays
- [ ] No errors in UI
- [ ] Status bar shows appropriate message

---

## Test Case 2: Load with Data

### Setup
```sql
-- Create test data (5 distinct items)
UPDATE isBOMImportBills
SET Status = 'NewBuyItem'
WHERE Id IN (
    SELECT TOP 10 Id 
    FROM isBOMImportBills 
    WHERE Status != 'Duplicate'
);
```

### Steps
1. [ ] Click "Refresh" button in New Buy Items view
2. [ ] Wait for loading indicator
3. [ ] Observe loaded data

### Expected Results
- [ ] Loading overlay appears
- [ ] Progress bar animates
- [ ] "Loading new buy items..." message shows
- [ ] Data loads in grid
- [ ] Multiple rows display
- [ ] All columns populated
- [ ] Statistics badge shows correct count
- [ ] Status bar shows "Loaded X items"
- [ ] No duplicate rows (grouped by ItemCode)

### Verify Columns
- [ ] Item Code column populated
- [ ] Description column populated
- [ ] UOM column populated
- [ ] Identified Date formatted correctly (yyyy-MM-dd)
- [ ] Identified By shows username
- [ ] Occurrences column shows count > 0

---

## Test Case 3: Grid Functionality

### Steps
1. [ ] Click Item Code column header
2. [ ] Verify items sort ascending
3. [ ] Click Item Code header again
4. [ ] Verify items sort descending
5. [ ] Click Description header
6. [ ] Verify sorts by description
7. [ ] Click a row to select
8. [ ] Verify row highlights
9. [ ] Click different row
10. [ ] Verify selection moves

### Expected Results
- [ ] Sorting works on all columns
- [ ] Selection highlighting works
- [ ] Only one row selected at a time
- [ ] Headers show sort indicators (?/?)

---

## Test Case 4: Print Functionality - No Items

### Setup
```sql
-- Ensure no items
UPDATE isBOMImportBills SET Status = 'Validated' WHERE Status = 'NewBuyItem';
```

### Steps
1. [ ] Click Refresh button
2. [ ] Click "??? Print" button

### Expected Results
- [ ] MessageBox appears
- [ ] Message: "No items to print."
- [ ] MessageBox type: Information
- [ ] No print dialog appears

---

## Test Case 5: Print Functionality - With Items

### Setup
```sql
-- Create test data
UPDATE TOP (10) isBOMImportBills SET Status = 'NewBuyItem';
```

### Steps
1. [ ] Click Refresh button
2. [ ] Wait for items to load
3. [ ] Click "??? Print" button
4. [ ] Observe print dialog

### Expected Results
- [ ] Print dialog appears
- [ ] Shows available printers
- [ ] Can select printer
- [ ] Can choose number of copies
- [ ] Cancel button works

### If Printing (optional)
5. [ ] Select a PDF printer or physical printer
6. [ ] Click Print button
7. [ ] Wait for print job

### Expected Print Output
- [ ] Title: "New Buy Items Report"
- [ ] Generated date/time displays
- [ ] "Total Items: X" displays
- [ ] Table with headers prints
- [ ] All rows print
- [ ] Borders visible
- [ ] Footer: "End of Report - X item(s)"

### After Printing
- [ ] Success message displays
- [ ] Message: "Document sent to printer successfully."
- [ ] Returns to normal view

---

## Test Case 6: Statistics

### Setup
```sql
-- Known quantity
UPDATE isBOMImportBills SET Status = 'Validated';
UPDATE isBOMImportBills SET Status = 'NewBuyItem' WHERE Id IN (1,2,3,4,5);
```

### Steps
1. [ ] Click Refresh
2. [ ] Observe statistics badge

### Expected Results
- [ ] Blue badge displays
- [ ] Shows "Total Items: 5" (or actual count)
- [ ] Count matches row count in grid
- [ ] Updates after refresh

---

## Test Case 7: Refresh Functionality

### Setup
```sql
-- Start with 3 items
UPDATE isBOMImportBills SET Status = 'Validated';
UPDATE TOP (3) isBOMImportBills SET Status = 'NewBuyItem';
```

### Steps
1. [ ] Click Refresh
2. [ ] Note count (should be 3 or fewer grouped)
3. [ ] In SQL, add more items:
   ```sql
   UPDATE TOP (5) isBOMImportBills SET Status = 'NewBuyItem' WHERE Status = 'Validated';
   ```
4. [ ] Click Refresh again
5. [ ] Observe updated count

### Expected Results
- [ ] Count increases after adding items
- [ ] Grid updates with new items
- [ ] Statistics badge updates
- [ ] Status message updates
- [ ] No errors

---

## Test Case 8: Loading State

### Steps
1. [ ] Note: May need slower connection to observe
2. [ ] Click Refresh button
3. [ ] Quickly observe loading state

### Expected Results
- [ ] Semi-transparent black overlay appears
- [ ] Progress bar shows (indeterminate)
- [ ] "Loading new buy items..." text displays
- [ ] Cannot click grid or buttons during load
- [ ] Loading overlay disappears when complete

---

## Test Case 9: Status Bar

### Steps
1. [ ] Observe status bar at bottom
2. [ ] Perform Refresh
3. [ ] Check status message changes

### Expected Results
- [ ] Status bar always visible
- [ ] Shows current status:
  - "Ready" initially
  - "Loading new buy items..." during load
  - "Loaded X items" after successful load
  - Error message if load fails
- [ ] Right side shows timestamp
- [ ] Timestamp updates on operations

---

## Test Case 10: Error Handling - Database Down

### Setup
1. [ ] Stop SQL Server service

### Steps
1. [ ] Click Refresh button
2. [ ] Wait for error

### Expected Results
- [ ] Loading indicator appears
- [ ] Error occurs after timeout
- [ ] MessageBox displays with error
- [ ] Error message mentions connection/database
- [ ] Status bar shows error
- [ ] Application doesn't crash
- [ ] Can close error dialog
- [ ] Grid remains functional (shows previous data or empty)

### Recovery
1. [ ] Start SQL Server
2. [ ] Click Refresh
3. [ ] Verify data loads successfully

---

## Test Case 11: Error Handling - Invalid Data

### Setup
```sql
-- Create invalid data (NULL ItemCode)
INSERT INTO isBOMImportBills (
    ImportFileName, ImportDate, ImportWindowsUser, TabName, Status,
    ComponentItemCode, Quantity, LineNumber, CreatedDate, ModifiedDate
) VALUES (
    'Test', GETDATE(), 'TestUser', 'TestTab', 'NewBuyItem',
    NULL, 1, 1, GETDATE(), GETDATE()
);
```

### Steps
1. [ ] Click Refresh

### Expected Results
- [ ] Either:
  - [ ] Record skipped (NULL ItemCode filtered)
  - [ ] Error handled gracefully
  - [ ] Error message shown
- [ ] Application doesn't crash
- [ ] Other valid records still load

---

## Test Case 12: Large Data Set

### Setup
```sql
-- Create many items (100+)
DECLARE @i INT = 0;
WHILE @i < 100
BEGIN
    UPDATE TOP (1) isBOMImportBills 
    SET Status = 'NewBuyItem' 
    WHERE Status = 'Validated' AND ComponentItemCode IS NOT NULL;
    SET @i = @i + 1;
END
```

### Steps
1. [ ] Click Refresh
2. [ ] Observe loading time
3. [ ] Scroll through grid

### Expected Results
- [ ] Loads in reasonable time (<5 seconds)
- [ ] All items display
- [ ] Scrolling is smooth
- [ ] No lag in UI
- [ ] Statistics accurate
- [ ] Can still sort/select

---

## Test Case 13: Occurrence Count

### Setup
```sql
-- Create same item multiple times
UPDATE isBOMImportBills
SET ComponentItemCode = 'TEST-ITEM', Status = 'NewBuyItem'
WHERE Id IN (SELECT TOP 5 Id FROM isBOMImportBills);
```

### Steps
1. [ ] Click Refresh
2. [ ] Find TEST-ITEM row
3. [ ] Check Occurrences column

### Expected Results
- [ ] Only one row for TEST-ITEM (grouped)
- [ ] Occurrences column shows 5 (or actual count)
- [ ] Occurrences column:
  - [ ] Centered
  - [ ] Bold
  - [ ] Orange color (#FF5722)

---

## Test Case 14: Column Widths

### Steps
1. [ ] Observe column widths
2. [ ] Resize window
3. [ ] Observe column behavior

### Expected Results
- [ ] Item Code: 150px fixed
- [ ] Description: Expands to fill space
- [ ] UOM: 80px fixed
- [ ] Identified Date: 120px fixed
- [ ] Identified By: 120px fixed
- [ ] Occurrences: 100px fixed
- [ ] Description column grows/shrinks with window

---

## Test Case 15: Visual Appearance

### Checklist
- [ ] Header is bold, 24px font
- [ ] Toolbar has gray background
- [ ] Buttons have icons (??, ???)
- [ ] Statistics badge is blue (#2196F3)
- [ ] Grid header is blue (#2196F3) with white text
- [ ] Alternating rows have gray background (#F9F9F9)
- [ ] Selected row has light blue background (#E3F2FD)
- [ ] Status bar has gray background
- [ ] Overall appearance is professional

---

## Test Case 16: Integration with Other Views

### Steps
1. [ ] Load New Buy Items view
2. [ ] Switch to "New BOMs" tab
3. [ ] Switch back to "New Buy Items" tab
4. [ ] Verify data persists

### Expected Results
- [ ] Data remains loaded when switching tabs
- [ ] No automatic refresh when returning to tab
- [ ] Statistics still accurate
- [ ] Selection preserved (if applicable)

---

## Performance Benchmarks

### Acceptable Performance
- [ ] Load 10 items: <1 second
- [ ] Load 50 items: <2 seconds
- [ ] Load 100 items: <5 seconds
- [ ] Print 10 items: <5 seconds
- [ ] Sort any column: <1 second
- [ ] Select row: Instant
- [ ] Refresh: <3 seconds (typical)

---

## Accessibility

### Keyboard Navigation
- [ ] Tab key moves between controls
- [ ] Enter key clicks focused button
- [ ] Arrow keys navigate grid
- [ ] Space selects grid row
- [ ] Tab reaches all interactive elements

---

## Browser / OS Compatibility

### Windows 10/11
- [ ] Application launches
- [ ] All features work
- [ ] Print works with system printer

### Different Screen Resolutions
- [ ] 1920x1080: All elements visible
- [ ] 1366x768: Layout adapts
- [ ] 4K: Scaling appropriate

---

## Sign-Off

### Test Execution

**Tested By:** ___________________  
**Date:** ___________________  
**Environment:**
- [ ] Development
- [ ] Test
- [ ] Production

### Results Summary

**Total Test Cases:** 16  
**Passed:** _____  
**Failed:** _____  
**Skipped:** _____  

### Issues Found

| Issue # | Description | Severity | Status |
|---------|-------------|----------|--------|
| 1 |  |  |  |
| 2 |  |  |  |
| 3 |  |  |  |

### Overall Status
- [ ] ? Ready for Production
- [ ] ?? Minor Issues (can deploy)
- [ ] ? Major Issues (cannot deploy)

### Notes
_______________________________________________________________________
_______________________________________________________________________
_______________________________________________________________________

---

## Quick SQL Helpers

### View Current State
```sql
SELECT Status, COUNT(*) as Count
FROM isBOMImportBills
GROUP BY Status;
```

### Create Test Data
```sql
UPDATE TOP (10) isBOMImportBills 
SET Status = 'NewBuyItem'
WHERE Status != 'Duplicate';
```

### Clear All Test Data
```sql
UPDATE isBOMImportBills 
SET Status = 'Validated'
WHERE Status = 'NewBuyItem';
```

### View Unique Buy Items
```sql
SELECT DISTINCT ComponentItemCode, COUNT(*) as Occurrences
FROM isBOMImportBills
WHERE Status = 'NewBuyItem'
GROUP BY ComponentItemCode
ORDER BY ComponentItemCode;
```

---

**Testing Complete:** _____________________  
**Approved By:** _____________________  
**Date:** _____________________
