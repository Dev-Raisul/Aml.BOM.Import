-- =============================================
-- Aml.BOM.Import Database Schema
-- Database: MAS_AML
-- =============================================
-- This script creates the database schema for the BOM Import application
-- Run this script after creating the MAS_AML database

USE MAS_AML;
GO

-- =============================================
-- Drop existing tables (in reverse order of dependencies)
-- =============================================
IF OBJECT_ID('dbo.BomImportLines', 'U') IS NOT NULL DROP TABLE dbo.BomImportLines;
IF OBJECT_ID('dbo.BomImportRecords', 'U') IS NOT NULL DROP TABLE dbo.BomImportRecords;
IF OBJECT_ID('dbo.NewMakeItems', 'U') IS NOT NULL DROP TABLE dbo.NewMakeItems;
IF OBJECT_ID('dbo.NewBuyItems', 'U') IS NOT NULL DROP TABLE dbo.NewBuyItems;
IF OBJECT_ID('dbo.SageItems', 'U') IS NOT NULL DROP TABLE dbo.SageItems;
GO

-- =============================================
-- Table: SageItems
-- Description: Cache of items from Sage system
-- =============================================
CREATE TABLE dbo.SageItems
(
    Id INT PRIMARY KEY IDENTITY(1,1),
    ItemCode NVARCHAR(50) NOT NULL UNIQUE,
    Description NVARCHAR(255) NULL,
    ItemType INT NOT NULL, -- 0=Buy, 1=Make
    UnitOfMeasure NVARCHAR(20) NULL,
    StockQuantity DECIMAL(18,4) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETDATE(),
    ModifiedDate DATETIME2 NOT NULL DEFAULT GETDATE()
);
GO

CREATE INDEX IX_SageItems_ItemCode ON dbo.SageItems(ItemCode);
CREATE INDEX IX_SageItems_ItemType ON dbo.SageItems(ItemType);
GO

-- =============================================
-- Table: NewBuyItems
-- Description: New buy items identified during import
-- =============================================
CREATE TABLE dbo.NewBuyItems
(
    Id INT PRIMARY KEY IDENTITY(1,1),
    ItemCode NVARCHAR(50) NOT NULL,
    Description NVARCHAR(255) NULL,
    UnitOfMeasure NVARCHAR(20) NULL,
    Status INT NOT NULL DEFAULT 0, -- 0=Pending, 1=Integrated
    IdentifiedDate DATETIME2 NOT NULL DEFAULT GETDATE(),
    IntegratedDate DATETIME2 NULL,
    IntegratedBy NVARCHAR(100) NULL,
    Notes NVARCHAR(MAX) NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETDATE(),
    ModifiedDate DATETIME2 NOT NULL DEFAULT GETDATE()
);
GO

CREATE INDEX IX_NewBuyItems_ItemCode ON dbo.NewBuyItems(ItemCode);
CREATE INDEX IX_NewBuyItems_Status ON dbo.NewBuyItems(Status);
GO

-- =============================================
-- Table: NewMakeItems
-- Description: New make items with editable fields
-- =============================================
CREATE TABLE dbo.NewMakeItems
(
    Id INT PRIMARY KEY IDENTITY(1,1),
    ItemCode NVARCHAR(50) NOT NULL,
    Description NVARCHAR(255) NULL,
    UnitOfMeasure NVARCHAR(20) NULL,
    MaterialCost DECIMAL(18,4) NULL,
    LabourCost DECIMAL(18,4) NULL,
    OverheadCost DECIMAL(18,4) NULL,
    LeadTime INT NULL, -- in days
    Category NVARCHAR(50) NULL,
    Notes NVARCHAR(MAX) NULL,
    Status INT NOT NULL DEFAULT 0, -- 0=Pending, 1=Integrated
    IdentifiedDate DATETIME2 NOT NULL DEFAULT GETDATE(),
    IntegratedDate DATETIME2 NULL,
    IntegratedBy NVARCHAR(100) NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETDATE(),
    ModifiedDate DATETIME2 NOT NULL DEFAULT GETDATE()
);
GO

CREATE INDEX IX_NewMakeItems_ItemCode ON dbo.NewMakeItems(ItemCode);
CREATE INDEX IX_NewMakeItems_Status ON dbo.NewMakeItems(Status);
CREATE INDEX IX_NewMakeItems_Category ON dbo.NewMakeItems(Category);
GO

-- =============================================
-- Table: BomImportRecords
-- Description: Header records for BOM imports
-- =============================================
CREATE TABLE dbo.BomImportRecords
(
    Id INT PRIMARY KEY IDENTITY(1,1),
    BomNumber NVARCHAR(50) NOT NULL,
    Description NVARCHAR(255) NULL,
    ParentItemCode NVARCHAR(50) NULL,
    Revision NVARCHAR(20) NULL,
    FileName NVARCHAR(255) NOT NULL,
    ImportDate DATETIME2 NOT NULL DEFAULT GETDATE(),
    ImportedBy NVARCHAR(100) NULL,
    Status INT NOT NULL DEFAULT 0, -- 0=Pending, 1=Validated, 2=Integrated, 3=Failed, 4=Duplicate
    ValidationErrors NVARCHAR(MAX) NULL,
    IntegratedDate DATETIME2 NULL,
    IntegratedBy NVARCHAR(100) NULL,
    IsDuplicate BIT NOT NULL DEFAULT 0,
    OriginalBomId INT NULL,
    Notes NVARCHAR(MAX) NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETDATE(),
    ModifiedDate DATETIME2 NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_BomImportRecords_OriginalBom FOREIGN KEY (OriginalBomId) 
        REFERENCES dbo.BomImportRecords(Id)
);
GO

