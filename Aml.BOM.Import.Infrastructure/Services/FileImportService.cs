using Aml.BOM.Import.Domain.Entities;
using Aml.BOM.Import.Shared.Interfaces;
using ClosedXML.Excel;
using System.IO;

namespace Aml.BOM.Import.Infrastructure.Services;

public class FileImportService : IFileImportService
{
    private readonly IImportBomFileLogRepository _fileLogRepository;
    private readonly IBomImportBillRepository _bomBillRepository;
    private readonly IBomValidationService _validationService;
    private readonly ILoggerService _logger;

    public FileImportService(
        IImportBomFileLogRepository fileLogRepository,
        IBomImportBillRepository bomBillRepository,
        IBomValidationService validationService,
        ILoggerService logger)
    {
        _fileLogRepository = fileLogRepository;
        _bomBillRepository = bomBillRepository;
        _validationService = validationService;
        _logger = logger;
    }

    public async Task<object> ImportFileAsync(string filePath)
    {
        _logger.LogInformation("Starting BOM file import for: {0}", filePath);

        // Validate file exists
        if (!File.Exists(filePath))
        {
            _logger.LogError("File not found: {0}", null, filePath);
            throw new FileNotFoundException("The specified file does not exist.", filePath);
        }

        var fileName = Path.GetFileName(filePath);
        var currentUser = Environment.UserName;
        var importDate = DateTime.Now;

        // Check if file was already imported
        var isAlreadyImported = await _validationService.IsFileAlreadyImportedAsync(fileName);
        if (isAlreadyImported)
        {
            _logger.LogWarning("File already imported: {0}", fileName);
            throw new InvalidOperationException($"The file '{fileName}' has already been imported. Duplicate imports are not allowed.");
        }
        
        // Create file log entry
        var fileLog = new ImportBomFileLog
        {
            FileName = fileName,
            UploadDate = importDate
        };

        try
        {
            // Log the file upload
            var fileId = await _fileLogRepository.CreateAsync(fileLog);
            _logger.LogInformation("BOM file logged successfully. FileId: {0}, FileName: {1}", fileId, fileLog.FileName);

            // Parse Excel file and import BOM data
            var importResults = await ParseAndImportExcelFileAsync(filePath, fileName, currentUser, importDate);

            // Automatically validate the newly imported file
            _logger.LogInformation("Starting automatic validation for newly imported file: {0}", fileName);
            var validationResult = await _validationService.ValidateImportFileAsync(fileName);

            // Also validate all other pending BOMs
            _logger.LogInformation("Validating all other pending BOMs");
            await _validationService.ValidateAllPendingAsync();

            // Return the results
            var result = new
            {
                FileId = fileId,
                FileName = fileLog.FileName,
                ImportedRecords = importResults.TotalRecords,
                Tabs = importResults.TabsProcessed,
                ValidatedRecords = validationResult.ValidatedRecords,
                NewBuyItems = validationResult.NewBuyItems,
                NewMakeItems = validationResult.NewMakeItems,
                DuplicateBoms = validationResult.DuplicateBoms,
                FailedRecords = validationResult.FailedRecords,
                Message = $"File uploaded and {importResults.TotalRecords} records imported. Validation: {validationResult.ValidatedRecords} validated, {validationResult.DuplicateBoms} duplicates found.",
                Warnings = validationResult.Warnings,
                Errors = validationResult.Errors
            };

            _logger.LogInformation("BOM file import and validation completed successfully. FileId: {0}, Records: {1}, Validated: {2}", 
                fileId, importResults.TotalRecords, validationResult.ValidatedRecords);
            return result;
        }
        catch (InvalidOperationException)
        {
            // Re-throw duplicate file exception
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to import BOM file: {0}", ex, filePath);
            throw;
        }
    }

    private async Task<ImportResults> ParseAndImportExcelFileAsync(string filePath, string fileName, 
        string currentUser, DateTime importDate)
    {
        _logger.LogInformation("Parsing Excel file: {0}", filePath);

        var bills = new List<BomImportBill>();
        var tabsProcessed = new List<string>();

        try
        {
            using var workbook = new XLWorkbook(filePath);

            // Process each worksheet/tab
            foreach (var worksheet in workbook.Worksheets)
            {
                var tabName = worksheet.Name;
                _logger.LogInformation("Processing tab: {0}", tabName);

                var tabBills = ParseWorksheet(worksheet, fileName, tabName, currentUser, importDate);
                bills.AddRange(tabBills);
                tabsProcessed.Add(tabName);

                _logger.LogInformation("Processed {0} records from tab: {1}", tabBills.Count, tabName);
            }

            // Save all bills to database in batch
            if (bills.Any())
            {
                var savedCount = await _bomBillRepository.CreateBatchAsync(bills);
                _logger.LogInformation("Saved {0} BOM import bills to database", savedCount);
            }

            return new ImportResults
            {
                TotalRecords = bills.Count,
                TabsProcessed = tabsProcessed.Count,
                TabNames = tabsProcessed
            };
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to parse Excel file: {0}", ex, filePath);
            throw;
        }
    }

