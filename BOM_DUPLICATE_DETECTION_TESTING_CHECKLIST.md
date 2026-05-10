# BOM Duplicate Detection - Testing Checklist

## Pre-Testing Setup

### 1. Database Verification
- [ ] Verify BM_BillHeader table exists
  ```sql
  SELECT TOP 10 BillNo FROM BM_BillHeader;
  ```
- [ ] Note down a few existing BillNo values for testing
- [ ] Verify isBOMImportBills table exists
  ```sql
  SELECT TOP 10 * FROM isBOMImportBills ORDER BY ImportDate DESC;
  ```

### 2. Application Setup
- [ ] Build solution (should be successful)
- [ ] Launch application
- [ ] Verify database connection works (Settings page)

---

## Test Case 1: Import New BOM (Not Duplicate)

### Test Data
Create Excel file with:
```
ParentItemCode | ComponentItemCode | Quantity | Description
NEW-BOM-TEST   | COMP-A           | 2        | Test Component A
NEW-BOM-TEST   | COMP-B           | 3        | Test Component B
```
Where `NEW-BOM-TEST` **does NOT exist** in BM_BillHeader

### Steps
- [ ] 1. Import the Excel file
- [ ] 2. Check import success message
- [ ] 3. Navigate to "New BOMs" view
- [ ] 4. Verify records show with Status = "Validated" or "NewBuyItem" (NOT "Duplicate")

### Expected Results
```
? Import successful
? Status = "Validated" or "NewBuyItem" (depends on component existence)
? ValidationMessage does NOT contain "Duplicate"
? Records appear in "New BOMs" view
? Records do NOT appear in "Duplicate BOMs" view
```

### SQL Verification
```sql
SELECT 
    ParentItemCode,
    ComponentItemCode,
    Status,
    ValidationMessage
FROM isBOMImportBills
WHERE ParentItemCode = 'NEW-BOM-TEST';
```

**Expected:** Status should NOT be "Duplicate"

---

## Test Case 2: Import Duplicate BOM (Exists in BM_BillHeader)

### Test Data
Create Excel file with:
```
ParentItemCode     | ComponentItemCode | Quantity | Description
<EXISTING-BILLNO>  | COMP-C           | 5        | Test Component C
<EXISTING-BILLNO>  | COMP-D           | 1        | Test Component D
```
Where `<EXISTING-BILLNO>` **EXISTS** in BM_BillHeader table

### Steps
- [ ] 1. Find existing BillNo from BM_BillHeader
  ```sql
  SELECT TOP 1 BillNo FROM BM_BillHeader;
  ```
- [ ] 2. Create Excel with that BillNo as ParentItemCode
- [ ] 3. Import the Excel file
- [ ] 4. Check import results
- [ ] 5. Navigate to "New BOMs" view
- [ ] 6. Check statistics (should show duplicate count)
- [ ] 7. Navigate to "Duplicate BOMs" view
- [ ] 8. Verify duplicate records appear

### Expected Results
```
? Import successful (doesn't fail)
? Status = "Duplicate" for ALL components
? ValidationMessage = "Duplicate BOM - Parent item already exists in BM_BillHeader table"
? Statistics show duplicate count > 0
? Records appear in "Duplicate BOMs" view
? Records do NOT appear in "New BOMs" view (or appear but marked as duplicate)
```

### SQL Verification
```sql
-- Verify duplicate detection
SELECT 
    ib.ParentItemCode,
    b.BillNo as FoundInSage,
    ib.ComponentItemCode,
    ib.Status,
    ib.ValidationMessage
FROM isBOMImportBills ib
LEFT JOIN BM_BillHeader b ON ib.ParentItemCode = b.BillNo
WHERE ib.ParentItemCode = '<EXISTING-BILLNO>';
```

**Expected:**
- `b.BillNo` should NOT be NULL (found in BM_BillHeader)
- `ib.Status` should be "Duplicate"
- `ib.ValidationMessage` should contain "BM_BillHeader table"

---

## Test Case 3: Mixed Import (Some Duplicate, Some New)

### Test Data
Create Excel file with:
```
ParentItemCode     | ComponentItemCode | Quantity | Description
<EXISTING-BILLNO>  | COMP-E           | 2        | Duplicate BOM Component
<EXISTING-BILLNO>  | COMP-F           | 3        | Duplicate BOM Component
NEW-BOM-MIXED      | COMP-G           | 4        | New BOM Component
NEW-BOM-MIXED      | COMP-H           | 1        | New BOM Component
```

### Steps
- [ ] 1. Import the Excel file
- [ ] 2. Check import statistics
- [ ] 3. Verify both duplicate and new BOMs are handled correctly

### Expected Results
```
? Import successful
? COMP-E and COMP-F: Status = "Duplicate"
? COMP-G and COMP-H: Status = "Validated" or "NewBuyItem"
? Statistics show both duplicates and validated records
```

### SQL Verification
```sql
SELECT 
    ParentItemCode,
    Status,
    COUNT(*) as Count
FROM isBOMImportBills
WHERE ImportFileName = '<your-test-file-name.xlsx>'
GROUP BY ParentItemCode, Status
ORDER BY ParentItemCode, Status;
```

---

## Test Case 4: Verify Logs

### Steps
- [ ] 1. Navigate to: `%APPDATA%\Aml.BOM.Import\Logs`
- [ ] 2. Open latest log file
- [ ] 3. Search for duplicate detection messages

