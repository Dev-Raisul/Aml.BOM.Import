using Aml.BOM.Import.Domain.Entities;
using Aml.BOM.Import.Shared.Interfaces;
using System.IO;

namespace Aml.BOM.Import.Infrastructure.Services;

public class FileImportService : IFileImportService
{
    private readonly IImportBomFileLogRepository _fileLogRepository;
    private readonly ILoggerService _logger;

    public FileImportService(IImportBomFileLogRepository fileLogRepository, ILoggerService logger)
    {
        _fileLogRepository = fileLogRepository;
        _logger = logger;
    }

    public async Task<object> ImportFileAsync(string filePath)
    {
        _logger.LogInformation("Starting BOM file import for: {0}", filePath);

        // Validate file exists
        if (!File.Exists(filePath))
        {
            _logger.LogError("File not found: {0}", null, filePath);
            throw new FileNotFoundException("The specified file does not exist.", filePath);
        }

        var fileName = Path.GetFileName(filePath);
        
        // Create file log entry
        var fileLog = new ImportBomFileLog
        {
            FileName = fileName,
            UploadDate = DateTime.Now
        };

        try
        {
            // Log the file upload
            var fileId = await _fileLogRepository.CreateAsync(fileLog);
            _logger.LogInformation("BOM file logged successfully. FileId: {0}, FileName: {1}", fileId, fileLog.FileName);

            // TODO: Implement actual file import logic
            // - Read CSV/Excel file
            // - Parse BOM data
            // - Create BomImportRecord and BomImportLine entities

            // Return the fileId
            var result = new
            {
                FileId = fileId,
                FileName = fileLog.FileName,
                Message = "File uploaded and logged successfully"
            };

            _logger.LogInformation("BOM file import initiated successfully. FileId: {0}", fileId);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to import BOM file: {0}", ex, filePath);
            throw;
        }
    }

    public async Task<bool> ValidateFileFormatAsync(string filePath)
    {
        _logger.LogDebug("Validating file format for: {0}", filePath);

        try
        {
            // Check if file exists
            if (!File.Exists(filePath))
            {
                _logger.LogWarning("File not found during validation: {0}", filePath);
                return false;
            }

            // Check file extension
            var extension = Path.GetExtension(filePath).ToLower();
            var supportedExtensions = GetSupportedFileExtensions();
            
            if (!supportedExtensions.Contains(extension))
            {
                _logger.LogWarning("Unsupported file extension: {0}. File: {1}", extension, filePath);
                return false;
            }

            // Check file size (max 50MB)
            var fileInfo = new FileInfo(filePath);
            const long maxFileSize = 50 * 1024 * 1024; // 50MB
            
            if (fileInfo.Length > maxFileSize)
            {
                _logger.LogWarning("File size exceeds maximum allowed. Size: {0} bytes, File: {1}", fileInfo.Length, filePath);
                return false;
            }

            _logger.LogInformation("File format validation passed for: {0}", filePath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError("Error during file format validation: {0}", ex, filePath);
            return false;
        }
    }

    public IEnumerable<string> GetSupportedFileExtensions()
    {
        return new[] { ".csv", ".xlsx", ".xls" };
    }
}
