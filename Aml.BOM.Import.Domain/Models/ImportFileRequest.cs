namespace Aml.BOM.Import.Domain.Models;

public class ImportFileRequest
{
    public string FilePath { get; set; } = string.Empty;
    public string ImportedBy { get; set; } = string.Empty;
}
