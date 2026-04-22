-- =============================================
-- Create Database for Aml.BOM.Import Application
-- Database Name: MAS_AML
-- =============================================
-- Run this script first to create the database
-- Then run CreateSchema.sql and CreateImportBOMFileLogTable.sql

-- Check if database exists, drop if it does (USE WITH CAUTION IN PRODUCTION!)
IF DB_ID('MAS_AML') IS NOT NULL
BEGIN
    PRINT 'Database MAS_AML already exists. Dropping...';
    ALTER DATABASE MAS_AML SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE MAS_AML;
END
GO

-- Create the database
CREATE DATABASE MAS_AML
GO

PRINT 'Database MAS_AML created successfully!';
PRINT 'Next steps:';
PRINT '1. Run CreateSchema.sql to create the main tables';
PRINT '2. Run CreateImportBOMFileLogTable.sql to create the import log table';
GO

-- Use the database
USE MAS_AML;
GO

-- Verify database was created
SELECT 
    name AS DatabaseName,
    database_id AS DatabaseID,
    create_date AS CreatedDate,
    state_desc AS State,
    recovery_model_desc AS RecoveryModel
FROM sys.databases
WHERE name = 'MAS_AML';
GO
