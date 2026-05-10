# New Make Items View - Complete Implementation Guide (Part 1)

## Overview

The New Make Items View is a sophisticated data management interface that allows users to view, filter, edit, and integrate new make items identified during BOM imports. It provides powerful filtering, bulk editing, and integration capabilities.

---

## Features Summary

### 1. **Data Display**
- Editable data grid with all required columns
- Sort by any column
- Read-only system fields
- Editable business fields

### 2. **Advanced Filtering**
- Import File Name filter
- Import Date range filter
- Item Code wildcard search (% and ?)
- Edited items only
- Missing data only
- Integrated items toggle

### 3. **Bulk Operations**
- Copy value to all filtered items
- Copy value to blank items only
- Copy from Sage item
- Clear all edited data

### 4. **Context Menu Actions**
- Copy to all filtered blank items
- Copy to all filtered items
- Clear for all filtered items

### 5. **Integration**
- Integrate button for items with Product Line set
- Track integrated status
- View integrated items history

---

## Column Definitions

| Column | Default | Editable | Source | Description |
|--------|---------|----------|--------|-------------|
| Import File Name | System | No | Import process | Source Excel file |
| Import File Date | System | No | Import process | When imported |
| Item Code | From Import | No | Excel file | Item identifier |
| Item Description | From Import | Yes | Excel file | Item description |
| Product Line | Blank | Yes | Lookup to Sage | Product line (required for integration) |
| Product Type | F | Yes | Default | Finished goods type |
| Procurement | M | Yes | Default | Make procurement |
| Standard Unit of Measure | EACH | Yes | Default | UOM |
| Sub Product Family | Blank | Yes | Lookup to Sage | Sub family |
| Staged Item | Unchecked | Yes | Default | Staged flag |
| Coated | Unchecked | Yes | Default | Coated flag |
| Golden Standard | Unchecked | Yes | Default | Golden standard flag |

---

## Wildcard Search Patterns

### Wildcard Characters

| Character | Meaning | Example |
|-----------|---------|---------|
| `%` | Zero, one, or more characters | `ACL%` matches `ACL`, `ACL5`, `ACLXXXXX` |
| `?` | Exactly one character | `ACL?` matches `ACL5`, `ACLX` but not `ACL` |

### Example Patterns

#### Example 1: Starts With
```
Pattern: ACL5%
Matches: ACL5, ACL5-123, ACL5XXXXX
```

#### Example 2: Ends With
```
Pattern: %LS40
Matches: ACL5-LS40, TEST-LS40, LS40
```

#### Example 3: Contains
```
Pattern: %ACL5%
Matches: TEST-ACL5-END, ACL5, START-ACL5
```

#### Example 4: Complex Pattern
```
Pattern: ACL5??LS40%
Matches: ACL5XXLS40, ACL5XYLS40-END, ACL5ABLS40123
Does Not Match: ACL5XLS40 (only 1 char), ACL5XXXLS40 (3 chars)
```

Breaking down `ACL5??LS40%`:
- `ACL5` - Must start with these exact characters
- `??` - Must have exactly 2 characters (any characters)
- `LS40` - Must have these exact characters next
- `%` - Can have any characters after (or none)

---

## Filtering Workflow

### Filter Panel Layout

```
???????????????????????????????????????????????????????????????
? Filters                                                     ?
???????????????????????????????????????????????????????????????
? Import File: [__________________] [Browse...]              ?
? Date From: [__________] To: [__________]                   ?
? Item Code: [__________________] (Use % and ? wildcards)    ?
? [?] Edited Only  [?] Missing Data  [?] Show Integrated     ?
? [Apply Filters] [Clear Filters]                            ?
???????????????????????????????????????????????????????????????
```

### Filter Logic

```csharp
// Import File Name - Contains
if (!string.IsNullOrWhiteSpace(FilterImportFileName))
{
    filtered = filtered.Where(i => 
        i.ImportFileName.Contains(FilterImportFileName, 
            StringComparison.OrdinalIgnoreCase));
}

// Import Date Range - Between dates
if (FilterImportDateFrom.HasValue)
{
    filtered = filtered.Where(i => 
        i.ImportFileDate.Date >= FilterImportDateFrom.Value.Date);
}

if (FilterImportDateTo.HasValue)
{
    filtered = filtered.Where(i => 
        i.ImportFileDate.Date <= FilterImportDateTo.Value.Date);
}

// Item Code - Wildcard matching
if (!string.IsNullOrWhiteSpace(FilterItemCode))
{
    var pattern = ConvertWildcardToRegex(FilterItemCode);
    filtered = filtered.Where(i => 
        Regex.IsMatch(i.ItemCode, pattern, RegexOptions.IgnoreCase));
}

// Edited Only - Has been modified
if (FilterEditedOnly)
{
    filtered = filtered.Where(i => i.IsEdited);
}

// Missing Data - Product Line is blank
if (FilterMissingDataOnly)
{
    filtered = filtered.Where(i => 
        string.IsNullOrWhiteSpace(i.ProductLine));
}

// Integrated Items - Show/hide integrated
if (!ShowIntegratedItems)
{
    filtered = filtered.Where(i => !i.IsIntegrated);
}
```

---

## Bulk Edit Workflow

### Scenario 1: Edit with Prompt

```
User edits a cell (e.g., Product Line = "PL-001")
    ?
Dialog appears: "Copy this value to all filtered items?"
    ?
If YES:
    - Apply to ALL filtered items
    - Save to database
    ?
If NO:
    - Apply to this item only
    - Disable prompt for this column
```

