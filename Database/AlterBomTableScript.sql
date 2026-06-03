-- Remove Constraints
ALTER TABLE dbo.isBOMImportBills
DROP CONSTRAINT CK_isBOMImportBills_ItemType;

ALTER TABLE dbo.isBOMImportBills
DROP CONSTRAINT CK_isBOMImportBills_Status;

-- Remove Unused Columns
ALTER TABLE dbo.isBOMImportBills
DROP COLUMN
    Reference,
    Notes,
    Category,
    Type,
    UnitCost,
    ExtendedCost,
    ItemType;

-- Add New Columns
ALTER TABLE dbo.isBOMImportBills
ADD
    ProductLine NVARCHAR(50) NULL,
    ProductType NVARCHAR(50) NULL,
    ProcurementType NVARCHAR(50) NULL,
    SubProductFamily NVARCHAR(100) NULL,
    StagedItem BIT NULL,
    Coated BIT NULL,
    GoldenStandard BIT NULL;
GO