# Phantom BOM Component Tracking - Implementation Complete

## Overview

The Phantom BOM View feature now automatically tracks phantom components during the import and validation process. Instead of creating phantom records manually, they are **automatically generated during file validation** when a phantom BOM (ProductType='P') is imported.

## How It Works

### 1. **Phantom Detection During Import**
When a BOM file is imported:
- The system detects if an item has `ProductType = 'P'` (indicating it's a phantom/assembly item)
- The phantom parent item is marked as validated since phantoms don't need to exist in CI_Item

### 2. **Automatic Phantom Component Recording (NEW)**
During the validation phase (`ValidateImportFileAsync`):
- For each phantom BOM component, a `PhantomBom` record is **automatically created**
- The component is checked against Sage `BM_BillHeader` table
- Status is set based on Sage existence:
  - **"Validated"** - Component exists in BM_BillHeader (ExistsInBillHeader = 1)
  - **"Missing Phantom"** - Component does NOT exist in BM_BillHeader (ExistsInBillHeader = 0)

### 3. **Phantom BOM View**
Users can view all phantom components in a dedicated screen:
- Lists all phantom components with their status
- Filter by component code, import filename, or show only missing phantoms
- Shows statistics: Total, Validated, Missing
- **Re-validation Option**: Click "Validate Phantoms" to check Sage again

## Database Schema

### isPhantomBoms Table
Stores phantom component records with the following structure:

```sql
CREATE TABLE isPhantomBoms (
    Id INT PRIMARY KEY,
    ImportFileName NVARCHAR(255),
    ImportDate DATETIME2,
    ImportWindowsUser NVARCHAR(100),
    TabName NVARCHAR(100),

    -- Component Details
    ComponentItemCode NVARCHAR(50),
    ComponentDescription NVARCHAR(255),
    ParentItemCode NVARCHAR(50),
    ParentDescription NVARCHAR(255),

    -- BOM Details
    BOMNumber NVARCHAR(50),
    BOMLevel NVARCHAR(20),
    LineNumber INT,
    Quantity DECIMAL(18,4),
    UnitOfMeasure NVARCHAR(20),

    -- Validation Status
    Status NVARCHAR(50), -- 'Validated' or 'Missing Phantom'
    ExistsInBillHeader BIT,
    ValidatedDate DATETIME2,

    -- Audit
    CreatedDate DATETIME2,
    ModifiedDate DATETIME2
)
```

## Code Changes

### 1. **BomValidationService.cs**
- Updated constructor to inject `IPhantomBomRepository`
- Modified `ValidateImportFileAsync()` method:
  - When a phantom BOM is detected:
    - Creates a `PhantomBom` record for each component
    - Checks component existence in Sage `BM_BillHeader`
    - Sets status to "Validated" or "Missing Phantom"
    - Saves to database automatically

### 2. **App.xaml.cs**
- Updated DI registration for `BomValidationService`
- Now passes `IPhantomBomRepository` to the constructor

### 3. **New Files**
- `CreateisPhantomBomsTable.sql` - Database table creation script
- `PhantomBom.cs` - Entity model for phantom component records
- `IPhantomBomRepository.cs` - Repository interface
- `PhantomBomRepository.cs` - Repository implementation with SQL access
- `PhantomBomsViewModel.cs` - View model for the UI
- `PhantomBomsView.xaml` - WPF UI for viewing/managing phantom components
- `PhantomBomsView.xaml.cs` - Code-behind

## User Workflow

### Step 1: Import Phantom BOM File
1. User imports an Excel file with a "Phantom" tab
2. System automatically detects phantom items (ProductType='P')
3. Phantom components are created in `isPhantomBoms` table during validation

### Step 2: View Phantom Components
1. Navigate to "Phantom BOMs" from the left menu
2. View all phantom components with their status:
   - ? **Validated** (Green) - Component exists in Sage BM_BillHeader
   - ?? **Missing Phantom** (Red) - Component NOT in Sage BM_BillHeader

### Step 3: Optional Re-validation
1. Click **"Validate Phantoms"** button
2. System queries Sage again for any newly created components
3. Updates statuses from "Missing Phantom" to "Validated" if found

### Step 4: Missing Component Resolution
- Users can create missing phantom components in Sage
- Re-run validation to confirm they're now in BM_BillHeader
- Once all phantom components are validated, BOM is ready to integrate

## Key Features

? **Automatic Detection**: Phantoms are detected and tracked without manual intervention  
? **Sage Validation**: Components are checked against actual Sage BM_BillHeader data  
? **Clear Status**: Visual indicators show validated vs. missing components  
? **Filtering**: Filter by component code, import file, or status  
? **Re-validation**: Check Sage again for newly created components  
? **Statistics**: Summary counts of validated and missing phantoms  

## SQL Script Installation

Run the following script to create the required table:

```bash
sqlcmd -S your_server -d MAS_AML -i CreateisPhantomBomsTable.sql
```

File location: `Aml.BOM.Import.Shared/Resources/Scripts/Tables/CreateisPhantomBomsTable.sql`

## Technical Details

### Validation Flow
```
Import File
    ?
Detect Phantom (ProductType = 'P')
    ?
For Each Phantom Component:
    - Check Sage BM_BillHeader for ComponentItemCode
    - If Found: Create PhantomBom with Status='Validated', ExistsInBillHeader=1
    - If NOT Found: Create PhantomBom with Status='Missing Phantom', ExistsInBillHeader=0
    ?
Save to isPhantomBoms Table
    ?
User can View/Filter in Phantom BOMs Screen
    ?
Optional: Re-validate to check for newly created components
```

### Repository Methods
- `GetAllAsync()` - Retrieve all phantom components
- `GetMissingPhantomsAsync()` - Get only "Missing Phantom" status records
- `GetValidatedPhantomsAsync()` - Get only "Validated" status records
- `UpdateStatusAsync(id, status, existsInBillHeader)` - Update component validation status
- `GetByFileNameAsync(fileName)` - Get components from specific import file

## Testing

### Test Case 1: Import Phantom BOM
1. Create Excel file with "Phantom" tab
2. Add parent item with ProductType='P'
3. Add component items in the phantom BOM
4. Import file
5. Verify PhantomBom records created in database
6. Check status reflects Sage existence

### Test Case 2: Re-validation
1. Create missing phantom component in Sage manually
2. Open Phantom BOMs view
3. Filter to show only "Missing Phantom"
4. Click "Validate Phantoms"
5. Verify component status updated to "Validated"

## Performance Considerations

- Phantom detection happens during validation (single pass)
- Sage BM_BillHeader check is optimized with indexed queries
- Database table has indexes on:
  - ImportFileName
  - Status
  - ComponentItemCode
  - ExistsInBillHeader
- Unique constraint prevents duplicate records

## Notes for Developers

1. **PhantomBom Records Auto-Creation**: Do NOT create these manually; they're generated during validation
2. **Sage Integration**: Uses `ISageItemRepository.BillExistsInBomHeaderAsync()` to check Sage
3. **Status Values**: Only two statuses: 'Validated' or 'Missing Phantom'
4. **ExistsInBillHeader**: Boolean flag mirrors Status (1='Validated', 0='Missing Phantom')
5. **ValidatedDate**: Set when component is found in Sage (null if missing)
