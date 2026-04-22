using Aml.BOM.Import.Domain.Entities;

namespace Aml.BOM.Import.Shared.Interfaces;

public interface IImportBomFileLogRepository
{
    Task<int> CreateAsync(ImportBomFileLog fileLog);
    Task<ImportBomFileLog?> GetByIdAsync(int fileId);
    Task<IEnumerable<ImportBomFileLog>> GetAllAsync();
    Task<IEnumerable<ImportBomFileLog>> GetRecentAsync(int count = 50);
    Task<bool> DeleteAsync(int fileId);
}
