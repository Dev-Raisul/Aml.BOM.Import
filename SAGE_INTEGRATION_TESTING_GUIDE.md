# Sage Integration - Quick Testing Guide

## ? How to Test the Complete Integration

---

## ?? Quick Start Test

### Prerequisites:
1. ? Sage 100 installed on your machine
2. ? Application settings configured
3. ? Database connection working
4. ? Sample data in isBOMImportBills table

---

## ?? Step-by-Step Test

### **Step 1: Configure Sage Settings**

1. Launch application
2. Click **Settings** in navigation
3. Configure Sage Integration Settings:
   ```
   Sage Home Directory: C:\Sage\Sage100Standard\MAS90\Home
   Username: your_sage_username
   Password: your_sage_password
   Company Code: RAI (or your company code)
   ```
4. Click **Save Settings**

---

### **Step 2: Prepare Test Items**

1. Click **New Make Items** in navigation
2. You should see items from BOM imports
3. Filter items if needed (e.g., `FilterItemCode = TEST%`)
4. For each item, ensure **Product Line** is set:
   ```
   Product Line: 0001 (or any valid product line)
   ```
5. Other fields can be left as defaults or customized

---

### **Step 3: Integrate Items**

1. Verify items are ready:
   - ? Product Line is set
   - ? Items show in "Ready for Integration" count
   - ? Items are NOT already integrated

2. Click **Integrate** button

3. Confirm dialog appears:
   ```
   Ready to integrate X make items into Sage 100.
   This will create the items in Sage using COM integration.
   Continue?
   ```

4. Click **Yes**

5. Watch for:
   - Progress indicator (loading...)
   - Status message updates

---

### **Step 4: Verify Success**

#### In Application:
- ? Success message appears
- ? Items marked as integrated
- ? Statistics updated
- ? Ready for Integration count = 0

#### In Database:
```sql
SELECT TOP 10 
    ComponentItemCode,
    Status,
    DateIntegrated,
    IntegratedBy
FROM isBOMImportBills
WHERE Status = 'Integrated'
ORDER BY DateIntegrated DESC
```

Should show:
```
ComponentItemCode | Status      | DateIntegrated      | IntegratedBy
TEST001          | Integrated  | 2024-01-16 14:30:00 | DOMAIN\User
TEST002          | Integrated  | 2024-01-16 14:30:00 | DOMAIN\User
```

#### In Sage 100:
1. Open Sage 100
2. Go to **Inventory Management** ? **Item Maintenance**
3. Search for integrated items (e.g., TEST001)
4. Verify fields:
   - ? Item Code: TEST001
   - ? Description: (from application)
   - ? Product Line: 0001
   - ? Item Type: 1 (Regular)
   - ? Procurement Type: M (Make)
   - ? Standard UOM: EACH

---

## ?? Test Scenarios

### ? Test 1: Successful Integration

**What to do**:
- Configure settings correctly
- Set Product Line for items
- Click Integrate

**Expected**:
```
? Success message
? All items integrated
? Items in Sage 100
```

---

### ? Test 2: Missing Settings

**What to do**:
- Delete or clear Sage settings
- Try to integrate

**Expected**:
```
Configuration Required dialog:
"Sage 100 settings are not configured.
Please go to Settings and configure..."
```

---

### ?? Test 3: Missing Product Line

**What to do**:
- Leave Product Line empty
- Try to integrate

**Expected**:
```
Information dialog:
"No items are ready for integration.
Please ensure Product Line is set for items."
```

---

### ?? Test 4: Partial Success

**What to do**:
- Set invalid Product Line for some items (e.g., "INVALID")
- Set valid Product Line for others (e.g., "0001")
- Try to integrate

**Expected**:
```
Partial Success dialog:
"Integration completed with some errors:
Successful: 5
Failed: 3
Check the logs for details."
```

---

## ?? Troubleshooting

### Issue: "Failed to create ProvideX.Script COM object"

**Cause**: Sage 100 not installed or ProvideX not registered

**Fix**:
```powershell
# Verify Sage is installed
Test-Path "C:\Sage\Sage100Standard\MAS90\Home"

# Re-register ProvideX (run as admin)
regsvr32 "C:\Sage\Sage100Standard\MAS90\Home\PVXPlus.dll"
```

---

### Issue: "nSetUser failed"

**Cause**: Invalid username or password

**Fix**:
1. Verify credentials in Sage 100
2. Try logging into Sage 100 manually
3. Update settings with correct credentials

---

### Issue: "nSetCompany failed"

**Cause**: Invalid company code

**Fix**:
1. Check company code in Sage 100 (3 characters)
2. Update settings with correct company code

