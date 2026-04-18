# Development Guide

## Quick Start

### Prerequisites Checklist
- [ ] .NET 8.0 SDK installed
- [ ] Visual Studio 2022 or later
- [ ] SQL Server (LocalDB, Express, or full version)
- [ ] Git (optional)

### First Time Setup

1. **Open the Solution**
   ```bash
   # Navigate to solution directory
   cd "D:\Freelance Work\Brad\Aml.BOM.Import"
   
   # Open in Visual Studio
   start Aml.BOM.Import.sln
   ```

2. **Restore NuGet Packages**
   - Visual Studio will automatically restore packages
   - Or manually: `dotnet restore`

3. **Build the Solution**
   - Press `Ctrl+Shift+B` in Visual Studio
   - Or: `dotnet build`

4. **Run the Application**
   - Set `Aml.BOM.Import.UI` as startup project
   - Press `F5` to run

## Development Workflow

### Adding a New Feature

1. **Define Domain Entities** (if needed)
   - Add to `Aml.BOM.Import.Domain/Entities`
   - Add enums to `Aml.BOM.Import.Domain/Enums`

2. **Define Interfaces**
   - Add repository interfaces to `Aml.BOM.Import.Shared/Interfaces`
   - Add service interfaces to `Aml.BOM.Import.Shared/Interfaces`

3. **Implement Application Services**
   - Add service to `Aml.BOM.Import.Application/Services`
   - Add DTOs to `Aml.BOM.Import.Application/Models`

4. **Implement Infrastructure**
   - Add repository to `Aml.BOM.Import.Infrastructure/Repositories`
   - Add service to `Aml.BOM.Import.Infrastructure/Services`

5. **Register in DI Container**
   - Open `Aml.BOM.Import.UI/App.xaml.cs`
   - Add registration in `ConfigureServices`

6. **Create UI**
   - Add ViewModel to `Aml.BOM.Import.UI/ViewModels`
   - Add View to `Aml.BOM.Import.UI/Views`
   - Wire up in MainWindow if needed

7. **Write Tests**
   - Add tests to `Aml.BOM.Import.Tests`

### Database Development

#### Creating the Schema

1. Create a new SQL script file: `Database/Schema.sql`

```sql
-- Example schema
CREATE TABLE BomImportRecords (
    Id INT PRIMARY KEY IDENTITY(1,1),
    BomNumber NVARCHAR(50) NOT NULL,
    Description NVARCHAR(255),
    FileName NVARCHAR(255),
    ImportDate DATETIME2 NOT NULL,
    ImportedBy NVARCHAR(100),
    Status INT NOT NULL,
    IntegratedDate DATETIME2 NULL,
    IntegratedBy NVARCHAR(100) NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETDATE(),
    ModifiedDate DATETIME2 NOT NULL DEFAULT GETDATE()
);

-- Add more tables...
```

2. Update connection string in settings
3. Run migration scripts

#### Implementing Repository Methods

Example: Implementing `BomImportRepository.GetAllAsync()`

```csharp
public async Task<IEnumerable<BomImportRecord>> GetAllAsync()
{
    var results = new List<BomImportRecord>();
    
    using var connection = new SqlConnection(_connectionString);
    await connection.OpenAsync();
    
    using var command = new SqlCommand(@"
        SELECT Id, BomNumber, Description, FileName, ImportDate, 
               ImportedBy, Status, IntegratedDate, IntegratedBy,
               CreatedDate, ModifiedDate
        FROM BomImportRecords
        ORDER BY ImportDate DESC", connection);
    
    using var reader = await command.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        results.Add(MapFromReader(reader));
    }
    
    return results;
}

private BomImportRecord MapFromReader(SqlDataReader reader)
{
    return new BomImportRecord
    {
        Id = reader.GetInt32(0),
        BomNumber = reader.GetString(1),
        Description = reader.GetString(2),
        // ... map other fields
    };
}
```

### UI Development

#### Adding a New View

1. **Create ViewModel**
   ```csharp
   public partial class MyNewViewModel : ObservableObject
   {
       [ObservableProperty]
       private string _title = "My New View";
       
       [RelayCommand]
       private async Task LoadData()
       {
           // Load data
       }
   }
   ```

2. **Create View (XAML)**
   ```xml
   <UserControl x:Class="Aml.BOM.Import.UI.Views.MyNewView"
                xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
       <Grid>
           <TextBlock Text="{Binding Title}"/>
       </Grid>
   </UserControl>
   ```

3. **Register in DI**
   ```csharp
   services.AddTransient<MyNewViewModel>();
   ```

4. **Add Navigation**
   - Update MainWindow navigation if needed

### Testing

#### Writing Unit Tests

```csharp
public class MyServiceTests
{
    private readonly Mock<IMyRepository> _mockRepository;
    private readonly MyService _service;
    
    public MyServiceTests()
    {
        _mockRepository = new Mock<IMyRepository>();
        _service = new MyService(_mockRepository.Object);
    }
    
    [Fact]
    public async Task GetData_ReturnsExpectedResult()
    {
        // Arrange
        var expectedData = new List<MyEntity> { /* ... */ };
        _mockRepository
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(expectedData);
        
        // Act
        var result = await _service.GetData();
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedData.Count, result.Count());
    }
}
```

