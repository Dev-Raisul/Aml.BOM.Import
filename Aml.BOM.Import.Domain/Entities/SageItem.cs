using Aml.BOM.Import.Domain.Enums;

namespace Aml.BOM.Import.Domain.Entities;

public class SageItem
{
    public string ItemCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? LongDescription { get; set; }
    public ItemType ItemType { get; set; }
    public string UnitOfMeasure { get; set; } = string.Empty;
    public string? ProductGroup { get; set; }
    public string? Category { get; set; }
    public decimal? StandardCost { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastModifiedDate { get; set; }
}
