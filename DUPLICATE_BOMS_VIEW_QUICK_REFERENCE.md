# Duplicate BOMs View - Quick Reference

## ?? What Are Duplicate BOMs?

BOMs marked as duplicates because:
- ? Parent item already exists in Sage BM_BillHeader
- ? Parent item exists in previous import file

**These BOMs will be ignored during integration**

---

## ?? Statistics Dashboard

| Metric | Meaning | Color |
|--------|---------|-------|
| **Duplicate BOMs** | Unique duplicate parent items | ?? Red |
| **Unique Parents** | Same as Duplicate BOMs | ?? Orange |
| **Total Records** | All duplicate component records | ? Black |

---

## ?? Search Features

**Search By**:
- Parent Item Code
- Parent Description
- Component Item Code  
- Component Description
- BOM Number
- File Name

**How**: Just type in the search box - results update live!

---

## ??? Actions Available

### Refresh
- Reloads duplicate BOMs from database
- Updates statistics

### Delete Selected
- Deletes ONE duplicate BOM
- Removes ALL records with that parent item
- **Requires confirmation**

### Delete All Duplicates
- Deletes ALL duplicate BOMs
- Removes ALL duplicate records
- ?? **Cannot be undone!**
- **Strong confirmation required**

---

## ?? Grid Columns

| Column | Shows |
|--------|-------|
| Parent Item Code | BOM parent item |
| Component Item Code | Component item code |
| Import File Name | Source import file |
| Import Date | When imported (yyyy-MM-dd HH:mm) |

**Simplified View**: Only essential columns shown for clarity

---

## ?? Visual Indicators

- **Pink/Red Background** - All duplicate rows
- **Darker Red on Select** - Selected row
- **Red Numbers** - Duplicate counts
- **Red Delete Button** - Caution: destructive

---

## ? Quick Workflow

### View Duplicates
```
1. Click "Duplicate BOMs" in nav
2. View statistics at top
3. Browse duplicates in grid
4. Use search to filter
```

### Delete Single Duplicate
```
1. Select a duplicate BOM
2. Click "Delete Selected"
3. Confirm deletion
4. Watch count update
```

### Delete All Duplicates
```
1. Click "Delete All Duplicates" (red button)
2. Read warning carefully!
3. Confirm if sure
4. All duplicates removed
```

---

## ?? Why BOMs Are Duplicate

### Already in Sage
```
Import: ASSY-001
Sage BM_BillHeader: ASSY-001 exists ?
Result: DUPLICATE
```

### Previous Import
```
Import 1: ASSY-001 (Jan.xlsx)
Import 2: ASSY-001 (Feb.xlsx)
Result: Second is DUPLICATE
```

### Same File, Different Tabs
```
Tab 1: ASSY-001
Tab 2: ASSY-001 (again!)
Result: Second is DUPLICATE
```

---

## ?? Example Scenarios

### Scenario 1: Check Duplicates After Import

```
Import completed: 100 records
Statistics show: 25 duplicate BOMs

Action:
1. Go to Duplicate BOMs View
2. See 25 unique parents
3. 150 total duplicate records
4. Review validation messages
```

### Scenario 2: Find Specific Duplicate

```
Need to find: "ASSY-001" duplicates

Action:
1. Type "ASSY-001" in search
2. See all related records
3. View which file imported it
4. Check validation message
```

### Scenario 3: Clean Up Duplicates

```
Want: Remove all duplicates

Action:
1. Review duplicate list
2. Click "Delete All Duplicates"
3. Confirm warning
4. All removed from database
```

---

## ?? Safety Features

### Confirmation Dialogs

**Delete Selected**:
```
"Delete duplicate BOM 'ASSY-001'?
 Will delete all 6 records."
```

**Delete All**:
```
"Delete ALL 25 duplicate BOMs?
 Will delete 150 records.
 CANNOT BE UNDONE!"
```

### Visual Warnings
- ?? Red delete button
- ?? Red statistics
- ?? Pink row background

---

## ?? Troubleshooting

### No duplicates shown?
- Check if any BOMs were imported
- Verify all BOMs are new (none in Sage)
- Click Refresh

### Can't delete?
- Check Settings ? Test Connection
- Verify database permissions
- Check logs

### Wrong items marked duplicate?
```sql
-- Verify in SQL Server
SELECT * FROM MAS_AML.dbo.BM_BillHeader 
WHERE BillNo = 'ASSY-001';
```

---

## ?? Tips

? **Search is powerful** - searches across all fields

? **Pink = Duplicate** - easy visual identification

? **Stats update** - after any deletion

? **Safe by design** - requires confirmations

? **Clean regularly** - keep database tidy

---

## ?? Related Views

| View | Purpose |
|------|---------|
| **New BOMs** | Shows non-duplicate BOMs |
| **Integrated BOMs** | Shows successfully integrated |
| **New Buy Items** | Shows items needing creation |

---

## ?? Key Takeaways

1. **Duplicates = Already Exist** (in Sage or previous import)
2. **Will Be Ignored** during integration
3. **Safe to Delete** from this view
4. **Search & Filter** to find specific items
5. **Statistics Show** overall duplicate count

---

**Quick Tip**: If you see duplicates, it means those BOMs already exist - no need to import them again! ??

**Full Documentation**: [DUPLICATE_BOMS_VIEW_IMPLEMENTATION_GUIDE.md](DUPLICATE_BOMS_VIEW_IMPLEMENTATION_GUIDE.md)
