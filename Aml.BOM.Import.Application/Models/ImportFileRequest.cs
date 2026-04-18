namespace Aml.BOM.Import.Application.Models;

public class ImportFileRequest
{
    public string FilePath { get; set; } = string.Empty;
    public string ImportedBy { get; set; } = string.Empty;
}
