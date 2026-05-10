using Aml.BOM.Import.Application.Models;
using Aml.BOM.Import.Shared.Interfaces;
using AppValidationResult = Aml.BOM.Import.Application.Models.ValidationResult;
using ServiceValidationResult = Aml.BOM.Import.Shared.Interfaces.ValidationResult;

namespace Aml.BOM.Import.Application.Services;

public class BomImportService
{
    private readonly IBomImportRepository _bomImportRepository;
    private readonly IFileImportService _fileImportService;
    private readonly IBomValidationService _bomValidationService;

    public BomImportService(
        IBomImportRepository bomImportRepository,
        IFileImportService fileImportService,
        IBomValidationService bomValidationService)
    {
        _bomImportRepository = bomImportRepository;
        _fileImportService = fileImportService;
        _bomValidationService = bomValidationService;
    }

    public async Task<ImportFileResponse> ImportBomFileAsync(ImportFileRequest request)
    {
        try
        {
            // Validate request
            if (request == null || string.IsNullOrWhiteSpace(request.FilePath))
            {
                return new ImportFileResponse 
                { 
                    Success = false, 
                    Message = "Invalid request: File path is required" 
                };
            }

            // Validate file format
            var isValid = await _fileImportService.ValidateFileFormatAsync(request.FilePath);
            if (!isValid)
            {
                return new ImportFileResponse 
                { 
                    Success = false, 
                    Message = "Invalid file format. Supported formats: .xlsx, .xls" 
                };
            }

            // Check if file was already imported
            var fileName = System.IO.Path.GetFileName(request.FilePath);
            var isAlreadyImported = await _bomValidationService.IsFileAlreadyImportedAsync(fileName);
            if (isAlreadyImported)
            {
                return new ImportFileResponse 
                { 
                    Success = false, 
                    Message = $"File '{fileName}' has already been imported. Duplicate imports are not allowed." 
                };
            }

            // Import the file (this includes automatic validation)
            var importResult = await _fileImportService.ImportFileAsync(request.FilePath);
            
            // Extract results from dynamic object
            dynamic result = importResult;
            
            return new ImportFileResponse
            {
                Success = true,
                Message = result.Message?.ToString() ?? "File imported successfully",
                FileId = result.FileId,
                FileName = result.FileName?.ToString() ?? fileName,
                ImportedRecords = result.ImportedRecords,
                ValidatedRecords = result.ValidatedRecords,
                NewBuyItems = result.NewBuyItems,
                NewMakeItems = result.NewMakeItems,
                DuplicateBoms = result.DuplicateBoms,
                FailedRecords = result.FailedRecords,
                TabsProcessed = result.Tabs,
                Warnings = result.Warnings != null ? ((IEnumerable<string>)result.Warnings).ToList() : new List<string>(),
                Errors = result.Errors != null ? ((IEnumerable<string>)result.Errors).ToList() : new List<string>()
            };
        }
        catch (System.IO.FileNotFoundException ex)
        {
            return new ImportFileResponse 
            { 
                Success = false, 
                Message = $"File not found: {ex.Message}" 
            };
        }
        catch (InvalidOperationException ex)
        {
            return new ImportFileResponse 
            { 
                Success = false, 
                Message = ex.Message 
            };
        }
        catch (Exception ex)
        {
            return new ImportFileResponse 
            { 
                Success = false, 
                Message = $"Import failed: {ex.Message}",
                Errors = new List<string> { ex.ToString() }
            };
        }
    }

    public async Task<IEnumerable<object>> GetAllBomsAsync()
    {
        return await _bomImportRepository.GetAllAsync();
    }

    public async Task<AppValidationResult> ValidateBomAsync(int bomImportRecordId)
    {
        // TODO: Implement BOM validation logic
        await Task.CompletedTask;
        return new AppValidationResult { IsValid = false };
    }
}
