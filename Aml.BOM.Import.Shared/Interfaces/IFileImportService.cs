namespace Aml.BOM.Import.Shared.Interfaces;

public interface IFileImportService
{
    Task<object> ImportFileAsync(string filePath);
    Task<bool> ValidateFileFormatAsync(string filePath);
    IEnumerable<string> GetSupportedFileExtensions();
}
