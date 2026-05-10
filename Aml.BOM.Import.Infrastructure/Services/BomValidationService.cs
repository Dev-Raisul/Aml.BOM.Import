using Aml.BOM.Import.Domain.Entities;
using Aml.BOM.Import.Shared.Interfaces;

namespace Aml.BOM.Import.Infrastructure.Services;

public class BomValidationService : IBomValidationService
{
    private readonly ISageItemRepository _sageItemRepository;
    private readonly IBomImportBillRepository _billRepository;
    private readonly IImportBomFileLogRepository _fileLogRepository;
    private readonly ILoggerService _logger;

    public BomValidationService(
        ISageItemRepository sageItemRepository,
        IBomImportBillRepository billRepository,
        IImportBomFileLogRepository fileLogRepository,
        ILoggerService logger)
    {
        _sageItemRepository = sageItemRepository;
        _billRepository = billRepository;
        _fileLogRepository = fileLogRepository;
        _logger = logger;
    }

    public async Task<ValidationResult> ValidateBillAsync(BomImportBill bill)
    {
        _logger.LogDebug("Validating bill Id: {0}, Component: {1}", bill.Id, bill.ComponentItemCode);

        var result = new ValidationResult { IsValid = true };

        if(bill.ComponentItemCode == "PH-ACL-95.92-35-HV1" || bill.ComponentItemCode == "PH-DR-ACL-8FT-SO-DE" || bill.ComponentItemCode == "PH-UNV-JNR-END-1" || bill.ComponentItemCode == "PH-ACL5-ENDPLATES-XP-1") {
            var a = 0;
        }

        // Check if it's a duplicate BOM
        if (!string.IsNullOrWhiteSpace(bill.ParentItemCode))
        {
            result.IsDuplicate = await IsDuplicateBomAsync(bill.ParentItemCode, bill.BOMNumber ?? string.Empty,bill.ImportFileName);
            if (result.IsDuplicate)
            {
                result.IsValid = false;
                result.ValidationMessage = "Duplicate BOM - Parent item already exists in BM_BillHeader table";
                result.Errors.Add("This BOM is a duplicate and will be ignored");
                return result;
            }
        }

        // Validate parent item (if specified)
        if (!string.IsNullOrWhiteSpace(bill.ParentItemCode))
        {
            var parentInfo = await _sageItemRepository.GetItemInfoAsync(bill.ParentItemCode);
            result.ParentExists = parentInfo?.Exists ?? false;
            result.ParentItemType = parentInfo?.ItemType;

            if (!result.ParentExists)
            {
                result.Warnings.Add($"Parent item '{bill.ParentItemCode}' not found in Sage");
            }
        }

        // Validate component item (required)
        var componentInfo = await _sageItemRepository.GetItemInfoAsync(bill.ComponentItemCode);
        result.ComponentExists = componentInfo?.Exists ?? false;
        result.ComponentItemType = componentInfo?.ItemType;

        if (!result.ComponentExists)
        {
            result.IsValid = false;
            result.Errors.Add($"Component item '{bill.ComponentItemCode}' not found in Sage");
            result.ValidationMessage = "Component item not found in Sage - New item required";
        }

        // Build validation message
        if (result.Errors.Any())
        {
            result.ValidationMessage = string.Join("; ", result.Errors);
        }
        else if (result.Warnings.Any())
        {
            result.ValidationMessage = string.Join("; ", result.Warnings);
        }
        else
        {
            result.ValidationMessage = "Validation successful";
        }

        return result;
    }

