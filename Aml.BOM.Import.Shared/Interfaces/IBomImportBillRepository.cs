using Aml.BOM.Import.Domain.Entities;

namespace Aml.BOM.Import.Shared.Interfaces;

/// <summary>
/// Repository interface for BOM Import Bills operations
/// </summary>
public interface IBomImportBillRepository
{
    // Create operations
    Task<int> CreateAsync(BomImportBill bill);
    Task<int> CreateBatchAsync(IEnumerable<BomImportBill> bills);
    
    // Read operations
    Task<BomImportBill?> GetByIdAsync(int id);
    Task<IEnumerable<BomImportBill>> GetAllAsync();
    Task<IEnumerable<BomImportBill>> GetByFileNameAsync(string fileName);
    Task<IEnumerable<BomImportBill>> GetByStatusAsync(string status);
    Task<IEnumerable<BomImportBill>> GetByTabNameAsync(string tabName);
    Task<IEnumerable<BomImportBill>> GetByFileAndTabAsync(string fileName, string tabName);
    Task<IEnumerable<BomImportBill>> GetByComponentItemCodeAsync(string itemCode);
    Task<IEnumerable<BomImportBill>> GetByParentItemCodeAsync(string itemCode);
    Task<IEnumerable<BomImportBill>> GetRecentAsync(int count = 100);
    
    // Update operations
    Task UpdateAsync(BomImportBill bill);
    Task UpdateStatusAsync(int id, string status, DateTime? validatedDate = null, DateTime? integratedDate = null);
    Task UpdateValidationAsync(int id, bool itemExists, string? itemType, string? validationMessage);
    Task UpdateBatchStatusAsync(IEnumerable<int> ids, string status);
    
    // Delete operations
    Task<bool> DeleteAsync(int id);
    Task<int> DeleteByFileNameAsync(string fileName);
    
    // Statistics
    Task<int> GetCountByStatusAsync(string status);
    Task<int> GetCountByFileNameAsync(string fileName);
    Task<Dictionary<string, int>> GetStatusSummaryAsync();
}
