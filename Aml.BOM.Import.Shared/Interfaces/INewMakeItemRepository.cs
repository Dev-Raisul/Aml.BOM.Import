using Aml.BOM.Import.Domain.Entities;

namespace Aml.BOM.Import.Shared.Interfaces;

public interface INewMakeItemRepository
{
    Task<IEnumerable<NewMakeItem>> GetAllAsync();
    Task<NewMakeItem?> GetByIdAsync(int id);
    Task<int> AddAsync(NewMakeItem newMakeItem);
    Task UpdateAsync(NewMakeItem newMakeItem);
    Task DeleteAsync(int id);
    Task<IEnumerable<NewMakeItem>> GetByStatusAsync(int status);
    Task BulkUpdateFieldAsync(IEnumerable<int> itemIds, string fieldName, object value);
    Task MarkAsIntegratedAsync(string itemCode, string importFileName);

    /// <summary>
    /// Copies all new make items from isBOMImportBills for the given import file into
    /// isBOMImport_NewMakeItems.  Only the first occurrence of each unique item code is
    /// inserted; items that already exist in the table are skipped.
    /// </summary>
    Task<int> CopyFromBillsAsync(string importFileName);
}
