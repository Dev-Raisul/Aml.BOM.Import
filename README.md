# Aml.BOM.Import - BOM Import Item Creation Utility

## Overview
This WPF desktop application is a single-user Windows utility that validates BOM imports, identifies new buy/make items, manages editable make-item data, and integrates eligible BOMs into Sage.

## Solution Structure

### Projects

#### 1. **Aml.BOM.Import.Domain**
Core domain layer containing business entities and enums.
- **Entities**: BomImportRecord, BomImportLine, NewBuyItem, NewMakeItem, SageItem
- **Enums**: BomIntegrationStatus, ItemIntegrationStatus, ItemType
- **Dependencies**: None (pure domain logic)

#### 2. **Aml.BOM.Import.Shared**
Shared contracts and interface definitions.
- **Interfaces**: Repository and service contracts
  - IBomImportRepository
  - INewMakeItemRepository
  - INewBuyItemRepository
  - ISageItemRepository
  - IBomValidationService
  - IBomIntegrationService
  - IFileImportService
  - ISettingsService
- **Dependencies**: None

#### 3. **Aml.BOM.Import.Application**
Application services and use case orchestration.
- **Services**: BomImportService, NewItemService, IntegrationService
- **Models**: AppSettings, ImportFileRequest, ImportFileResponse, ValidationResult
- **Dependencies**: Domain, Shared

#### 4. **Aml.BOM.Import.Infrastructure**
Infrastructure implementation layer.
- **Repositories**: Concrete implementations of repository interfaces
- **Services**: File import, validation, integration, settings services
- **External Integration**: SQL Server, Sage integration placeholders
- **Dependencies**: Domain, Shared, Application

#### 5. **Aml.BOM.Import.UI**
WPF desktop application with MVVM architecture.
- **ViewModels**: MainWindowViewModel, NewBuyItemsViewModel, NewMakeItemsViewModel, NewBomsViewModel, IntegratedBomsViewModel, DuplicateBomsViewModel, SettingsViewModel
- **Views**: Corresponding UserControl views for each section
- **Converters**: BoolToVisibilityConverter, StringToVisibilityConverter
- **Styles**: Centralized WPF styles in AppStyles.xaml
- **Dependency Injection**: Configured in App.xaml.cs using Microsoft.Extensions.DependencyInjection
- **Dependencies**: All other projects

#### 6. **Aml.BOM.Import.Tests**
Unit test project using xUnit.
- **Domain Tests**: Entity and business logic tests
- **Application Tests**: Service and workflow tests
- **Mocking**: Uses Moq for dependency mocking
- **Dependencies**: Application, Domain, Infrastructure

## Technology Stack

- **.NET 8.0** (WPF targets net8.0-windows)
- **C# with nullable reference types**
- **WPF** for UI
- **MVVM pattern** using CommunityToolkit.Mvvm
- **Dependency Injection** using Microsoft.Extensions.DependencyInjection
- **SQL Server** (via Microsoft.Data.SqlClient)
- **xUnit** for testing
- **Moq** for mocking

## Key Features (Planned)

### Core Functionality
- Upload and import BOM files (CSV, Excel)
- Validate BOMs against existing Sage item data
- Identify duplicate BOMs
- Identify new buy items and new make items
- Editable make-item fields with bulk editing
- Copy field values to filtered rows
- Copy-from-item functionality
- Track integration status (pending vs integrated)
- Store import metadata (file name, date, user)

### UI Sections
1. **New Buy Items** - View and manage identified buy items
2. **New Make Items** - Edit and manage make items with bulk operations
3. **New BOMs** - Import and validate BOMs
4. **Integrated BOMs** - View successfully integrated BOMs
5. **Duplicate BOMs** - Manage duplicate BOM entries
6. **Settings** - Configure database, Sage, and report settings

## Architecture

### Clean Architecture Principles
- **Domain Layer**: Pure business logic, no dependencies
- **Application Layer**: Use cases and workflow orchestration
- **Infrastructure Layer**: External concerns (database, file system, external APIs)
- **UI Layer**: WPF presentation logic using MVVM

### Dependency Flow
```
UI ? Application ? Domain
UI ? Infrastructure ? Application ? Domain
UI ? Infrastructure ? Domain
UI ? Shared
```

## Getting Started

### Prerequisites
- .NET 8.0 SDK
- Windows OS (WPF requirement)
- SQL Server (for persistence)
- Visual Studio 2022 or later (recommended)

### Building the Solution
```bash
dotnet restore
dotnet build
```

### Running the Application
```bash
dotnet run --project Aml.BOM.Import.UI
```

### Running Tests
```bash
dotnet test
```

## Configuration

Application settings are stored in:
`%APPDATA%\Aml.BOM.Import\appsettings.json`

Settings include:
- Database connection string
- Sage server URL and credentials
- Report output directory
- Auto-generate reports flag

## Next Steps for Development

### Immediate Tasks
1. Implement SQL database schema and migrations
2. Complete repository implementations with actual SQL queries
3. Implement file import logic (CSV/Excel parsing)
4. Implement BOM validation rules
5. Implement Sage integration logic
6. Add comprehensive error handling and logging
7. Implement reporting functionality

### Future Enhancements
- Add user authentication
- Implement audit logging
- Add advanced filtering and search
- Export functionality
- Batch processing improvements

## Contributing
This is an internal utility. All development should follow the established clean architecture patterns and maintain separation of concerns.

## Notes
- All TODOs in the code indicate areas requiring implementation
- Repository methods currently return placeholder objects
- Sage integration is stubbed and requires actual API integration
- Database schema needs to be created before the application can persist data
