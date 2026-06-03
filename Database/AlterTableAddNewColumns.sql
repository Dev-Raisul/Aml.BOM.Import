-- =============================================
-- ALTER TABLE: isBOMImportBills
-- Description: Add new columns for Excel import fields
-- Database: MAS_AML
-- =============================================

USE MAS_AML;
GO

PRINT 'Starting ALTER TABLE for isBOMImportBills...';
GO

-- Check if columns already exist before adding them
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'isBOMImportBills' AND COLUMN_NAME = 'ProductLine')
BEGIN
    ALTER TABLE dbo.isBOMImportBills 
    ADD ProductLine NVARCHAR(50) NULL;
    PRINT 'Added column: ProductLine';
END
ELSE
BEGIN
    PRINT 'Column ProductLine already exists - skipping';
END
GO

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'isBOMImportBills' AND COLUMN_NAME = 'ProductType')
BEGIN
    ALTER TABLE dbo.isBOMImportBills 
    ADD ProductType NVARCHAR(50) NULL;
    PRINT 'Added column: ProductType';
END
ELSE
BEGIN
    PRINT 'Column ProductType already exists - skipping';
END
GO

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'isBOMImportBills' AND COLUMN_NAME = 'ProcurementType')
BEGIN
    ALTER TABLE dbo.isBOMImportBills 
    ADD ProcurementType NVARCHAR(50) NULL;
    PRINT 'Added column: ProcurementType';
END
ELSE
BEGIN
    PRINT 'Column ProcurementType already exists - skipping';
END
GO

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'isBOMImportBills' AND COLUMN_NAME = 'SubProductFamily')
BEGIN
    ALTER TABLE dbo.isBOMImportBills 
    ADD SubProductFamily NVARCHAR(100) NULL;
    PRINT 'Added column: SubProductFamily';
END
ELSE
BEGIN
    PRINT 'Column SubProductFamily already exists - skipping';
END
GO

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'isBOMImportBills' AND COLUMN_NAME = 'StagedItem')
BEGIN
    ALTER TABLE dbo.isBOMImportBills 
    ADD StagedItem BIT NULL;
    PRINT 'Added column: StagedItem';
END
ELSE
BEGIN
    PRINT 'Column StagedItem already exists - skipping';
END
GO

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'isBOMImportBills' AND COLUMN_NAME = 'Coated')
BEGIN
    ALTER TABLE dbo.isBOMImportBills 
    ADD Coated BIT NULL;
    PRINT 'Added column: Coated';
END
ELSE
BEGIN
    PRINT 'Column Coated already exists - skipping';
END
GO

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'isBOMImportBills' AND COLUMN_NAME = 'GoldenStandard')
BEGIN
    ALTER TABLE dbo.isBOMImportBills 
    ADD GoldenStandard BIT NULL;
    PRINT 'Added column: GoldenStandard';
END
ELSE
BEGIN
    PRINT 'Column GoldenStandard already exists - skipping';
END
GO

-- Update ItemType constraint to include 'Phantom'
IF EXISTS (SELECT * FROM sys.check_constraints 
           WHERE name = 'CK_isBOMImportBills_ItemType' 
           AND parent_object_id = OBJECT_ID('dbo.isBOMImportBills'))
BEGIN
    ALTER TABLE dbo.isBOMImportBills 
    DROP CONSTRAINT CK_isBOMImportBills_ItemType;
    PRINT 'Dropped old ItemType constraint';
    
    ALTER TABLE dbo.isBOMImportBills 
    ADD CONSTRAINT CK_isBOMImportBills_ItemType 
    CHECK (ItemType IS NULL OR ItemType IN ('Buy', 'Make', 'Phantom'));
    PRINT 'Added updated ItemType constraint (now includes Phantom)';
END
ELSE
BEGIN
    -- Constraint doesn't exist, add it
    ALTER TABLE dbo.isBOMImportBills 
    ADD CONSTRAINT CK_isBOMImportBills_ItemType 
    CHECK (ItemType IS NULL OR ItemType IN ('Buy', 'Make', 'Phantom'));
    PRINT 'Added ItemType constraint';
END
GO

-- Verification query
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'isBOMImportBills'
  AND COLUMN_NAME IN ('ProductLine', 'ProductType', 'ProcurementType', 'SubProductFamily', 
                      'StagedItem', 'Coated', 'GoldenStandard')
ORDER BY ORDINAL_POSITION;
GO

-- Display total column count
SELECT 
    TABLE_NAME,
    COUNT(*) AS TotalColumns
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'isBOMImportBills'
GROUP BY TABLE_NAME;
GO

PRINT 'ALTER TABLE completed successfully!';
PRINT 'New columns added: ProductLine, ProductType, ProcurementType, SubProductFamily, StagedItem, Coated, GoldenStandard';
PRINT 'ItemType constraint updated to include: Buy, Make, Phantom';
GO
