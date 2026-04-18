namespace Aml.BOM.Import.Application.Models;

public class ImportFileResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int? BomImportRecordId { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}
