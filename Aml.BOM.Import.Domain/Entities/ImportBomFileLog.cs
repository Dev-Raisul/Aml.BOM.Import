namespace Aml.BOM.Import.Domain.Entities;

public class ImportBomFileLog
{
    public int FileId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public DateTime UploadDate { get; set; }
}
