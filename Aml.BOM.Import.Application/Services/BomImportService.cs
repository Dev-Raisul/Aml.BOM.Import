using Aml.BOM.Import.Application.Models;
using Aml.BOM.Import.Shared.Interfaces;

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
        // TODO: Implement BOM file import logic
        await Task.CompletedTask;
        return new ImportFileResponse { Success = false, Message = "Not implemented" };
    }

    public async Task<IEnumerable<object>> GetAllBomsAsync()
    {
        return await _bomImportRepository.GetAllAsync();
    }

    public async Task<ValidationResult> ValidateBomAsync(int bomImportRecordId)
    {
        // TODO: Implement BOM validation logic
        await Task.CompletedTask;
        return new ValidationResult { IsValid = false };
    }
}
