# New BOMs View Statistics - Quick Reference

## ?? Dashboard Metrics

| Metric | Status | Color | Action Needed |
|--------|--------|-------|---------------|
| **Total Pending** | All (except Integrated/Duplicate) | Black | Overview |
| **Ready to Integrate** | Validated | ?? Green | None - ready! |
| **New Make Items** | NewMakeItem | ?? Orange | Create make items |
| **New Buy Items** | NewBuyItem | ?? Blue | Create buy items |
| **Duplicates** | Duplicate | ? Gray | None - ignored |

---

## ? Quick Actions

```
Import File       ? Imports Excel file + validates
Revalidate All    ? Re-validates all pending BOMs
Refresh           ? Reloads data and statistics
```

---

## ?? What Each Count Means

### Total Pending
```
= New + Validated + NewBuyItem + NewMakeItem + Failed
```
**Excludes**: Integrated, Duplicate

### Ready to Integrate (Green)
- BOMs with Status = 'Validated'
- All components exist in Sage
- Can be integrated immediately

### New Make Items (Orange)
- BOMs with Status = 'NewMakeItem'
- Component items not found in Sage
- Need to create make items first

### New Buy Items (Blue)
- BOMs with Status = 'NewBuyItem'
- Component items not found in Sage
- Need to create buy items first

### Duplicates (Gray)
- BOMs with Status = 'Duplicate'
- Parent item already exists
- Will be ignored by system

---

## ?? When Statistics Update

? View loads  
? File imported  
? Revalidate clicked  
? Refresh clicked  

---

## ?? Workflow

```
1. Import File
   ?
2. Check Statistics
   ?
3. If New Items Required:
   ? Create items in Sage
   ? Click "Revalidate All"
   ?
4. When "Ready to Integrate" > 0:
   ? Go to Integration View
   ? Integrate BOMs
```

---

## ?? Status Values

| Status | Description | In Total? |
|--------|-------------|-----------|
| New | Just imported | ? Yes |
| Validated | Ready for integration | ? Yes |
| NewBuyItem | Need buy item | ? Yes |
| NewMakeItem | Need make item | ? Yes |
| Failed | Validation error | ? Yes |
| Duplicate | Already exists | ? No |
| Integrated | Complete | ? No |

---

## ?? Troubleshooting

### Statistics not updating?
1. Click "Refresh"
2. Check Settings ? Test Connection
3. Check logs: `%APPDATA%\Aml.BOM.Import\Logs\`

### Counts seem wrong?
```sql
-- Run in SQL Server Management Studio
SELECT Status, COUNT(*) 
FROM isBOMImportBills 
GROUP BY Status;
```

### Zero everywhere?
- No BOMs imported yet
- Or all BOMs are Integrated/Duplicate

---

## ?? Tips

? **Color Coding**: 
- Green = Good to go
- Orange/Blue = Action needed
- Gray = Ignore

? **Total Pending**: Main number to watch

? **After Import**: Check statistics to see validation results

? **Before Integration**: Ensure "Ready to Integrate" > 0

---

## ?? Quick Start

1. **Click "Import File"**
2. **Select Excel file**
3. **View statistics update**
4. **Take action based on counts**

---

## ?? Need Help?

- **Documentation**: [NEW_BOMS_VIEW_STATISTICS_GUIDE.md](NEW_BOMS_VIEW_STATISTICS_GUIDE.md)
- **Logs**: `%APPDATA%\Aml.BOM.Import\Logs\`
- **Database**: Check `isBOMImportBills` table

---

**Quick Tip**: Watch the "Ready to Integrate" count - that's how many BOMs you can process right now! ??
