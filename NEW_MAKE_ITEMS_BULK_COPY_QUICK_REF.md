# New Make Items - Bulk Copy Prompt Quick Reference

## ?? What It Does

When you edit any cell, the system asks:
> **"Do you want to copy this value to all currently filtered items?"**

---

## ?? How It Works

```
Edit cell ? Press Enter ? Prompt appears ? Choose:
  ?? YES ? Copies to ALL filtered items
  ?? NO  ? Current row only + Disables prompt for that column
```

---

## ?? Quick Examples

### Example 1: Bulk Update

```
1. Filter: ItemCode = "ACL5%"  (25 items)
2. Edit Product Line ? "PL-ACL"
3. Press Enter
4. Prompt: "Copy to all?"
5. Click YES
Result: All 25 items have ProductLine = "PL-ACL"
```

### Example 2: Individual Updates

```
1. No filter (150 items)
2. Edit Product Line ? "PL-001"
3. Press Enter
4. Prompt: "Copy to all?"
5. Click NO
Result: Only current item updated
        Prompt disabled for "Product Line"
```

---

## ?? Prompt Behavior

### When Prompt Appears

? After editing any text field  
? After toggling any checkbox  
? Only if more than 1 item filtered  
? Only if prompt enabled for that column  

### When Prompt Doesn't Appear

? If you clicked "No" before (disabled)  
? If only 1 item in grid  
? During ESC (cancel edit)  

---

## ?? Disable/Enable Prompts

### Disable Prompt

```
1. Edit a cell
2. Click "No" on prompt
Result: Message appears
        "Future edits to 'Product Line' will not prompt"
        Only that column disabled
```

### Re-enable Prompt

```
Method 1: Right-click any cell
Method 2: Use context menu (Copy to all/Clear)
Result: Prompt re-enabled for that column
```

---

## ?? Per-Column Tracking

Prompt status is **independent per column**:

```
Column           | Prompt Status
-----------------|--------------
Product Line     | Disabled (said "No")
Product Type     | Enabled
Procurement      | Enabled
Staged           | Enabled
Coated           | Disabled (said "No")
```

Each column remembers your choice!

---

## ?? Pro Tips

### Tip 1: Use Filters

```
? Filter first (e.g., "ACL5%")
? Edit and copy to filtered set
? Clear filter
? Repeat for next group
```

### Tip 2: Test Small First

```
? Filter to 5-10 items
? Test bulk copy
? Verify results
? Then apply to larger sets
```

### Tip 3: Disable for Individual Edits

```
If you need different values per item:
1. Click "No" on first edit
2. Prompt disabled
3. Edit each item individually
4. No more prompts!
```

### Tip 4: Visual Confirmation

```
After "Yes":
- Status: "Updated X items"
- Statistics update
- Badge shows "? Edited"
```

---

## ?? Common Workflows

### Workflow 1: Group Update

```
Filter ? Edit ? YES ? All updated
Clear ? Filter ? Edit ? YES ? All updated
```

### Workflow 2: Mixed Updates

```
Filter ACL5% ? Edit ? YES ? 25 updated
Filter BRACKET% ? Edit ? YES ? 15 updated
Different values per group!
```

### Workflow 3: Individual Customization

```
Edit Item 1 ? NO ? Item 1 only
Edit Item 2 ? (no prompt) ? Item 2 only
Edit Item 3 ? (no prompt) ? Item 3 only
Each item unique!
```

---

## ?? Important Notes

### Filtered Items Only

Bulk copy affects **currently filtered items only**:
```
Total: 150 items
Filtered: 25 items

"Yes" updates: 25 items (not 150!)
```

### Column Independence

Disabling one column doesn't affect others:
```
Disabled: Product Line
Still works: Product Type, Staged, Coated
```

### Edit Must Complete

```
Type ? Press Enter/Tab ? Prompt
Not: While typing
```

---

## ?? Quick Test

```
1. Filter to 10 items
2. Edit Product Line ? "TEST"
3. Press Enter
4. Should see prompt
5. Click "Yes"
6. Verify all 10 items have "TEST"
7. Check statistics updated
```

? If works, feature is ready!

---

## ?? Dialog Messages

**Copy Prompt**:
```
?????????????????????????????????
?  Copy Value to All       [?]  ?
?                               ?
?  Do you want to copy this     ?
?  value to all currently       ?
?  filtered items?              ?
?                               ?
?    [ Yes ]      [ No ]        ?
?????????????????????????????????
```

**Prompt Disabled**:
```
?????????????????????????????????
?  Prompt Disabled         [i]  ?
?                               ?
?  Future edits to 'Product     ?
?  Line' will not prompt.       ?
?                               ?
?  To re-enable, use right-     ?
?  click context menu.          ?
?                               ?
?           [ OK ]              ?
?????????????????????????????????
```

---

## ?? Summary

| Action | Result |
|--------|--------|
| Edit ? YES | All filtered items updated |
| Edit ? NO | Current item only + Prompt disabled |
| Right-click | Prompt re-enabled |
| Filter ? Edit | Works on filtered items only |

**Key Point**: One edit can update many items! ??

---

**Full Documentation**: [NEW_MAKE_ITEMS_BULK_COPY_PROMPT_FEATURE.md](NEW_MAKE_ITEMS_BULK_COPY_PROMPT_FEATURE.md)