### Scenario 2: Right-Click Menu

```
User right-clicks on edited cell
    ?
Context menu appears:
    - Copy to all filtered blank items
    - Copy to all filtered items
    - Clear for all filtered items
    ?
User selects option
    ?
Action applied to filtered items
    ?
Prompt re-enabled for future edits
```

### Scenario 3: Copy From Item

```
User clicks "Copy From Item" button
    ?
Item Search dialog opens (searches Sage CI_Item)
    ?
User searches and selects an item
    ?
System copies all fields (except description) to filtered items
    ?
Items marked as edited
    ?
Changes saved
```

---

## Integration Workflow

### Prerequisites

For an item to be ready for integration:
1. Product Line must be set (not blank)
2. Item must not be already integrated

### Integration Process

```
User clicks "Integrate" button
    ?
System counts items ready for integration:
    - Not integrated
    - Product Line is set
    ?
Confirmation dialog:
    "Ready to integrate X make items into Sage.
     This will create the items in Sage 100. Continue?"
    ?
If YES:
    For each item:
        - Create in Sage CI_Item
        - Set IsIntegrated = true
        - Set IntegratedDate = now
        - Set IntegratedBy = current user
    ?
Show results:
    "Integration complete:
     Successful: X
     Failed: Y"
    ?
Refresh grid
```

### Integration Status

| Status | Meaning | Action |
|--------|---------|--------|
| **Not Integrated** | New item, not yet in Sage | Can be edited and integrated |
| **Ready** | Has Product Line, ready to integrate | Can integrate |
| **Integrated** | Successfully integrated to Sage | View only (if filter enabled) |

---

## Data Validation

### Required Fields for Integration

```csharp
bool IsReadyForIntegration(NewMakeItem item)
{
    return !item.IsIntegrated 
        && !string.IsNullOrWhiteSpace(item.ProductLine);
}
```

### Field Validation Rules

| Field | Rule | Error Message |
|-------|------|---------------|
| Product Line | Required for integration | "Product Line is required" |
| Product Type | Must be valid type | "Invalid Product Type" |
| Procurement | Must be M (Make) | "Must be 'M' for Make items" |
| Standard UOM | Cannot be blank | "UOM is required" |

---

## User Interface Layout

### Main Layout

```
??????????????????????????????????????????????????????????????????
? [Refresh] [Copy From Item] [Clear All] [Integrate]            ?
??????????????????????????????????????????????????????????????????
? ?? Statistics                                                  ?
? Total: 150  ?  Edited: 45  ?  Ready: 30  ?  Missing Data: 20 ?
??????????????????????????????????????????????????????????????????
? ?? Filters                                                     ?
? File: [_______] Date: [____] to [____] Code: [_______]       ?
? [?] Edited  [?] Missing Data  [ ] Show Integrated            ?
? [Apply] [Clear]                                               ?
??????????????????????????????????????????????????????????????????
? ?? Make Items Grid (Editable)                                 ?
? File ? Date ? Code ? Desc ? Prod Line ? Type ? ... ?         ?
? [...rows with editable cells...]                             ?
??????????????????????????????????????????????????????????????????
? Status: Loaded 150 items, 30 ready for integration           ?
??????????????????????????????????????????????????????????????????
```

---

## Context Menu Implementation

### Right-Click Menu Structure

```xml
<DataGrid.ContextMenu>
    <ContextMenu>
        <MenuItem Header="Copy to all filtered (blank only)" 
                 Command="{Binding CopyToAllFilteredBlankCommand}"
                 CommandParameter="{Binding PlacementTarget.CurrentColumn.Header, 
                                   RelativeSource={RelativeSource AncestorType=ContextMenu}}"/>
        
        <MenuItem Header="Copy to all filtered items" 
                 Command="{Binding CopyToAllFilteredCommand}"
                 CommandParameter="{Binding PlacementTarget.CurrentColumn.Header, 
                                   RelativeSource={RelativeSource AncestorType=ContextMenu}}"/>
        
        <Separator/>
        
        <MenuItem Header="Clear for all filtered items" 
                 Command="{Binding ClearForAllFilteredCommand}"
                 CommandParameter="{Binding PlacementTarget.CurrentColumn.Header, 
                                   RelativeSource={RelativeSource AncestorType=ContextMenu}}"/>
    </ContextMenu>
</DataGrid.ContextMenu>
```

---

## Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| `Ctrl+R` | Refresh |
| `Ctrl+F` | Apply Filters |
| `Ctrl+Shift+F` | Clear Filters |
| `Ctrl+I` | Integrate |
| `Ctrl+C` | Copy from Item |
| `Delete` | Clear selected cells |
| `F5` | Refresh |

---

## Statistics Panel

### Real-Time Statistics

```csharp
private void UpdateStatistics()
{
    TotalItems = Items.Count;
    EditedItems = Items.Count(i => i.IsEdited);
    ReadyForIntegration = Items.Count(i => 
        !i.IsIntegrated && 
        !string.IsNullOrWhiteSpace(i.ProductLine));
    MissingDataItems = Items.Count(i => 
        string.IsNullOrWhiteSpace(i.ProductLine));
}
```

### Statistics Display

```
?????????????????????????????????????????????????????????????
? Total Items  ? Edited Items ? Ready to Int ? Missing Data ?
?     150      ?      45      ?      30      ?      20      ?
?  (All items) ?  (Modified)  ?  (Can integ) ? (Need data)  ?
?????????????????????????????????????????????????????????????
```

---

*Continued in Part 2...*
