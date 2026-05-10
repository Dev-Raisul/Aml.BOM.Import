namespace Aml.BOM.Import.Domain.Entities;

/// <summary>
/// Detailed Sage item information for display in search/copy dialogs
/// </summary>
public class SageItemDetails
{
    public string ItemCode { get; set; } = string.Empty;
    public string ItemDescription { get; set; } = string.Empty;
    public string ProductLine { get; set; } = string.Empty;
    public string ProductType { get; set; } = string.Empty;
    public string Procurement { get; set; } = string.Empty;
    public string StandardUnitOfMeasure { get; set; } = string.Empty;
    public string SubProductFamily { get; set; } = string.Empty;
    public bool StagedItem { get; set; }
    public bool Coated { get; set; }
    public bool GoldenStandard { get; set; }
}