### Expected Log Entries
```
? "Checking for duplicate BOM. Parent: <ParentItemCode>, BOM#: <BOMNumber>"
? "Checking if BillNo exists in BM_BillHeader: <ParentItemCode>"
? "Duplicate BOM detected - BillNo exists in BM_BillHeader: <ParentItemCode>"
? "Marked X bills as duplicate for parent: <ParentItemCode>"
```

---

## Test Case 5: UI Statistics Verification

### Steps
- [ ] 1. Import a file with known duplicates
- [ ] 2. Check "New BOMs" view statistics panel
- [ ] 3. Verify duplicate count is correct
- [ ] 4. Navigate to "Duplicate BOMs" view
- [ ] 5. Verify all duplicates are listed

### Expected Results
```
New BOMs View:
? Total: <correct count>
? Validated: <correct count>
? Duplicates: <correct count>
? New Items: <correct count>

Duplicate BOMs View:
? Shows all duplicate records
? Grouped by ParentItemCode
? Shows validation message
```

---

## Test Case 6: Revalidation

### Steps
- [ ] 1. Import a file
- [ ] 2. Manually insert a BOM in BM_BillHeader that matches an imported record
  ```sql
  INSERT INTO BM_BillHeader (BillNo) 
  VALUES ('<some-imported-ParentItemCode>');
  ```
- [ ] 3. Click "Revalidate" button in the application
- [ ] 4. Verify the record is now marked as duplicate

### Expected Results
```
? Revalidation completes successfully
? Previously "Validated" record now shows "Duplicate"
? ValidationMessage updated
```

---

## SQL Verification Queries

### Query 1: Check All Duplicates
```sql
SELECT 
    ib.ParentItemCode,
    b.BillNo as ExistsInBM_BillHeader,
    COUNT(*) as ComponentCount,
    ib.Status,
    ib.ValidationMessage
FROM isBOMImportBills ib
LEFT JOIN BM_BillHeader b ON ib.ParentItemCode = b.BillNo
WHERE ib.Status = 'Duplicate'
GROUP BY ib.ParentItemCode, b.BillNo, ib.Status, ib.ValidationMessage;
```

### Query 2: Verify Duplicate Detection Logic
```sql
-- This should show which BOMs would be duplicates
SELECT DISTINCT
    ib.ParentItemCode,
    b.BillNo,
    CASE 
        WHEN b.BillNo IS NOT NULL THEN 'DUPLICATE'
        ELSE 'NEW'
    END as DetectionResult
FROM isBOMImportBills ib
LEFT JOIN BM_BillHeader b ON ib.ParentItemCode = b.BillNo
WHERE ib.ParentItemCode IS NOT NULL
ORDER BY DetectionResult DESC, ib.ParentItemCode;
```

### Query 3: Status Summary
```sql
SELECT 
    Status,
    COUNT(DISTINCT ParentItemCode) as UniqueBOMs,
    COUNT(*) as TotalComponents
FROM isBOMImportBills
GROUP BY Status
ORDER BY Status;
```

---

## Performance Testing

### Test Large File
- [ ] Create Excel with 1000+ rows
- [ ] Multiple BOMs (some duplicate, some new)
- [ ] Import and verify:
  - [ ] Import completes successfully
  - [ ] Duplicate detection runs in reasonable time (<30 seconds)
  - [ ] All duplicates correctly identified
  - [ ] UI remains responsive

---

## Edge Cases

### Test Case 7: Empty ParentItemCode
```
ParentItemCode | ComponentItemCode | Quantity
<empty>        | COMP-X           | 1
```
**Expected:** Should NOT be marked as duplicate (ParentItemCode is null)

### Test Case 8: Null/Whitespace ParentItemCode
```
ParentItemCode | ComponentItemCode | Quantity
<whitespace>   | COMP-Y           | 1
```
**Expected:** Should NOT be marked as duplicate

### Test Case 9: Case Sensitivity
```
ParentItemCode | ComponentItemCode | Quantity
TEST-BOM-001   | COMP-Z           | 1
test-bom-001   | COMP-A           | 1
```
**Expected:** Verify if BillNo matching is case-sensitive or not

---

## Error Handling

### Test Case 10: Database Connection Issue
- [ ] Disconnect from database
- [ ] Attempt import
- [ ] Verify graceful error handling

### Test Case 11: BM_BillHeader Table Missing
- [ ] (Don't actually drop table, but verify error handling if query fails)
- [ ] Should log error but not crash application

---

## Rollback Test

### If Issues Found
- [ ] Document the issue
- [ ] Revert changes:
  ```
  git revert <commit-hash>
  ```
- [ ] Test with old logic
- [ ] Report findings

---

## Final Verification

- [ ] All test cases passed
- [ ] No compilation errors
- [ ] No runtime errors
- [ ] UI displays correctly
- [ ] Logs show correct messages
- [ ] SQL queries return expected results
- [ ] Performance is acceptable

---

## Sign-Off

**Tested By:** ___________________  
**Date:** ___________________  
**Status:** [ ] Pass [ ] Fail  
**Notes:**  
________________________________________________
________________________________________________
________________________________________________

---

## Reference Documents

- **Detailed Logic:** BOM_DUPLICATE_DETECTION_LOGIC.md
- **Quick Reference:** BOM_DUPLICATE_DETECTION_QUICK_REFERENCE.md
- **Visual Guide:** BOM_DUPLICATE_DETECTION_VISUAL_GUIDE.md
- **Implementation:** BOM_DUPLICATE_DETECTION_IMPLEMENTATION_SUMMARY.md
- **Changes:** BOM_DUPLICATE_DETECTION_CHANGES.md
