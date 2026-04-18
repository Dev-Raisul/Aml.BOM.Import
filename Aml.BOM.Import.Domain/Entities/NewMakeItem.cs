using Aml.BOM.Import.Domain.Enums;

namespace Aml.BOM.Import.Domain.Entities;

public class NewMakeItem
{
    public int Id { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? LongDescription { get; set; }
    public string UnitOfMeasure { get; set; } = string.Empty;
    public string? DrawingNumber { get; set; }
    public string? Revision { get; set; }
    public string? ProductGroup { get; set; }
    public string? Category { get; set; }
    public decimal? StandardCost { get; set; }
    public ItemIntegrationStatus Status { get; set; }
    public DateTime IdentifiedDate { get; set; }
    public string IdentifiedBy { get; set; } = string.Empty;
    public DateTime? IntegratedDate { get; set; }
    public string? IntegratedBy { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
}
