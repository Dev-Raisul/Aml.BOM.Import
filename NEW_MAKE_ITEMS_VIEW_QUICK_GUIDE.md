# New Make Items View - Quick Reference Card

## ?? Quick Overview

**Purpose**: Manage new make items identified during BOM imports before integration into Sage.

---

## ?? Statistics Dashboard

| Metric | Color | Meaning |
|--------|-------|---------|
| **Total Items** | Black | All items in current filter |
| **Edited Items** | ?? Orange | Items with user changes |
| **Ready to Integrate** | ?? Green | Has Product Line set |
| **Missing Data** | ?? Red | Product Line is blank |

---

## ?? Action Buttons

| Button | Action |
|--------|--------|
| **Refresh** | Reload all data |
| **Copy From Item** | Copy from Sage item |
| **Clear All** | Remove all edits |
| **Integrate** | Create items in Sage |

---

## ?? Filters

### Text Filters
- **Import File**: File name contains
- **Item Code**: Wildcard search (% and ?)

### Date Filters
- **Date From**: Minimum date
- **Date To**: Maximum date

### Quick Filters
- [?] **Edited Only** - Show modified items
- [?] **Missing Data** - Show items needing Product Line
- [?] **Show Integrated** - Include completed items

---

## ?? Grid Columns

### Read-Only (Gray Background)
1. Import File Name
2. Import Date
3. Item Code

### Editable (White Background)
4. Item Description
5. **Product Line** (Required! Red if blank)
6. Product Type (Default: F)
7. Procurement (Default: M)
8. Standard UOM (Default: EACH)
9. Sub Product Family
10. Staged (CheckBox)
11. Coated (CheckBox)
12. Golden Std (CheckBox)

### Status
- ?? New
- ? Edited (Orange)
- ? Done (Green)

---

## ?? Wildcard Search

| Pattern | Matches | Example |
|---------|---------|---------|
| `ACL5%` | Starts with ACL5 | ACL5, ACL5-001 |
| `%LS40` | Ends with LS40 | TEST-LS40 |
| `%ACL5%` | Contains ACL5 | A-ACL5-B |
| `ACL5??LS40%` | Complex pattern | ACL5XYLS40-END |

**Wildcards**:
- `%` = Zero or more characters
- `?` = Exactly one character

---

## ?? Editing Workflow

### Single Edit
```
1. Click cell
2. Type value
3. Press Enter
4. Dialog: "Copy to all?"
   - Yes ? Apply to all filtered
   - No ? This item only
```

### Bulk Edit (Right-Click)
```
Right-click cell ? Menu:
  - Copy to blank only
  - Copy to all items
  - Clear all
```

---

## ?? Integration Workflow

```
1. Set Product Line for items
2. Check "Ready to Integrate" count
3. Click "Integrate" button
4. Confirm dialog
5. Items created in Sage
6. Status changes to "? Done"
```

**Requirements**:
- Product Line must be set
- Item not already integrated

---

## ?? Quick Tips

### Tip 1: Fast Bulk Edit
```
1. Apply specific filter
2. Edit one cell
3. Click "Yes" to copy to all
4. Done!
```

### Tip 2: Find Missing Data
```
1. Check "Missing Data"
2. Click "Apply Filters"
3. See items needing Product Line
4. Bulk edit to add
```

### Tip 3: Review Changes
```
1. Check "Edited Only"
2. Review all changes
3. Verify before integration
```

### Tip 4: Complex Search
```
ACL5??LS40%
= ACL5 + 2 chars + LS40 + anything
```

### Tip 5: Safe Bulk Copy
```
Right-click ? "Copy to blank only"
= Updates only empty cells
= Keeps existing values
```

---

## ?? Visual Indicators

### Row Colors
- **White** - Normal item
- **Light Gray** - Alternating rows
- **Light Blue** - Mouse hover
- **Gray Italic** - Integrated item

### Cell Colors
- **Gray** - Read-only field
- **Red** - Missing Product Line
- **White** - Editable field

### Badges
- **(none)** - New item
- **? Edited** - Orange badge
- **? Done** - Green badge

---

## ?? Keyboard Shortcuts

| Key | Action |
|-----|--------|
| `Ctrl+R` | Refresh |
| `Ctrl+I` | Integrate |
| `Enter` | Commit edit |
| `Tab` | Next cell |
| `Escape` | Cancel edit |
| `F5` | Refresh |

---

## ?? Common Workflows

### Workflow 1: Simple Update
```
Open view ? Edit cells ? Integrate
```

### Workflow 2: Filtered Update
```
Apply filter ? Edit ? Copy to all ? Clear filter
```

### Workflow 3: Find Specific Items
```
Type wildcard ? Apply ? Edit ? Done
```

### Workflow 4: Bulk Product Line
```
Filter items ? Edit ProductLine ? Yes to all ? Integrate
```

---

## ?? Important Notes

### Before Integration
- ? Product Line MUST be set
- ? Review "Ready to Integrate" count
- ? Check "Missing Data" count

### After Integration
- Items marked as "? Done"
- Shown in gray/italic
- Can't be edited
- Can be hidden (uncheck "Show Integrated")

### Wildcard Examples
```
ACL5%       ? ACL5*
%LS40       ? *LS40
%ACL5%      ? *ACL5*
ACL5??LS40% ? ACL5??LS40*
```

---

## ?? Statistics Meaning

```
Total: 150      ? All items (after filter)
Edited: 45      ? Items with changes
Ready: 30       ? Can integrate now
Missing: 20     ? Need Product Line
```

**Goal**: Missing = 0, Ready = Total

---

## ?? Success Checklist

Before Integration:
- [ ] All items have Product Line
- [ ] Ready count > 0
- [ ] Missing count = 0
- [ ] Review edited items

After Integration:
- [ ] Success message shown
- [ ] Items marked as "Done"
- [ ] Statistics updated
- [ ] Grid refreshed

---

## ?? Quick Troubleshooting

**Can't integrate?**
- Check Product Line is set
- Verify "Ready" count > 0

**Filter not working?**
- Click "Apply Filters"
- Check wildcard syntax

**Bulk copy not prompting?**
- Edit cell and press Enter
- Prompt appears after first edit

**Can't edit cell?**
- Check if integrated (gray/italic)
- Verify not read-only column

---

## ?? More Help

**Full Documentation**:
- NEW_MAKE_ITEMS_VIEW_COMPLETE.md
- NEW_MAKE_ITEMS_IMPLEMENTATION_SUMMARY.md

**Wildcard Guide**:
- NEW_MAKE_ITEMS_VIEW_IMPLEMENTATION_PART1.md

---

**Quick Start**: Open view ? Edit Product Line ? Click "Integrate" ? Done! ?
