namespace Aml.BOM.Import.Application.Models;

public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public int NewBuyItemsCount { get; set; }
    public int NewMakeItemsCount { get; set; }
}
