using Aml.BOM.Import.Domain.Enums;

namespace Aml.BOM.Import.Domain.Entities;

public class BomImportRecord
{
    public int Id { get; set; }
    public string BomNumber { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public DateTime ImportDate { get; set; }
    public string ImportedBy { get; set; } = string.Empty;
    public BomIntegrationStatus Status { get; set; }
    public DateTime? IntegratedDate { get; set; }
    public string? IntegratedBy { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
    
    public ICollection<BomImportLine> Lines { get; set; } = new List<BomImportLine>();
}
