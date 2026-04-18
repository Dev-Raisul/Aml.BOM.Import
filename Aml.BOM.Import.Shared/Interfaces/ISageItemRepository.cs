namespace Aml.BOM.Import.Shared.Interfaces;

public interface ISageItemRepository
{
    Task<object?> GetByItemCodeAsync(string itemCode);
    Task<IEnumerable<object>> SearchAsync(string searchTerm);
    Task<bool> ExistsAsync(string itemCode);
}