    public async Task<ImportValidationResult> ValidateImportFileAsync(string fileName)
    {
        _logger.LogInformation("Validating import file: {0}", fileName);

        var result = new ImportValidationResult();

        try
        {
            // Get all bills from this file
            var bills = (await _billRepository.GetByFileNameAsync(fileName)).ToList();
            result.TotalRecords = bills.Count;

            if (!bills.Any())
            {
                result.Errors.Add("No records found for this file");
                return result;
            }

            // First, mark duplicates
            var duplicateCount = await MarkDuplicateBillsAsync(fileName);
            result.DuplicateBoms = duplicateCount;

            // Reload bills after duplicate marking
            bills = (await _billRepository.GetByFileNameAsync(fileName)).ToList();

            // Validate each bill
            foreach (var bill in bills.Where(b => b.Status == "New"))
            {
                var validationResult = await ValidateBillAsync(bill);

                // Update bill based on validation
                if (validationResult.IsDuplicate)
                {
                    bill.Status = "Duplicate";
                    bill.ValidationMessage = validationResult.ValidationMessage;
                }
                else if (!validationResult.ComponentExists)
                {
                    // Determine if it's a buy or make item (simplified logic)
                    bill.Status = "NewBuyItem"; // Default to buy, could be enhanced with more logic
                    bill.ItemExists = false;
                    bill.ValidationMessage = validationResult.ValidationMessage;
                    result.NewBuyItems++;
                }
                else if (validationResult.IsValid)
                {
                    bill.Status = "Validated";
                    bill.DateValidated = DateTime.Now;
                    bill.ItemExists = validationResult.ComponentExists;
                    bill.ItemType = validationResult.ComponentItemType;
                    bill.ValidationMessage = validationResult.ValidationMessage;
                    result.ValidatedRecords++;
                }
                else
                {
                    bill.Status = "Failed";
                    bill.ValidationMessage = validationResult.ValidationMessage;
                    result.FailedRecords++;
                }

                // Update the bill in database
                await _billRepository.UpdateAsync(bill);
            }

            // Build status summary
            var summary = await _billRepository.GetStatusSummaryAsync();
            result.StatusSummary = summary;

            _logger.LogInformation("Validation complete for file: {0}. Validated: {1}, Failed: {2}, Duplicates: {3}",
                fileName, result.ValidatedRecords, result.FailedRecords, result.DuplicateBoms);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to validate import file: {0}", ex, fileName);
            result.Errors.Add($"Validation failed: {ex.Message}");
            return result;
        }
    }

    public async Task<ImportValidationResult> ValidateAllPendingAsync()
    {
        _logger.LogInformation("Validating all pending BOMs");

        var result = new ImportValidationResult();

        try
        {
            // Get all pending (New status) bills
            var bills = (await _billRepository.GetByStatusAsync("New")).ToList();
            result.TotalRecords = bills.Count;

            if (!bills.Any())
            {
                _logger.LogInformation("No pending BOMs to validate");
                return result;
            }

            // Group by filename to check duplicates per file
            var fileGroups = bills.GroupBy(b => b.ImportFileName);

            foreach (var fileGroup in fileGroups)
            {
                var fileResult = await ValidateImportFileAsync(fileGroup.Key);
                
                result.ValidatedRecords += fileResult.ValidatedRecords;
                result.NewBuyItems += fileResult.NewBuyItems;
                result.NewMakeItems += fileResult.NewMakeItems;
                result.DuplicateBoms += fileResult.DuplicateBoms;
                result.FailedRecords += fileResult.FailedRecords;
                result.Errors.AddRange(fileResult.Errors);
                result.Warnings.AddRange(fileResult.Warnings);
            }

            // Get final status summary
            result.StatusSummary = await _billRepository.GetStatusSummaryAsync();

            _logger.LogInformation("Validation of all pending complete. Total: {0}, Validated: {1}, Failed: {2}",
                result.TotalRecords, result.ValidatedRecords, result.FailedRecords);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to validate all pending BOMs", ex);
            result.Errors.Add($"Validation failed: {ex.Message}");
            return result;
        }
    }

