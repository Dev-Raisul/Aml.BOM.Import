namespace Aml.BOM.Import.Application.Models;

public class ImportFileResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    
    // File Information
    public int? FileId { get; set; }
    public string FileName { get; set; } = string.Empty;
    
    // Import Statistics
    public int ImportedRecords { get; set; }
    public int TabsProcessed { get; set; }
    
    // Validation Statistics
    public int ValidatedRecords { get; set; }
    public int NewBuyItems { get; set; }
    public int NewMakeItems { get; set; }
    public int DuplicateBoms { get; set; }
    public int FailedRecords { get; set; }
    
    // Messages
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    
    // Legacy property (kept for backward compatibility)
    [Obsolete("Use FileId instead")]
    public int? BomImportRecordId 
    { 
        get => FileId; 
        set => FileId = value; 
    }
}
