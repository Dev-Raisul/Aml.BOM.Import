-- =============================================
-- ALTER TABLE: isBOMImportBills
-- Description: Add 'Ready' status to the Status constraint
-- Database: MAS_AML
-- =============================================

USE MAS_AML;
GO

PRINT 'Updating Status constraint to include Ready status...';
GO

-- Drop existing Status constraint
IF EXISTS (SELECT * FROM sys.check_constraints 
           WHERE name = 'CK_isBOMImportBills_Status' 
           AND parent_object_id = OBJECT_ID('dbo.isBOMImportBills'))
BEGIN
    ALTER TABLE dbo.isBOMImportBills 
    DROP CONSTRAINT CK_isBOMImportBills_Status;
    PRINT 'Dropped old Status constraint';
END
GO

-- Add updated Status constraint with 'Ready' status
ALTER TABLE dbo.isBOMImportBills 
ADD CONSTRAINT CK_isBOMImportBills_Status 
CHECK (Status IN ('New', 'Validated', 'Ready', 'Integrated', 'NewBuyItem', 'NewMakeItem', 'Failed', 'Duplicate'));
PRINT 'Added updated Status constraint (now includes Ready)';
GO

-- Verification query
SELECT CONSTRAINT_NAME, CHECK_CLAUSE
FROM INFORMATION_SCHEMA.CHECK_CONSTRAINTS
WHERE CONSTRAINT_NAME = 'CK_isBOMImportBills_Status';
GO

PRINT 'ALTER TABLE completed successfully!';
PRINT 'Status constraint updated to include: New, Validated, Ready, Integrated, NewBuyItem, NewMakeItem, Failed, Duplicate';
GO