CREATE INDEX IX_BomImportRecords_BomNumber ON dbo.BomImportRecords(BomNumber);
CREATE INDEX IX_BomImportRecords_Status ON dbo.BomImportRecords(Status);
CREATE INDEX IX_BomImportRecords_ImportDate ON dbo.BomImportRecords(ImportDate);
CREATE INDEX IX_BomImportRecords_IsDuplicate ON dbo.BomImportRecords(IsDuplicate);
GO

-- =============================================
-- Table: BomImportLines
-- Description: Line items for BOM imports
-- =============================================
CREATE TABLE dbo.BomImportLines
(
    Id INT PRIMARY KEY IDENTITY(1,1),
    BomImportRecordId INT NOT NULL,
    LineNumber INT NOT NULL,
    ItemCode NVARCHAR(50) NOT NULL,
    Description NVARCHAR(255) NULL,
    Quantity DECIMAL(18,4) NOT NULL,
    UnitOfMeasure NVARCHAR(20) NULL,
    Reference NVARCHAR(100) NULL,
    Notes NVARCHAR(MAX) NULL,
    ItemExists BIT NOT NULL DEFAULT 0,
    ItemType INT NULL, -- 0=Buy, 1=Make
    ValidationStatus INT NOT NULL DEFAULT 0, -- 0=NotValidated, 1=Valid, 2=Invalid, 3=Warning
    ValidationMessage NVARCHAR(500) NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETDATE(),
    ModifiedDate DATETIME2 NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_BomImportLines_BomImportRecord FOREIGN KEY (BomImportRecordId) 
        REFERENCES dbo.BomImportRecords(Id) ON DELETE CASCADE
);
GO

CREATE INDEX IX_BomImportLines_BomImportRecordId ON dbo.BomImportLines(BomImportRecordId);
CREATE INDEX IX_BomImportLines_ItemCode ON dbo.BomImportLines(ItemCode);
CREATE INDEX IX_BomImportLines_ValidationStatus ON dbo.BomImportLines(ValidationStatus);
GO

-- =============================================
-- Insert sample data (optional - for testing)
-- =============================================

-- Sample Sage Items (existing items in Sage system)
INSERT INTO dbo.SageItems (ItemCode, Description, ItemType, UnitOfMeasure, StockQuantity)
VALUES 
    ('WIDGET-001', 'Standard Widget', 1, 'EA', 100.00),
    ('PART-ABC', 'Component Part ABC', 0, 'EA', 250.00),
    ('ASSY-XYZ', 'Assembly XYZ', 1, 'EA', 50.00);
GO

-- Sample New Buy Items
INSERT INTO dbo.NewBuyItems (ItemCode, Description, UnitOfMeasure, Status)
VALUES 
    ('SCREW-M6-20', 'M6 x 20mm Screw', 'EA', 0),
    ('WASHER-M6', 'M6 Washer', 'EA', 0);
GO

-- Sample New Make Items
INSERT INTO dbo.NewMakeItems (ItemCode, Description, UnitOfMeasure, MaterialCost, LabourCost, Status)
VALUES 
    ('BRACKET-001', 'Mounting Bracket Type 1', 'EA', 5.50, 2.25, 0),
    ('PLATE-STEEL', 'Steel Mounting Plate', 'EA', 8.75, 3.50, 0);
GO

-- =============================================
-- Verification Queries
-- =============================================

-- Verify tables were created
SELECT 
    TABLE_NAME,
    (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = t.TABLE_NAME) AS ColumnCount
FROM INFORMATION_SCHEMA.TABLES t
WHERE TABLE_SCHEMA = 'dbo'
    AND TABLE_TYPE = 'BASE TABLE'
ORDER BY TABLE_NAME;
GO

-- Verify sample data
SELECT 'SageItems' AS TableName, COUNT(*) AS RecordCount FROM dbo.SageItems
UNION ALL
SELECT 'NewBuyItems', COUNT(*) FROM dbo.NewBuyItems
UNION ALL
SELECT 'NewMakeItems', COUNT(*) FROM dbo.NewMakeItems
UNION ALL
SELECT 'BomImportRecords', COUNT(*) FROM dbo.BomImportRecords
UNION ALL
SELECT 'BomImportLines', COUNT(*) FROM dbo.BomImportLines;
GO

PRINT 'Database schema created successfully in MAS_AML!';
PRINT 'Next step: Run CreateImportBOMFileLogTable.sql to create the import log table';
GO