---

### Issue: "nWrite failed: Required field missing"

**Cause**: Required Sage field not populated

**Fix**:
1. Ensure Product Line is set
2. Check Sage 100 field requirements
3. Set all required fields

---

## ?? Verification Checklist

After integration, verify:

### Application:
- [ ] Items show IsIntegrated = true
- [ ] IntegratedDate is set
- [ ] IntegratedBy is set
- [ ] Statistics updated
- [ ] Items hidden if "Show Integrated" unchecked

### Database:
- [ ] Status = 'Integrated'
- [ ] DateIntegrated populated
- [ ] IntegratedBy populated

### Sage 100:
- [ ] Items exist in CI_Item table
- [ ] Item Code matches
- [ ] Description matches
- [ ] Product Line matches
- [ ] Item Type = 1 (Regular)
- [ ] Procurement Type = M (Make)
- [ ] Standard UOM = EACH

### Logs:
- [ ] Check logs at: `%APPDATA%\Aml.BOM.Import\Logs\`
- [ ] Should see:
  ```
  Starting integration of X new make items
  Integrating item: ITEM001 - Description
  Item written to Sage successfully: ITEM001
  Integration complete: X succeeded, 0 failed
  ```

---

## ?? Quick Test Commands

### SQL Verification:
```sql
-- Check integrated items
SELECT COUNT(*) FROM isBOMImportBills WHERE Status = 'Integrated'

-- Check specific item
SELECT * FROM isBOMImportBills WHERE ComponentItemCode = 'TEST001'

-- Check integration history
SELECT 
    ComponentItemCode,
    DateIntegrated,
    IntegratedBy
FROM isBOMImportBills
WHERE Status = 'Integrated'
ORDER BY DateIntegrated DESC
```

### Sage Verification:
```sql
-- In Sage database (if accessible)
SELECT 
    ItemCode,
    ItemCodeDesc,
    ProductLine,
    ItemType,
    ProcurementType
FROM CI_Item
WHERE ItemCode LIKE 'TEST%'
ORDER BY ItemCode
```

---

## ?? Test Data Setup

If you need test data:

```sql
-- Insert test new make item
INSERT INTO isBOMImportBills (
    ImportFileName,
    ImportDate,
    ImportWindowsUser,
    TabName,
    Status,
    ParentItemCode,
    ComponentItemCode,
    ComponentDescription,
    Quantity,
    UnitOfMeasure,
    ItemExists,
    ItemType,
    CreatedDate,
    ModifiedDate
)
VALUES (
    'TestFile.xlsx',
    GETDATE(),
    SYSTEM_USER,
    'Sheet1',
    'NewMakeItem',
    'PARENT001',
    'TESTITEM001',
    'Test Make Item 001',
    1.0,
    'EACH',
    0,  -- Does not exist in Sage
    'M', -- Make item
    GETDATE(),
    GETDATE()
)
```

---

## ?? Expected User Experience

### Successful Integration:

```
User Action:
1. Opens New Make Items view
2. Sees 10 items ready
3. Clicks "Integrate"
4. Confirms dialog

System Response:
? "Integrating items into Sage 100..."
? Progress shown
? "Successfully integrated 10 items into Sage 100"

Result:
? Items created in Sage
? Database updated
? UI refreshed
? Statistics updated
```

### Failed Integration (Settings):

```
User Action:
1. Settings not configured
2. Clicks "Integrate"

System Response:
? "Configuration Required"
? "Please go to Settings and configure..."

Result:
! No integration attempted
! User directed to Settings
```

### Failed Integration (Data):

```
User Action:
1. Product Line not set
2. Clicks "Integrate"

System Response:
? "No items are ready for integration"
? "Please ensure Product Line is set"

Result:
! No integration attempted
! User directed to fix data
```

---

## ? Success Criteria

Integration is successful if:

1. ? Items created in Sage 100 CI_Item table
2. ? Database updated to Status = 'Integrated'
3. ? Application shows integrated status
4. ? Statistics reflect changes
5. ? No errors in logs
6. ? Items visible in Sage UI

---

## ?? Quick Status Check

**Is integration working?**

Run this quick test:
1. Configure Sage settings ?
2. Set Product Line for 1 test item ?
3. Click Integrate ?
4. Check Sage for item ?

If item appears in Sage ? **Integration Working!** ?

If not ? Check:
- Settings correct?
- Logs for errors?
- Sage running?
- Permissions OK?

---

**Testing Guide**: ? Complete  
**Ready**: ? For Testing  
**Support**: See full documentation

?? **Happy Testing!** ??
