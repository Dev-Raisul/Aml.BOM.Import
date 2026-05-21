using Aml.BOM.Import.Domain.Models;

namespace Aml.BOM.Import.Shared.Interfaces;

public interface IFileImportService
{
    Task<ImportFileResponse> ImportFileAsync(string filePath);
    Task<bool> ValidateFileFormatAsync(string filePath);
    IEnumerable<string> GetSupportedFileExtensions();
}
