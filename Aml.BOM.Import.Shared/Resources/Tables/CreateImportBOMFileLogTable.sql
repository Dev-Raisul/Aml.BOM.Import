-- =============================================
-- Create Table: isBOMImportFileLog
-- Description: Logs all BOM file uploads
-- Database: MAS_AML
-- =============================================

USE MAS_AML;
GO

-- Drop table if exists (for development/testing)
IF OBJECT_ID('dbo.isBOMImportFileLog', 'U') IS NOT NULL 
    DROP TABLE dbo.isBOMImportFileLog;
GO

-- Create the BOM file import log table
CREATE TABLE dbo.isBOMImportFileLog
(
    FileId INT PRIMARY KEY IDENTITY(1,1),
    FileName NVARCHAR(255) NOT NULL,
    UploadDate DATETIME2 NOT NULL DEFAULT GETDATE()
);
GO

-- Create index for better query performance
CREATE INDEX IX_isBOMImportFileLog_UploadDate 
    ON dbo.isBOMImportFileLog(UploadDate DESC);
GO

-- Add description
EXEC sys.sp_addextendedproperty 
    @name = N'MS_Description', 
    @value = N'Logs all BOM file uploads',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE', @level1name = N'isBOMImportFileLog';
GO

-- Verification query
SELECT 
    TABLE_NAME,
    (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'isBOMImportFileLog') AS ColumnCount
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_NAME = 'isBOMImportFileLog';
GO

PRINT 'Table isBOMImportFileLog created successfully in MAS_AML database!';
GO