    private List<BomImportBill> ParseWorksheet(IXLWorksheet worksheet, string fileName, string tabName, 
        string currentUser, DateTime importDate)
    {
        var bills = new List<BomImportBill>();

        // Find the header row (assuming row 1 contains headers)
        var headerRow = worksheet.Row(1);
        
        // Map column names to indices
        var columnMap = BuildColumnMap(headerRow);

        // Start from row 2 (assuming row 1 is header)
        var currentRow = 2;
        var lineNumber = 1;

        while (!worksheet.Row(currentRow).IsEmpty())
        {
            try
            {
                var row = worksheet.Row(currentRow);

                var bill = new BomImportBill
                {
                    // Import metadata
                    ImportFileName = fileName,
                    ImportDate = importDate,
                    ImportWindowsUser = currentUser,
                    TabName = tabName,
                    Status = "New",

                    // BOM data from Excel columns
                    LineNumber = lineNumber,
                    ParentItemCode = GetCellValue(row, columnMap, "Parent Item", "Parent Item Code", "Parent Part"),
                    ParentDescription = GetCellValue(row, columnMap, "Parent Description", "Parent Desc"),
                    BOMLevel = GetCellValue(row, columnMap, "Level", "BOM Level"),
                    BOMNumber = GetCellValue(row, columnMap, "BOM Number", "BOM#", "BOM No"),
                    ComponentItemCode = GetCellValue(row, columnMap, "Component", "Component Item", "Item Code", "Part Number") ?? string.Empty,
                    ComponentDescription = GetCellValue(row, columnMap, "Description", "Component Description", "Item Description"),
                    Quantity = ParseDecimal(GetCellValue(row, columnMap, "Quantity", "Qty"), 0),
                    UnitOfMeasure = GetCellValue(row, columnMap, "UOM", "Unit", "Unit of Measure"),
                    Reference = GetCellValue(row, columnMap, "Reference", "Ref", "Designator"),
                    Notes = GetCellValue(row, columnMap, "Notes", "Comments", "Remarks"),
                    Category = GetCellValue(row, columnMap, "Category", "Type"),
                    Type = GetCellValue(row, columnMap, "Item Type", "Type"),
                    UnitCost = ParseDecimal(GetCellValue(row, columnMap, "Unit Cost", "Cost", "Price"), null),
                    ExtendedCost = ParseDecimal(GetCellValue(row, columnMap, "Extended Cost", "Total Cost", "Total"), null),

                    // Audit fields
                    CreatedDate = DateTime.Now,
                    ModifiedDate = DateTime.Now
                };

                // Only add if component item code is not empty
                if (!string.IsNullOrWhiteSpace(bill.ComponentItemCode))
                {
                    bills.Add(bill);
                    lineNumber++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed to parse row {0} in tab {1}: {2}", currentRow, tabName, ex.Message);
            }

            currentRow++;
        }

        return bills;
    }

    private Dictionary<string, int> BuildColumnMap(IXLRow headerRow)
    {
        var columnMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        for (int col = 1; col <= headerRow.CellCount(); col++)
        {
            var cellValue = headerRow.Cell(col).GetString().Trim();
            if (!string.IsNullOrWhiteSpace(cellValue))
            {
                columnMap[cellValue] = col;
            }
        }

        return columnMap;
    }

    private string? GetCellValue(IXLRow row, Dictionary<string, int> columnMap, params string[] possibleNames)
    {
        foreach (var name in possibleNames)
        {
            if (columnMap.TryGetValue(name, out int colIndex))
            {
                var value = row.Cell(colIndex).GetString().Trim();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }
        }
        return null;
    }

    private decimal ParseDecimal(string? value, decimal? defaultValue)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue ?? 0;
        }

        if (decimal.TryParse(value, out decimal result))
        {
            return result;
        }

        return defaultValue ?? 0;
    }

    public async Task<bool> ValidateFileFormatAsync(string filePath)
    {
        _logger.LogDebug("Validating file format for: {0}", filePath);

        try
        {
            // Check if file exists
            if (!File.Exists(filePath))
            {
                _logger.LogWarning("File not found during validation: {0}", filePath);
                return false;
            }

            // Check file extension
            var extension = Path.GetExtension(filePath).ToLower();
            var supportedExtensions = GetSupportedFileExtensions();
            
            if (!supportedExtensions.Contains(extension))
            {
                _logger.LogWarning("Unsupported file extension: {0}. File: {1}", extension, filePath);
                return false;
            }

            // Check file size (max 50MB)
            var fileInfo = new FileInfo(filePath);
            const long maxFileSize = 50 * 1024 * 1024; // 50MB
            
            if (fileInfo.Length > maxFileSize)
            {
                _logger.LogWarning("File size exceeds maximum allowed. Size: {0} bytes, File: {1}", fileInfo.Length, filePath);
                return false;
            }

            // Try to open Excel file to verify it's valid
            if (extension == ".xlsx" || extension == ".xls")
            {
                try
                {
                    using var workbook = new XLWorkbook(filePath);
                    if (workbook.Worksheets.Count == 0)
                    {
                        _logger.LogWarning("Excel file has no worksheets: {0}", filePath);
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Failed to open Excel file: {0}. Error: {1}", filePath, ex.Message);
                    return false;
                }
            }

            _logger.LogInformation("File format validation passed for: {0}", filePath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError("Error during file format validation: {0}", ex, filePath);
            return false;
        }
    }

    public IEnumerable<string> GetSupportedFileExtensions()
    {
        return new[] { ".xlsx", ".xls" };
    }

    private class ImportResults
    {
        public int TotalRecords { get; set; }
        public int TabsProcessed { get; set; }
        public List<string> TabNames { get; set; } = new();
    }
}
