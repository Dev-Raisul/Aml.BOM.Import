-- =============================================
-- Create Table: isBOMImportBills
-- Description: Stores imported BOM data from Excel files
-- Database: MAS_AML
-- =============================================

USE MAS_AML;
GO

-- Drop table if exists (for development/testing)
IF OBJECT_ID('dbo.isBOMImportBills', 'U') IS NOT NULL 
    DROP TABLE dbo.isBOMImportBills;
GO

-- Create the BOM Import Bills table
CREATE TABLE dbo.isBOMImportBills
(
    -- Primary Key
    Id INT PRIMARY KEY IDENTITY(1,1),
    
    -- Import Metadata (Required)
    ImportFileName NVARCHAR(255) NOT NULL,
    ImportDate DATETIME2 NOT NULL DEFAULT GETDATE(),
    ImportWindowsUser NVARCHAR(100) NOT NULL,
    TabName NVARCHAR(100) NOT NULL,
    
    -- Status Information
    Status NVARCHAR(50) NOT NULL DEFAULT 'New',
    DateValidated DATETIME2 NULL,
    DateIntegrated DATETIME2 NULL,
    
    -- BOM Header Information
    ParentItemCode NVARCHAR(50) NULL,
    ParentDescription NVARCHAR(255) NULL,
    BOMLevel NVARCHAR(20) NULL,
    BOMNumber NVARCHAR(50) NULL,
    
    -- Component/Line Item Information
    LineNumber INT NOT NULL,
    ComponentItemCode NVARCHAR(50) NOT NULL,
    ComponentDescription NVARCHAR(255) NULL,
    Quantity DECIMAL(18,4) NOT NULL,
    UnitOfMeasure NVARCHAR(20) NULL,
    Reference NVARCHAR(100) NULL,
    Notes NVARCHAR(MAX) NULL,
    
    -- Additional Fields
    Category NVARCHAR(50) NULL,
    Type NVARCHAR(50) NULL,
    UnitCost DECIMAL(18,4) NULL,
    ExtendedCost DECIMAL(18,4) NULL,
    
    -- Validation Fields
    ItemExists BIT NOT NULL DEFAULT 0,
    ItemType NVARCHAR(20) NULL, -- Buy, Make
    ValidationMessage NVARCHAR(500) NULL,
    
    -- Audit Fields
    CreatedDate DATETIME2 NOT NULL DEFAULT GETDATE(),
    ModifiedDate DATETIME2 NOT NULL DEFAULT GETDATE(),
    
    -- Constraints
    CONSTRAINT CK_isBOMImportBills_Status CHECK (Status IN ('New', 'Validated', 'Integrated', 'NewBuyItem', 'NewMakeItem', 'Failed', 'Duplicate')),
    CONSTRAINT CK_isBOMImportBills_ItemType CHECK (ItemType IS NULL OR ItemType IN ('Buy', 'Make'))
);
GO

-- Create indexes for better query performance
CREATE INDEX IX_isBOMImportBills_ImportFileName 
    ON dbo.isBOMImportBills(ImportFileName);
GO

CREATE INDEX IX_isBOMImportBills_ImportDate 
    ON dbo.isBOMImportBills(ImportDate DESC);
GO

CREATE INDEX IX_isBOMImportBills_Status 
    ON dbo.isBOMImportBills(Status);
GO

CREATE INDEX IX_isBOMImportBills_TabName 
    ON dbo.isBOMImportBills(TabName);
GO

CREATE INDEX IX_isBOMImportBills_ComponentItemCode 
    ON dbo.isBOMImportBills(ComponentItemCode);
GO

CREATE INDEX IX_isBOMImportBills_ParentItemCode 
    ON dbo.isBOMImportBills(ParentItemCode);
GO

CREATE INDEX IX_isBOMImportBills_BOMNumber 
    ON dbo.isBOMImportBills(BOMNumber);
GO

-- Composite index for common queries
CREATE INDEX IX_isBOMImportBills_Import_Status 
    ON dbo.isBOMImportBills(ImportFileName, Status, ImportDate DESC);
GO

-- Add table description
EXEC sys.sp_addextendedproperty 
    @name = N'MS_Description', 
    @value = N'Stores imported BOM (Bill of Materials) data from Excel files with import metadata and status tracking',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE', @level1name = N'isBOMImportBills';
GO

-- Add column descriptions
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Import file name', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'isBOMImportBills', @level2type = N'COLUMN', @level2name = N'ImportFileName';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Date and time of import', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'isBOMImportBills', @level2type = N'COLUMN', @level2name = N'ImportDate';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Windows user who performed the import', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'isBOMImportBills', @level2type = N'COLUMN', @level2name = N'ImportWindowsUser';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Excel worksheet/tab name', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'isBOMImportBills', @level2type = N'COLUMN', @level2name = N'TabName';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Current status: New, Validated, Integrated, NewBuyItem, NewMakeItem, Failed, Duplicate', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'isBOMImportBills', @level2type = N'COLUMN', @level2name = N'Status';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Component item code from BOM', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'isBOMImportBills', @level2type = N'COLUMN', @level2name = N'ComponentItemCode';
GO

-- Verification query
SELECT 
    TABLE_NAME,
    (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'isBOMImportBills') AS ColumnCount,
    (SELECT COUNT(*) FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.isBOMImportBills')) AS IndexCount
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_NAME = 'isBOMImportBills';
GO

PRINT 'Table isBOMImportBills created successfully in MAS_AML database!';
PRINT 'Total Columns: 27';
PRINT 'Total Indexes: 8';
GO
