-- =============================================
-- Create Table: isBOMImport_NewMakeItems
-- Description: Stores unique new make items identified during BOM import.
--              Each item code appears only once (first occurrence wins).
--              Holds info from the BOM Import File plus user-edited fields
--              and integration tracking.
-- Database: MAS_AML
-- =============================================

USE MAS_AML;
GO

-- Drop table if exists (for development / re-run safety)
IF OBJECT_ID('dbo.isBOMImport_NewMakeItems', 'U') IS NOT NULL
    DROP TABLE dbo.isBOMImport_NewMakeItems;
GO

CREATE TABLE dbo.isBOMImport_NewMakeItems
(
    -- Primary key
    Id                    INT           NOT NULL IDENTITY(1,1) CONSTRAINT PK_isBOMImport_NewMakeItems PRIMARY KEY,

    -- From first occurrence of the BOM Import File
    ItemCode              NVARCHAR(50)  NOT NULL,
    ImportFileName        NVARCHAR(255) NOT NULL,
    ImportDate            DATETIME2     NOT NULL,

    -- Editable fields (initially from first occurrence; updated when user edits)
    ItemDescription       NVARCHAR(255) NULL,
    ProductLine           NVARCHAR(100) NULL,
    ProductType           NVARCHAR(50)  NOT NULL CONSTRAINT DF_NewMakeItems_ProductType           DEFAULT 'F',
    Procurement           NVARCHAR(50)  NOT NULL CONSTRAINT DF_NewMakeItems_Procurement           DEFAULT 'M',
    StandardUnitOfMeasure NVARCHAR(20)  NOT NULL CONSTRAINT DF_NewMakeItems_StandardUnitOfMeasure DEFAULT 'EACH',
    SubProductFamily      NVARCHAR(100) NULL,
    StagedItem            BIT           NOT NULL CONSTRAINT DF_NewMakeItems_StagedItem            DEFAULT 0,
    Coated                BIT           NOT NULL CONSTRAINT DF_NewMakeItems_Coated                DEFAULT 0,
    GoldenStandard        BIT           NOT NULL CONSTRAINT DF_NewMakeItems_GoldenStandard        DEFAULT 0,

    -- Integration tracking
    IsIntegrated          BIT           NOT NULL CONSTRAINT DF_NewMakeItems_IsIntegrated          DEFAULT 0,
    DateIntegrated        DATETIME2     NULL,
    IntegratedBy          NVARCHAR(100) NULL,

    -- Audit fields
    CreatedDate           DATETIME2     NOT NULL CONSTRAINT DF_NewMakeItems_CreatedDate           DEFAULT GETDATE(),
    CreatedWindowsUser    NVARCHAR(100) NOT NULL,
    ModifiedDate          DATETIME2     NOT NULL CONSTRAINT DF_NewMakeItems_ModifiedDate          DEFAULT GETDATE(),
    ModifiedWindowsUser   NVARCHAR(100) NOT NULL,

    -- Enforce one row per item code
    CONSTRAINT UQ_isBOMImport_NewMakeItems_ItemCode UNIQUE (ItemCode)
);
GO

-- Indexes for common query patterns
CREATE INDEX IX_isBOMImport_NewMakeItems_ImportFileName
    ON dbo.isBOMImport_NewMakeItems (ImportFileName);
GO

CREATE INDEX IX_isBOMImport_NewMakeItems_IsIntegrated
    ON dbo.isBOMImport_NewMakeItems (IsIntegrated);
GO

-- =============================================
-- Stored procedure: copy new make items from isBOMImportBills after a file import.
-- Inserts only the first occurrence of each item code (ignores duplicates).
-- =============================================
IF OBJECT_ID('dbo.usp_CopyNewMakeItemsFromBills', 'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_CopyNewMakeItemsFromBills;
GO

CREATE PROCEDURE dbo.usp_CopyNewMakeItemsFromBills
    @ImportFileName   NVARCHAR(255),
    @WindowsUser      NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    -- Insert distinct item codes from the import file that are not already
    -- present in isBOMImport_NewMakeItems.  Only the very first occurrence
    -- (lowest Id) within this file is used for the description.
    INSERT INTO dbo.isBOMImport_NewMakeItems
    (
        ItemCode,
        ImportFileName,
        ImportDate,
        ItemDescription,
        ProductLine,
        ProductType,
        Procurement,
        StandardUnitOfMeasure,
        SubProductFamily,
        StagedItem,
        Coated,
        GoldenStandard,
        IsIntegrated,
        CreatedDate,
        CreatedWindowsUser,
        ModifiedDate,
        ModifiedWindowsUser
    )
    SELECT
        src.ComponentItemCode  AS ItemCode,
        src.ImportFileName,
        src.ImportDate,
        src.ComponentDescription AS ItemDescription,
        NULL                   AS ProductLine,
        'F'                    AS ProductType,
        'M'                    AS Procurement,
        'EACH'                 AS StandardUnitOfMeasure,
        NULL                   AS SubProductFamily,
        0                      AS StagedItem,
        0                      AS Coated,
        0                      AS GoldenStandard,
        0                      AS IsIntegrated,
        GETDATE()              AS CreatedDate,
        @WindowsUser           AS CreatedWindowsUser,
        GETDATE()              AS ModifiedDate,
        @WindowsUser           AS ModifiedWindowsUser
    FROM
    (
        -- Pick the single row with the lowest Id for each unique ComponentItemCode
        -- within this import file.
        SELECT
            b.ComponentItemCode,
            b.ImportFileName,
            b.ImportDate,
            b.ComponentDescription,
            ROW_NUMBER() OVER (PARTITION BY b.ComponentItemCode ORDER BY b.Id ASC) AS rn
        FROM dbo.isBOMImportBills b
        WHERE b.ImportFileName = @ImportFileName
          AND b.Status         = 'NewMakeItem'
    ) src
    WHERE src.rn = 1
      -- Skip any item code already captured from a previous import
      AND NOT EXISTS
      (
          SELECT 1
          FROM dbo.isBOMImport_NewMakeItems existing
          WHERE existing.ItemCode = src.ComponentItemCode
      );

    SELECT @@ROWCOUNT AS InsertedCount;
END;
GO

-- =============================================
-- Verification
-- =============================================
SELECT
    TABLE_NAME,
    (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
     WHERE TABLE_NAME = 'isBOMImport_NewMakeItems') AS ColumnCount
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_NAME = 'isBOMImport_NewMakeItems'
  AND TABLE_SCHEMA = 'dbo';
GO

PRINT 'Table isBOMImport_NewMakeItems created successfully in MAS_AML database!';
PRINT 'Stored procedure usp_CopyNewMakeItemsFromBills created successfully!';
GO
