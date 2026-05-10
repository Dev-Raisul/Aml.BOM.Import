# BOM Import Service - Quick Reference

## Quick Start

```csharp
// 1. Inject service
private readonly BomImportService _bomImportService;

// 2. Create request
var request = new ImportFileRequest
{
    FilePath = @"C:\BOM\MyBOM.xlsx",
    ImportedBy = Environment.UserName
};

// 3. Import file
var response = await _bomImportService.ImportBomFileAsync(request);

// 4. Check result
if (response.Success)
{
    Console.WriteLine($"Imported {response.ImportedRecords} records");
}
```

---

## Request Model

```csharp
public class ImportFileRequest
{
    public string FilePath { get; set; }    // Required: Full path to Excel file
    public string ImportedBy { get; set; }  // Optional: Username
}
```

---

## Response Model

```csharp
public class ImportFileResponse
{
    // Result
    public bool Success { get; set; }
    public string Message { get; set; }
    
    // File Info
    public int? FileId { get; set; }
    public string FileName { get; set; }
    
    // Import Stats
    public int ImportedRecords { get; set; }
    public int TabsProcessed { get; set; }
    
    // Validation Stats
    public int ValidatedRecords { get; set; }
    public int NewBuyItems { get; set; }
    public int NewMakeItems { get; set; }
    public int DuplicateBoms { get; set; }
    public int FailedRecords { get; set; }
    
    // Messages
    public List<string> Errors { get; set; }
    public List<string> Warnings { get; set; }
}
```

---

## Import Workflow

```
1. Validate Request
   ?
2. Check File Format (.xlsx/.xls)
   ?
3. Check Duplicate File
   ?
4. Import Excel ? Database
   ?
5. Auto-Validate (against Sage)
   ?
6. Return Results
```

---

## Error Messages

| Message | Meaning |
|---------|---------|
| "File path is required" | Empty request |
| "Invalid file format..." | Wrong extension or corrupted |
| "File already imported..." | Duplicate filename |
| "File not found..." | Path doesn't exist |
| "Import failed..." | Unexpected error |

---

## Common Patterns

### Pattern 1: Simple Import

```csharp
var response = await _bomImportService.ImportBomFileAsync(
    new ImportFileRequest { FilePath = path });

if (response.Success)
{
    Console.WriteLine($"? Imported {response.ImportedRecords} records");
}
```

### Pattern 2: With Error Handling

```csharp
try
{
    var response = await _bomImportService.ImportBomFileAsync(request);
    
    if (response.Success)
    {
        ShowSuccess(response);
    }
    else
    {
        ShowError(response.Message);
    }
}
catch (Exception ex)
{
    ShowError($"Error: {ex.Message}");
}
```

### Pattern 3: Display Results

```csharp
var response = await _bomImportService.ImportBomFileAsync(request);

var message = $"File: {response.FileName}\n" +
              $"Records: {response.ImportedRecords}\n" +
              $"Validated: {response.ValidatedRecords}\n" +
              $"New Items: {response.NewBuyItems + response.NewMakeItems}\n" +
              $"Duplicates: {response.DuplicateBoms}\n" +
              $"Failed: {response.FailedRecords}";

MessageBox.Show(message, "Import Complete");
```

---

## WPF Integration

```csharp
// ViewModel
public class NewBomsViewModel
{
    private readonly BomImportService _bomImportService;
    
    public ICommand ImportCommand { get; }
    
    public NewBomsViewModel(BomImportService bomImportService)
    {
        _bomImportService = bomImportService;
        ImportCommand = new RelayCommand(async () => await ImportAsync());
    }
    
    private async Task ImportAsync()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Excel Files|*.xlsx;*.xls"
        };
        
        if (dialog.ShowDialog() == true)
        {
            var request = new ImportFileRequest
            {
                FilePath = dialog.FileName
            };
            
            var response = await _bomImportService.ImportBomFileAsync(request);
            
            if (response.Success)
            {
                StatusMessage = $"Imported {response.ImportedRecords} records";
                await RefreshDataAsync();
            }
            else
            {
                MessageBox.Show(response.Message, "Import Failed");
            }
        }
    }
}
```

---

## Validation Status Meanings

| Status | Description |
|--------|-------------|
| **Validated** | Component exists in Sage, ready for integration |
| **NewBuyItem** | Component not in Sage, needs to be created as buy item |
| **NewMakeItem** | Component not in Sage, needs to be created as make item |
| **Duplicate** | Parent BOM already exists, will be ignored |
| **Failed** | Validation error (e.g., invalid quantity) |

---

## Performance Guidelines

| Records | Time (est) | Recommendation |
|---------|-----------|----------------|
| < 1,000 | < 2 sec | Normal import |
| 1,000 - 10,000 | < 30 sec | Show progress |
| > 10,000 | > 1 min | Consider splitting file |

---

## Troubleshooting

### Import Fails Immediately
- ? Check file exists at path
- ? Verify file extension (.xlsx or .xls)
- ? Ensure file is not open in Excel

### "File already imported"
- ? Rename file
- ? Or delete old entry from `isBOMImportFileLog`

### Import Hangs
- ? Check file size (max 50MB)
- ? Verify Excel file isn't corrupted
- ? Check number of records (max ~100,000)

### All Items "Not Found"
- ? Verify Sage database connection
- ? Check `CI_Item` table accessible
- ? Verify item codes match Sage format

---

## Testing Quick Check

```csharp
[Test]
public async Task Import_ValidFile_Success()
{
    // Arrange
    var request = new ImportFileRequest 
    { 
        FilePath = @"C:\test.xlsx" 
    };
    
    // Act
    var response = await _service.ImportBomFileAsync(request);
    
    // Assert
    Assert.IsTrue(response.Success);
    Assert.Greater(response.ImportedRecords, 0);
}
```

---

## Dependency Injection Setup

```csharp
// App.xaml.cs or Startup.cs
services.AddSingleton<IBomImportRepository, BomImportRepository>();
services.AddSingleton<IFileImportService, FileImportService>();
services.AddSingleton<IBomValidationService, BomValidationService>();
services.AddSingleton<BomImportService>();
```

---

## File Requirements

**Supported Formats**: `.xlsx`, `.xls`  
**Maximum Size**: 50 MB  
**Required Columns**: Component/Item Code  
**Optional Columns**: Parent, Description, Quantity, UOM, etc.

---

## What Happens During Import?

1. **File Log Created** ? `isBOMImportFileLog` table
2. **Data Extracted** ? All worksheets/tabs parsed
3. **Records Created** ? `isBOMImportBills` table
4. **Validation Runs** ? Against Sage `CI_Item` table
5. **Status Updated** ? Validated/NewBuyItem/Duplicate/Failed
6. **Results Returned** ? Statistics and messages

---

## Example Excel Structure

| Parent Item | Component | Quantity | UOM |
|-------------|-----------|----------|-----|
| ASSY-001 | PART-001 | 2 | EA |
| ASSY-001 | SCREW-M6 | 4 | EA |
| ASSY-002 | PART-002 | 1 | EA |

---

## Related Services

- **FileImportService** ? Handles Excel parsing
- **BomValidationService** ? Validates against Sage
- **BomImportBillRepository** ? Database access
- **ImportBomFileLogRepository** ? File logging

---

**Quick Tip**: The service handles everything automatically - just provide the file path and check the response!
