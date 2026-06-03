USE [MAS_AML]
GO

/****** Object:  Table [dbo].[isBOMImportBills]    Script Date: 5/31/2026 11:39:44 AM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[isBOMImportBills](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[ImportFileName] [nvarchar](255) NOT NULL,
	[ImportDate] [datetime2](7) NOT NULL,
	[ImportWindowsUser] [nvarchar](100) NOT NULL,
	[TabName] [nvarchar](100) NOT NULL,
	[Status] [nvarchar](50) NOT NULL,
	[DateValidated] [datetime2](7) NULL,
	[DateIntegrated] [datetime2](7) NULL,
	[ParentItemCode] [nvarchar](50) NULL,
	[ParentDescription] [nvarchar](255) NULL,
	[BOMLevel] [nvarchar](20) NULL,
	[BOMNumber] [nvarchar](50) NULL,
	[LineNumber] [int] NOT NULL,
	[ComponentItemCode] [nvarchar](50) NOT NULL,
	[ComponentDescription] [nvarchar](255) NULL,
	[Quantity] [decimal](18, 4) NOT NULL,
	[UnitOfMeasure] [nvarchar](20) NULL,
	[ItemExists] [bit] NOT NULL,
	[ValidationMessage] [nvarchar](500) NULL,
	[CreatedDate] [datetime2](7) NOT NULL,
	[ModifiedDate] [datetime2](7) NOT NULL,
	[ProductLine] [nvarchar](50) NULL,
	[ProductType] [nvarchar](50) NULL,
	[ProcurementType] [nvarchar](50) NULL,
	[SubProductFamily] [nvarchar](100) NULL,
	[StagedItem] [bit] NULL,
	[Coated] [bit] NULL,
	[GoldenStandard] [bit] NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[isBOMImportBills] ADD  DEFAULT (getdate()) FOR [ImportDate]
GO

ALTER TABLE [dbo].[isBOMImportBills] ADD  DEFAULT ('New') FOR [Status]
GO

ALTER TABLE [dbo].[isBOMImportBills] ADD  DEFAULT ((0)) FOR [ItemExists]
GO

ALTER TABLE [dbo].[isBOMImportBills] ADD  DEFAULT (getdate()) FOR [CreatedDate]
GO

ALTER TABLE [dbo].[isBOMImportBills] ADD  DEFAULT (getdate()) FOR [ModifiedDate]
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Import file name' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'isBOMImportBills', @level2type=N'COLUMN',@level2name=N'ImportFileName'
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Date and time of import' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'isBOMImportBills', @level2type=N'COLUMN',@level2name=N'ImportDate'
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Windows user who performed the import' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'isBOMImportBills', @level2type=N'COLUMN',@level2name=N'ImportWindowsUser'
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Excel worksheet/tab name' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'isBOMImportBills', @level2type=N'COLUMN',@level2name=N'TabName'
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Current status: New, Validated, Integrated, NewBuyItem, NewMakeItem, Failed, Duplicate' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'isBOMImportBills', @level2type=N'COLUMN',@level2name=N'Status'
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Component item code from BOM' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'isBOMImportBills', @level2type=N'COLUMN',@level2name=N'ComponentItemCode'
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Stores imported BOM (Bill of Materials) data from Excel files with import metadata and status tracking' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'isBOMImportBills'
GO


