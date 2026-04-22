namespace Aml.BOM.Import.Domain.Entities;

/// <summary>
/// Represents a BOM (Bill of Materials) import record with all details from the Excel file
/// </summary>
public class BomImportBill
{
    public int Id { get; set; }
    
    // Import Metadata
    public string ImportFileName { get; set; } = string.Empty;
    public DateTime ImportDate { get; set; }
    public string ImportWindowsUser { get; set; } = string.Empty;
    public string TabName { get; set; } = string.Empty;
    
    // Status Information
    public string Status { get; set; } = "New"; // Validated, Integrated, New Buy Item, New Make Item
    public DateTime? DateValidated { get; set; }
    public DateTime? DateIntegrated { get; set; }
    
    // BOM Header Information (from Excel columns)
    public string? ParentItemCode { get; set; }
    public string? ParentDescription { get; set; }
    public string? BOMLevel { get; set; }
    public string? BOMNumber { get; set; }
    
    // Component/Line Item Information
    public int LineNumber { get; set; }
    public string ComponentItemCode { get; set; } = string.Empty;
    public string? ComponentDescription { get; set; }
    public decimal Quantity { get; set; }
    public string? UnitOfMeasure { get; set; }
    public string? Reference { get; set; }
    public string? Notes { get; set; }
    
    // Additional Fields from Excel
    public string? Category { get; set; }
    public string? Type { get; set; }
    public decimal? UnitCost { get; set; }
    public decimal? ExtendedCost { get; set; }
    
    // Validation Fields
    public bool ItemExists { get; set; }
    public string? ItemType { get; set; } // Buy, Make
    public string? ValidationMessage { get; set; }
    
    // Audit Fields
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
}

public enum BomImportStatus
{
    New,
    Validated,
    Integrated,
    NewBuyItem,
    NewMakeItem,
    Failed
}
