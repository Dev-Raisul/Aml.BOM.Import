namespace Aml.BOM.Import.Shared.Interfaces;

/// <summary>
/// Repository for accessing Sage CI_Item table
/// </summary>
public interface ISageItemRepository
{
    /// <summary>
    /// Checks if an item exists in Sage CI_Item table
    /// </summary>
    Task<bool> ItemExistsAsync(string itemCode);
    
    /// <summary>
    /// Gets item type (Buy/Make) from Sage
    /// </summary>
    Task<string?> GetItemTypeAsync(string itemCode);
    
    /// <summary>
    /// Gets item details from Sage
    /// </summary>
    Task<SageItemInfo?> GetItemInfoAsync(string itemCode);
    
    /// <summary>
    /// Checks if multiple items exist (batch operation)
    /// </summary>
    Task<Dictionary<string, bool>> ItemsExistAsync(IEnumerable<string> itemCodes);
    
    /// <summary>
    /// Legacy method - kept for backwards compatibility
    /// </summary>
    Task<object?> GetByItemCodeAsync(string itemCode);
    
    /// <summary>
    /// Search items by term
    /// </summary>
    Task<IEnumerable<object>> SearchAsync(string searchTerm);
    
    /// <summary>
    /// Search items with full details (for copy from item functionality)
    /// </summary>
    Task<IEnumerable<object>> SearchItemsWithDetailsAsync(string searchTerm);
    
    /// <summary>
    /// Legacy method - use ItemExistsAsync instead
    /// </summary>
    Task<bool> ExistsAsync(string itemCode);
    
    /// <summary>
    /// Checks if a BillNo exists in BM_BillHeader table (for duplicate BOM detection)
    /// </summary>
    Task<bool> BillExistsInBomHeaderAsync(string billNo);
}

/// <summary>
/// Information about a Sage item
/// </summary>
public class SageItemInfo
{
    public string ItemCode { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ItemType { get; set; } // Buy, Make
    public bool Exists { get; set; }
    public string? Category { get; set; }
    public decimal? StandardCost { get; set; }
}
