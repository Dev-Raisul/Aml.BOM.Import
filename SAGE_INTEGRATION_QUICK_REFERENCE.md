# Sage 100 Integration - Quick Reference Guide

## ?? Quick Start

### 1. Configure Sage Settings

**Open Application** ? **Settings**

Enter the following:
- **Sage Path**: `C:\Sage\Sage100Standard\MAS90\Home` (adjust to your installation)
- **Username**: Your Sage 100 username
- **Password**: Your Sage 100 password
- **Company Code**: Your 3-character company code (e.g., "RAI")

Click **Save Settings**

---

### 2. Integrate New Make Items

**New Make Items View** ? Select items ? Click **Integrate**

The system will:
1. Load items from filtered list
2. Connect to Sage 100
3. Create each item in CI_Item table
4. Mark as integrated in database
5. Show success/failure counts

---

### 3. Integrate BOMs

**New BOMs View** ? Select BOM ? Click **Integrate**

The system will:
1. Create BOM header in BM_BillHeader
2. Create component lines in BM_BillDetail
3. Update integration status
4. Show results

---

## ?? Key Classes

### SageSessionService
**Location**: `Aml.BOM.Import.Infrastructure\Services\SageSessionService.cs`

**Purpose**: Manages Sage 100 ProvideX session

**Usage**:
```csharp
var settings = new SageSettings
{
    SagePath = @"C:\Sage\Sage100Standard\MAS90\Home",
    Username = "user",
    Password = "pass",
    CompanyCode = "RAI"
};

using var session = new SageSessionService(settings, logger);
session.InitializeSession();

var itemBus = session.CreateBusinessObject("CI_ItemCode_bus");
// Use itemBus...
```

---

### BomIntegrationService
**Location**: `Aml.BOM.Import.Infrastructure\Services\BomIntegrationService.cs`

**Purpose**: Implements Sage integration logic

**Methods**:
- `IntegrateNewItemsAsync(IEnumerable<int> itemIds)` - Create items in Sage
- `IntegrateBomAsync(int bomImportRecordId)` - Create BOM in Sage
- `GetIntegrationStatusAsync(int bomImportRecordId)` - Get status

**Usage**:
```csharp
// Inject via DI
private readonly IBomIntegrationService _integrationService;

// Integrate items
var result = await _integrationService.IntegrateNewItemsAsync(itemIds);

// Integrate BOM
var result = await _integrationService.IntegrateBomAsync(bomId);
```

---

## ?? Configuration

### Settings File Location
```
%APPDATA%\Aml.BOM.Import\appsettings.json
```

### Settings Format
```json
{
  "SageSettings": {
    "SagePath": "C:\\Sage\\Sage100Standard\\MAS90\\Home",
    "Username": "your_username",
    "Password": "your_password",
    "CompanyCode": "RAI"
  }
}
```

---

## ??? Sage Business Objects

### CI_ItemCode_bus (Create Items)
```csharp
var itemBus = session.CreateBusinessObject("CI_ItemCode_bus");
itemBus.nSetKey("ITEM001");
itemBus.nSetValue("ItemCodeDesc$", "Description");
itemBus.nSetValue("ProductLine$", "PL001");
itemBus.nSetValue("ItemType$", "1");
itemBus.nSetValue("ProcurementType$", "M");
itemBus.nWrite();
```

### BM_BillHeader_bus (Create BOM Header)
```csharp
var headerBus = session.CreateBusinessObject("BM_BillHeader_bus");
headerBus.nSetKey("PARENT001");
headerBus.nSetValue("BillDescription$", "Parent Assembly");
headerBus.nWrite();
```

### BM_BillDetail_bus (Create BOM Line)
```csharp
var detailBus = session.CreateBusinessObject("BM_BillDetail_bus");
detailBus.nSetKeyValue("BillNo$", "PARENT001");
detailBus.nSetKeyValue("ComponentItemCode$", "COMP001");
detailBus.nSetValue("QuantityPerBill", 5.0);
detailBus.nWrite();
```

---

## ?? Common Issues

### Issue: "Failed to create ProvideX.Script COM object"
**Solution**: Verify Sage 100 is installed and ProvideX is registered

### Issue: "nSetUser failed"
**Solution**: Check username/password in Sage settings

### Issue: "nSetCompany failed"
**Solution**: Verify company code (3 characters, case-sensitive)