    public async Task<ImportValidationResult> RevalidateAllPendingAsync()
    {
        _logger.LogInformation("Re-validating all pending BOMs");

        // Reset status of all non-duplicate bills back to "New" for re-validation
        var pendingStatuses = new[] { "Validated", "Failed", "NewBuyItem", "NewMakeItem" };
        
        foreach (var status in pendingStatuses)
        {
            var bills = await _billRepository.GetByStatusAsync(status);
            var ids = bills.Select(b => b.Id).ToList();
            
            if (ids.Any())
            {
                await _billRepository.UpdateBatchStatusAsync(ids, "New");
            }
        }

        // Now validate all pending
        return await ValidateAllPendingAsync();
    }

    public async Task<bool> IsFileAlreadyImportedAsync(string fileName)
    {
        _logger.LogDebug("Checking if file already imported: {0}", fileName);

        try
        {
            var existingLogs = await _fileLogRepository.GetAllAsync();
            var exists = existingLogs.Any(log => 
                log.FileName.Equals(fileName, StringComparison.OrdinalIgnoreCase));

            if (exists)
            {
                _logger.LogWarning("File already imported: {0}", fileName);
            }

            return exists;
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to check if file already imported: {0}", ex, fileName);
            return false;
        }
    }

    public async Task<bool> IsDuplicateBomAsync(string parentItemCode, string bomNumber,string importFileName)
    {
        if (string.IsNullOrWhiteSpace(parentItemCode))
            return false;

        _logger.LogDebug("Checking for duplicate BOM. Parent: {0}, BOM#: {1}", parentItemCode, bomNumber);

        try
        {
            // Check if parent item (BillNo) exists in BM_BillHeader table
            // This indicates the BOM already exists in the system
            var billExists = await _sageItemRepository.BillExistsInBomHeaderAsync(parentItemCode);
            if (billExists)
            {
                _logger.LogInformation("Duplicate BOM detected - BillNo exists in BM_BillHeader: {0}", parentItemCode);
                return true;
            }

            // Check if parent exists in previous imports
            var existingBills = await _billRepository.GetByParentItemCodeAsync(parentItemCode);
            var hasDuplicate = existingBills.Any(b =>b.ImportFileName != importFileName);

            if (hasDuplicate)
            {
                _logger.LogInformation("Duplicate BOM detected - Parent exists in previous import: {0}", parentItemCode);
            }

            return hasDuplicate;
        }
        catch (Exception ex)
        {
        _logger.LogError("Failed to check for duplicate BOM", ex);
            return false;
        }
    }

    public async Task<int> MarkDuplicateBillsAsync(string fileName)
    {
        _logger.LogInformation("Marking duplicate bills for file: {0}", fileName);

        int duplicateCount = 0;

        try
        {
            var bills = (await _billRepository.GetByFileNameAsync(fileName)).ToList();
            
            // Group by parent item code
            var parentGroups = bills
                .Where(b => !string.IsNullOrWhiteSpace(b.ParentItemCode))
                .GroupBy(b => b.ParentItemCode);

            foreach (var group in parentGroups)
            {
                var parentItemCode = group.Key;
                var isDuplicate = await IsDuplicateBomAsync(parentItemCode, group.First().BOMNumber,group.First().ImportFileName);

                if (isDuplicate)
                {
                    // Mark all bills with this parent as duplicate
                    var billIds = group.Select(b => b.Id).ToList();
                    await _billRepository.UpdateBatchStatusAsync(billIds, "Duplicate");
                    duplicateCount += billIds.Count;

                    // Update validation message
                    foreach (var bill in group)
                    {
                        await _billRepository.UpdateValidationAsync(
                            bill.Id,
                            false,
                            null,
                            "Duplicate BOM - Parent item already exists in BM_BillHeader table");
                    }

                    _logger.LogInformation("Marked {0} bills as duplicate for parent: {1}", 
                        billIds.Count, parentItemCode);
                }
            }

            return duplicateCount;
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to mark duplicate bills for file: {0}", ex, fileName);
            return 0;
        }
    }
}