#### Running Tests

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true

# Run specific test
dotnet test --filter "FullyQualifiedName~MyServiceTests"
```

## Common Tasks

### Adding a NuGet Package

```bash
# Add to specific project
dotnet add Aml.BOM.Import.Infrastructure package Newtonsoft.Json

# Update package
dotnet add package Newtonsoft.Json --version 13.0.3
```

### Updating Database Connection String

Edit settings through the Settings UI or manually:
- Location: `%APPDATA%\Aml.BOM.Import\appsettings.json`

```json
{
  "DatabaseConnectionString": "Server=localhost;Database=AmlBomImport;Trusted_Connection=true;",
  "SageSettings": {
    "ServerUrl": "",
    "Username": "",
    "CompanyCode": ""
  },
  "ReportSettings": {
    "OutputDirectory": "",
    "AutoGenerateReports": false
  }
}
```

### Debugging Tips

1. **Set Breakpoints**
   - Click in left margin of code editor
   - Press F9 on line

2. **Debug Specific Project**
   - Right-click project ? Debug ? Start New Instance

3. **View Output**
   - Debug ? Windows ? Output
   - View ? Output Window

4. **Inspect Variables**
   - Hover over variables while debugging
   - Debug ? Windows ? Locals
   - Debug ? Windows ? Watch

### Performance Optimization

1. **Use async/await properly**
   - Don't block on async calls
   - Use `ConfigureAwait(false)` in libraries

2. **Database optimization**
   - Add proper indexes
   - Use parameterized queries
   - Batch operations when possible

3. **UI optimization**
   - Use virtualization for large lists
   - Debounce search/filter operations
   - Load data asynchronously

## Code Style Guidelines

### Naming Conventions

- **Classes**: PascalCase (`BomImportService`)
- **Interfaces**: PascalCase with I prefix (`IBomImportRepository`)
- **Methods**: PascalCase (`GetAllAsync`)
- **Properties**: PascalCase (`BomNumber`)
- **Fields**: _camelCase with underscore (`_connectionString`)
- **Parameters**: camelCase (`bomImportRecord`)
- **Local variables**: camelCase (`importedItems`)

### File Organization

- One class per file
- File name matches class name
- Organize using folders
- Keep related files together

### Comments

- XML documentation for public APIs
- Inline comments for complex logic
- TODO comments for future work
- Avoid obvious comments

```csharp
/// <summary>
/// Validates a BOM import record against Sage item data.
/// </summary>
/// <param name="bomRecord">The BOM record to validate</param>
/// <returns>Validation result with errors and warnings</returns>
public async Task<ValidationResult> ValidateBomAsync(BomImportRecord bomRecord)
{
    // TODO: Implement validation logic
    // - Check if all items exist in Sage
    // - Validate quantities
    // - Check for duplicates
}
```

## Troubleshooting

### Build Errors

**Error**: "Could not find SDK 'Microsoft.NET.Sdk'"
- **Solution**: Install .NET 8.0 SDK

**Error**: "The type or namespace name 'X' could not be found"
- **Solution**: Check project references and using statements

**Error**: Package restore failed
- **Solution**: Clear NuGet cache: `dotnet nuget locals all --clear`

### Runtime Errors

**Error**: "Unable to resolve service for type 'X'"
- **Solution**: Register service in DI container in App.xaml.cs

**Error**: "Null reference exception"
- **Solution**: Check for nullable types, use null-conditional operators

### Database Errors

**Error**: "Cannot open database"
- **Solution**: Check connection string and SQL Server is running

**Error**: "Invalid object name 'TableName'"
- **Solution**: Create database schema first

## Resources

### Documentation
- [.NET Documentation](https://docs.microsoft.com/dotnet/)
- [WPF Documentation](https://docs.microsoft.com/dotnet/desktop/wpf/)
- [MVVM Toolkit](https://learn.microsoft.com/windows/communitytoolkit/mvvm/introduction)

### Tools
- Visual Studio 2022
- SQL Server Management Studio
- Git for version control
- NuGet Package Manager

## Getting Help

1. Check the README.md for overview
2. Review code comments and TODO items
3. Check this development guide
4. Review existing tests for examples
5. Consult team members

## Version Control

### Recommended Git Workflow

```bash
# Create feature branch
git checkout -b feature/my-new-feature

# Make changes and commit
git add .
git commit -m "Add new feature"

# Push to remote
git push origin feature/my-new-feature

# Create pull request for review
```

### Commit Message Guidelines

- Use present tense ("Add feature" not "Added feature")
- First line should be descriptive but concise
- Reference issue numbers if applicable

```
Add BOM validation logic

- Implement validation rules
- Add unit tests
- Update documentation

Closes #123
```

## Next Steps

1. ? Solution scaffold complete
2. ?? Create database schema
3. ?? Implement repository methods
4. ?? Implement file import
5. ?? Implement validation logic
6. ?? Implement Sage integration
7. ?? Add error handling and logging
8. ?? Create reports
9. ?? User acceptance testing
10. ?? Deploy to production

---

Happy coding! ??
