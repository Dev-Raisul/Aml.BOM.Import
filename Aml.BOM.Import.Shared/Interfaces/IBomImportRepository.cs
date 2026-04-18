namespace Aml.BOM.Import.Shared.Interfaces;

public interface IBomImportRepository
{
    Task<IEnumerable<object>> GetAllAsync();
    Task<object?> GetByIdAsync(int id);
    Task<int> AddAsync(object bomImportRecord);
    Task UpdateAsync(object bomImportRecord);
    Task DeleteAsync(int id);
    Task<IEnumerable<object>> GetByStatusAsync(int status);
    Task<bool> IsDuplicateAsync(string bomNumber);
}
