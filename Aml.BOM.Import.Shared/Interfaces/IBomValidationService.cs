using Aml.BOM.Import.Domain.Entities;

namespace Aml.BOM.Import.Shared.Interfaces;

/// <summary>
/// Service for validating BOM imports against Sage data
/// </summary>
public interface IBomValidationService
{
    /// <summary>
    /// Validates a single BOM bill record against Sage CI_Item table
    /// </summary>
    Task<ValidationResult> ValidateBillAsync(BomImportBill bill);
    
    /// <summary>
    /// Validates all bills from a specific import file
    /// </summary>
    Task<ImportValidationResult> ValidateImportFileAsync(string fileName);
    
    /// <summary>
    /// Validates all pending (New status) BOM bills
    /// </summary>
    Task<ImportValidationResult> ValidateAllPendingAsync();
    
    /// <summary>
    /// Re-validates all pending BOMs (called when user clicks Revalidate button)
    /// </summary>
    Task<ImportValidationResult> RevalidateAllPendingAsync();
    
    /// <summary>
    /// Checks if a filename already exists in the import log
    /// </summary>
    Task<bool> IsFileAlreadyImportedAsync(string fileName);
    
    /// <summary>
    /// Checks if a BOM already exists (duplicate detection)
    /// </summary>
    Task<bool> IsDuplicateBomAsync(string parentItemCode, string bomNumber,string importFileName);
    
    /// <summary>
    /// Marks all bills with the same parent/BOM as duplicates
    /// </summary>
    Task<int> MarkDuplicateBillsAsync(string fileName);
}

/// <summary>
/// Result of validating a single BOM bill
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; }
    public bool ParentExists { get; set; }
    public bool ComponentExists { get; set; }
    public string? ParentItemType { get; set; } // Buy, Make
    public string? ComponentItemType { get; set; } // Buy, Make
    public bool IsDuplicate { get; set; }
    public string? ValidationMessage { get; set; }
    public List<string> Warnings { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}

/// <summary>
/// Result of validating an entire import
/// </summary>
public class ImportValidationResult
{
    public int TotalRecords { get; set; }
    public int ValidatedRecords { get; set; }
    public int NewBuyItems { get; set; }
    public int NewMakeItems { get; set; }
    public int DuplicateBoms { get; set; }
    public int FailedRecords { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public Dictionary<string, int> StatusSummary { get; set; } = new();
}
