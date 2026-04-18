namespace Aml.BOM.Import.Domain.Entities;

public class BomImportLine
{
    public int Id { get; set; }
    public int BomImportRecordId { get; set; }
    public int LineNumber { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string UnitOfMeasure { get; set; } = string.Empty;
    public string? Notes { get; set; }
    
    public BomImportRecord BomImportRecord { get; set; } = null!;
}