### Issue: "nWrite failed: Required field missing"
**Solution**: Ensure ProductLine is populated for all items

### Issue: "Item already exists"
**Solution**: Item already created in Sage - check IsIntegrated flag

---

## ?? Workflow

### Item Integration Flow
```
User selects items
  ?
IntegrateNewItemsAsync() called
  ?
Load Sage settings
  ?
Initialize Sage session
  ?
For each item:
  Create CI_ItemCode_bus
  Set fields
  Write to Sage
  Mark as integrated
  ?
Return results
```

### BOM Integration Flow
```
User selects BOM
  ?
IntegrateBomAsync() called
  ?
Load BOM details from database
  ?
Initialize Sage session
  ?
Create BOM header
  ?
For each component:
  Create BOM detail line
  ?
Update integration status
  ?
Return results
```

---

## ?? Return Values

### IntegrateNewItemsAsync()
```csharp
Task<bool> // true if all items succeeded, false if any failed
```

Logs include:
- Success count
- Failure count
- Error messages for each failure

### IntegrateBomAsync()
```csharp
Task<bool> // true if BOM succeeded, false if failed
```

Updates database Status to "Integrated" on success

---

## ?? Testing

### Test 1: Session Initialization
```csharp
using var session = new SageSessionService(settings, logger);
session.InitializeSession();
// No exception = success
```

### Test 2: Create Test Item
```csharp
var itemIds = new[] { testItemId };
var result = await integrationService.IntegrateNewItemsAsync(itemIds);

// Verify in Sage:
SELECT * FROM CI_Item WHERE ItemCode = 'TESTITEM001'
```

### Test 3: Create Test BOM
```csharp
var result = await integrationService.IntegrateBomAsync(bomRecordId);

// Verify in Sage:
SELECT * FROM BM_BillHeader WHERE BillNo = 'PARENT001'
SELECT * FROM BM_BillDetail WHERE BillNo = 'PARENT001'
```

---

## ?? Code Examples

### Example 1: Manual Item Creation
```csharp
var settings = await settingsService.GetSettingsAsync() as AppSettings;
using var session = new SageSessionService(settings.SageSettings, logger);

session.InitializeSession();
session.SetProgramContext("CI_ItemMaintenance_ui");

dynamic itemBus = session.CreateBusinessObject("CI_ItemCode_bus");
try
{
    itemBus.nSetKey("TEST001");
    itemBus.nSetValue("ItemCodeDesc$", "Test Item");
    itemBus.nSetValue("ProductLine$", "0001");
    itemBus.nSetValue("ItemType$", "1");
    itemBus.nSetValue("ProcurementType$", "M");
    
    int retVal = itemBus.nWrite();
    if (retVal == 0)
    {
        Console.WriteLine("Error: " + itemBus.sLastErrorMsg);
    }
    else
    {
        Console.WriteLine("Item created successfully!");
    }
}
finally
{
    itemBus.DropObject();
    Marshal.ReleaseComObject(itemBus);
}
```

### Example 2: Using Integration Service
```csharp
// Much simpler - recommended approach
var itemIds = new[] { 1, 2, 3, 4, 5 };
var result = await integrationService.IntegrateNewItemsAsync(itemIds);

if (result)
{
    MessageBox.Show("All items integrated successfully!");
}
else
{
    MessageBox.Show("Some items failed. Check logs.");
}
```

---

## ?? Key Points

? **Session Order Matters**: Must call nSetUser, nSetCompany, nSetDate, nSetModule in exact order  
? **Check Return Values**: 0 = failure, non-zero = success  
? **Always Cleanup**: Call DropObject() and Marshal.ReleaseComObject()  
? **Use Dynamic**: COM objects must be `dynamic`, not strongly typed  
? **Date Format**: Use `DateTime.Today.ToString("yyyyMMdd")`  
? **Error Messages**: Read `sLastErrorMsg` on failures  

---

## ?? Support

**Full Documentation**: [SAGE_INTEGRATION_IMPLEMENTATION_SUMMARY.md](SAGE_INTEGRATION_IMPLEMENTATION_SUMMARY.md)

**VBS Reference**: `Aml.BOM.Import.Shared\Resources\TestVBSScript\Sage_ItemCreation_Test.vbs`

**Logs Location**: `%APPDATA%\Aml.BOM.Import\Logs\`

---

**Quick Reference Version**: 1.0  
**Last Updated**: 2024  
**Status**: ? Complete and Ready
