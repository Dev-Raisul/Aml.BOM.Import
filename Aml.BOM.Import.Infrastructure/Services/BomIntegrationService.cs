using System.Runtime.InteropServices;
using Aml.BOM.Import.Domain.Entities;
using Aml.BOM.Import.Shared.Interfaces;

namespace Aml.BOM.Import.Infrastructure.Services;

public class BomIntegrationService : IBomIntegrationService
{
    private readonly INewMakeItemRepository _makeItemRepository;
    private readonly IBomImportBillRepository _bomBillRepository;
    private readonly ISettingsService _settingsService;
    private readonly ILoggerService _logger;

    public BomIntegrationService(
        INewMakeItemRepository makeItemRepository,
        IBomImportBillRepository bomBillRepository,
        ISettingsService settingsService,
        ILoggerService logger)
    {
        _makeItemRepository = makeItemRepository;
        _bomBillRepository = bomBillRepository;
        _settingsService = settingsService;
        _logger = logger;
    }

    /// <summary>
    /// Integrates new make items into Sage 100 using CI_ItemCode_bus
    /// </summary>
    public async Task<bool> IntegrateNewItemsAsync(IEnumerable<object> items)
    {
        return await Task.Run(async () =>
        {
            var itemsList = items.Cast<NewMakeItem>().ToList();
            _logger.LogInformation("Starting integration of {0} new make items", itemsList.Count);

            if (!itemsList.Any())
            {
                _logger.LogWarning("No items to integrate");
                return false;
            }

            // Load settings
            var settings = await _settingsService.GetSettingsAsync() as Application.Models.AppSettings;
            if (settings?.SageSettings == null)
            {
                _logger.LogError("Sage settings not configured", null);
                throw new InvalidOperationException("Sage settings are not configured. Please configure Sage settings first.");
            }

            SageSessionService? session = null;
            int successCount = 0;
            int failedCount = 0;
            var errors = new List<string>();

            try
            {
                // Initialize Sage session
                session = new SageSessionService(settings.SageSettings, _logger);
                session.InitializeSession();

                // Set program context for Item Maintenance
                session.SetProgramContext("CI_ItemCode_ui");

                // Process each item (using their current in-memory values, not database values)
                foreach (var item in itemsList)
                {
                    try
                    {
                        _logger.LogInformation("Integrating item: {0} - {1}", item.ItemCode, item.ItemDescription);

                        // Validate required fields
                        if (string.IsNullOrWhiteSpace(item.ProductLine))
                        {
                            errors.Add($"{item.ItemCode}: Product Line is required");
                            failedCount++;
                            continue;
                        }

                        // Create item in Sage using current in-memory values
                        bool success = await IntegrateSingleItemAsync(session, item);
                        
                        if (success)
                        {
                            // Mark as integrated in database
                            await _makeItemRepository.MarkAsIntegratedAsync(item.ItemCode, item.ImportFileName);
                            successCount++;
                            _logger.LogInformation("Item integrated successfully: {0}", item.ItemCode);
                        }
                        else
                        {
                            failedCount++;
                            errors.Add($"{item.ItemCode}: Integration failed");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Failed to integrate item {item.ItemCode}", ex);
                        failedCount++;
                        errors.Add($"{item.ItemCode}: {ex.Message}");
                    }
                }

                _logger.LogInformation("Integration complete: {0} succeeded, {1} failed", successCount, failedCount);

                if (errors.Any())
                {
                    _logger.LogWarning("Integration errors: {0}", string.Join("; ", errors));
                }

                return failedCount == 0;
            }
            catch (Exception ex)
            {
                _logger.LogError("Fatal error during item integration", ex);
                throw;
            }
            finally
            {
                session?.Dispose();
            }
        });
    }

    /// <summary>
    /// Integrates a single make item into Sage
    /// </summary>
    private async Task<bool> IntegrateSingleItemAsync(SageSessionService session, NewMakeItem item)
    {
        dynamic? itemBus = null;

        try
        {
            // Create CI_ItemCode_bus object
            itemBus = session.CreateBusinessObject("CI_ItemCode_bus");

            // Set key (Item Code) to create new item
            int retVal = itemBus.nSetKey(item.ItemCode);
            if (retVal == 0)
            {
                string errorMsg = itemBus.sLastErrorMsg ?? "Unknown error";
                throw new InvalidOperationException($"nSetKey failed for {item.ItemCode}: {errorMsg}");
            }

            // Set field values
            retVal = itemBus.nSetValue("ItemCodeDesc$", item.ItemDescription ?? "");
            if (retVal == 0)
            {
                string errorMsg = itemBus.sLastErrorMsg ?? "Unknown error";
                _logger.LogWarning("Failed to set ItemCodeDesc for {0}: {1}", item.ItemCode, errorMsg);
            }

            retVal = itemBus.nSetValue("ProductLine$", item.ProductLine);
            if (retVal == 0)
            {
                string errorMsg = itemBus.sLastErrorMsg ?? "Unknown error";
                throw new InvalidOperationException($"Failed to set ProductLine for {item.ItemCode}: {errorMsg}");
            }

            retVal = itemBus.nSetValue("ItemType$", "1"); // 1 = Regular
            if (retVal == 0)
            {
                string errorMsg = itemBus.sLastErrorMsg ?? "Unknown error";
                _logger.LogWarning("Failed to set ItemType for {0}: {1}", item.ItemCode, errorMsg);
            }

            retVal = itemBus.nSetValue("ProcurementType$", "M"); // M = Make
            if (retVal == 0)
            {
                string errorMsg = itemBus.sLastErrorMsg ?? "Unknown error";
                _logger.LogWarning("Failed to set ProcurementType for {0}: {1}", item.ItemCode, errorMsg);
            }

            retVal = itemBus.nSetValue("StandardUnitOfMeasure$", item.StandardUnitOfMeasure ?? "EACH");
            if (retVal == 0)
            {
                string errorMsg = itemBus.sLastErrorMsg ?? "Unknown error";
                _logger.LogWarning("Failed to set StandardUnitOfMeasure for {0}: {1}", item.ItemCode, errorMsg);
            }

            // Optional fields
            if (!string.IsNullOrWhiteSpace(item.SubProductFamily))
            {
                itemBus.nSetValue("SubProductFamily$", item.SubProductFamily);
            }

            // Write the item to Sage
            retVal = itemBus.nWrite();
            if (retVal == 0)
            {
                string errorMsg = itemBus.sLastErrorMsg ?? "Unknown error";
                throw new InvalidOperationException($"nWrite failed for {item.ItemCode}: {errorMsg}");
            }

            _logger.LogInformation("Item written to Sage successfully: {0}", item.ItemCode);
            await Task.CompletedTask;
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error integrating item {item.ItemCode}", ex);
            return false;
        }
        finally
        {
            if (itemBus != null)
            {
                try
                {
                    itemBus.DropObject();
                    Marshal.ReleaseComObject(itemBus);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Error releasing item business object: {0}", ex.Message);
                }
            }
        }
    }

    /// <summary>
    /// Integrates a BOM into Sage 100 using BM_BillHeader_bus and BM_BillDetail_bus
    /// </summary>
    public async Task<bool> IntegrateBomAsync(int bomImportRecordId)
    {
        return await Task.Run(async () =>
        {
            _logger.LogInformation("Starting BOM integration for record ID: {0}", bomImportRecordId);

            // Load BOM details from database
            var bomRecord = await _bomBillRepository.GetByIdAsync(bomImportRecordId);
            if (bomRecord == null)
            {
                _logger.LogError("BOM record not found: ID {0}", null, bomImportRecordId);
                throw new InvalidOperationException($"BOM record not found: {bomImportRecordId}");
            }

            if (string.IsNullOrWhiteSpace(bomRecord.ParentItemCode))
            {
                throw new InvalidOperationException("Parent item code is required for BOM integration");
            }

            // Get all BOM lines for this parent item
            var bomLines = await _bomBillRepository.GetByParentItemCodeAsync(bomRecord.ParentItemCode);
            var bomLinesList = bomLines.ToList();

            if (!bomLinesList.Any())
            {
                _logger.LogWarning("No BOM lines found for parent: {0}", bomRecord.ParentItemCode);
                return false;
            }

            // Load settings
            var settings = await _settingsService.GetSettingsAsync() as Application.Models.AppSettings;
            if (settings?.SageSettings == null)
            {
                throw new InvalidOperationException("Sage settings are not configured");
            }

            SageSessionService? session = null;

            try
            {
                // Initialize Sage session
                session = new SageSessionService(settings.SageSettings, _logger);
                session.InitializeSession();

                // Set program context for Bill of Materials
                session.SetProgramContext("BM_BillMaintenance_ui");

                // Create BOM header
                bool headerCreated = await IntegrateBomHeaderAsync(session, bomRecord);
                if (!headerCreated)
                {
                    throw new InvalidOperationException($"Failed to create BOM header for {bomRecord.ParentItemCode}");
                }

                // Create BOM detail lines
                int successCount = 0;
                int failedCount = 0;

                foreach (var line in bomLinesList)
                {
                    try
                    {
                        bool lineCreated = await IntegrateBomLineAsync(session, bomRecord.ParentItemCode, line);
                        if (lineCreated)
                        {
                            successCount++;
                        }
                        else
                        {
                            failedCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Failed to integrate BOM line: {line.ComponentItemCode}", ex);
                        failedCount++;
                    }
                }

                _logger.LogInformation("BOM integration complete: {0} lines succeeded, {1} failed", 
                    successCount, failedCount);

                // Update integration status if all lines succeeded
                if (failedCount == 0)
                {
                    foreach (var line in bomLinesList)
                    {
                        await _bomBillRepository.UpdateStatusAsync(
                            line.Id, 
                            "Integrated", 
                            DateTime.Now, 
                            DateTime.Now);
                    }
                }

                return failedCount == 0;
            }
            catch (Exception ex)
            {
                _logger.LogError("Fatal error during BOM integration", ex);
                throw;
            }
            finally
            {
                session?.Dispose();
            }
        });
    }

    /// <summary>
    /// Integrates BOM header into Sage
    /// </summary>
    private async Task<bool> IntegrateBomHeaderAsync(SageSessionService session, BomImportBill bomRecord)
    {
        dynamic? billHeader = null;

        try
        {
            _logger.LogInformation("Creating BOM header for: {0}", bomRecord.ParentItemCode);

            billHeader = session.CreateBusinessObject("BM_BillHeader_bus");

            // Set key (Parent Item Code)
            int retVal = billHeader.nSetKey(bomRecord.ParentItemCode);
            if (retVal == 0)
            {
                string errorMsg = billHeader.sLastErrorMsg ?? "Unknown error";
                throw new InvalidOperationException($"nSetKey failed for BOM header {bomRecord.ParentItemCode}: {errorMsg}");
            }

            // Set header fields
            if (!string.IsNullOrWhiteSpace(bomRecord.ParentDescription))
            {
                billHeader.nSetValue("BillDescription$", bomRecord.ParentDescription);
            }

            // Write header
            retVal = billHeader.nWrite();
            if (retVal == 0)
            {
                string errorMsg = billHeader.sLastErrorMsg ?? "Unknown error";
                throw new InvalidOperationException($"Failed to write BOM header for {bomRecord.ParentItemCode}: {errorMsg}");
            }

            _logger.LogInformation("BOM header created successfully: {0}", bomRecord.ParentItemCode);
            await Task.CompletedTask;
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error creating BOM header for {bomRecord.ParentItemCode}", ex);
            return false;
        }
        finally
        {
            if (billHeader != null)
            {
                try
                {
                    billHeader.DropObject();
                    Marshal.ReleaseComObject(billHeader);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Error releasing BOM header object: {0}", ex.Message);
                }
            }
        }
    }

    /// <summary>
    /// Integrates a single BOM detail line into Sage
    /// </summary>
    private async Task<bool> IntegrateBomLineAsync(SageSessionService session, string parentItemCode, BomImportBill line)
    {
        dynamic? billDetail = null;

        try
        {
            _logger.LogInformation("Creating BOM line: {0} -> {1} (Qty: {2})", 
                parentItemCode, line.ComponentItemCode, line.Quantity);

            billDetail = session.CreateBusinessObject("BM_BillDetail_bus");

            // Set keys (Parent + Component)
            int retVal = billDetail.nSetKeyValue("BillNo$", parentItemCode);
            if (retVal == 0)
            {
                string errorMsg = billDetail.sLastErrorMsg ?? "Unknown error";
                throw new InvalidOperationException($"Failed to set BillNo for detail line: {errorMsg}");
            }

            retVal = billDetail.nSetKeyValue("ComponentItemCode$", line.ComponentItemCode);
            if (retVal == 0)
            {
                string errorMsg = billDetail.sLastErrorMsg ?? "Unknown error";
                throw new InvalidOperationException($"Failed to set ComponentItemCode: {errorMsg}");
            }

            // Set quantity
            retVal = billDetail.nSetValue("QuantityPerBill", line.Quantity);
            if (retVal == 0)
            {
                string errorMsg = billDetail.sLastErrorMsg ?? "Unknown error";
                _logger.LogWarning("Failed to set QuantityPerBill: {0}", errorMsg);
            }

            // Set optional fields
            if (!string.IsNullOrWhiteSpace(line.Reference))
            {
                billDetail.nSetValue("Reference$", line.Reference);
            }

            if (!string.IsNullOrWhiteSpace(line.Notes))
            {
                billDetail.nSetValue("CommentText$", line.Notes);
            }

            // Write detail line
            retVal = billDetail.nWrite();
            if (retVal == 0)
            {
                string errorMsg = billDetail.sLastErrorMsg ?? "Unknown error";
                throw new InvalidOperationException($"Failed to write BOM detail line: {errorMsg}");
            }

            _logger.LogInformation("BOM line created successfully: {0} -> {1}", 
                parentItemCode, line.ComponentItemCode);
            
            await Task.CompletedTask;
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error creating BOM line: {parentItemCode} -> {line.ComponentItemCode}", ex);
            return false;
        }
        finally
        {
            if (billDetail != null)
            {
                try
                {
                    billDetail.DropObject();
                    Marshal.ReleaseComObject(billDetail);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Error releasing BOM detail object: {0}", ex.Message);
                }
            }
        }
    }

    public async Task<object> GetIntegrationStatusAsync(int bomImportRecordId)
    {
        var bomRecord = await _bomBillRepository.GetByIdAsync(bomImportRecordId);
        
        if (bomRecord == null)
        {
            return new { Status = "Not Found", IntegratedDate = (DateTime?)null };
        }

        return new
        {
            Status = bomRecord.Status,
            IntegratedDate = bomRecord.DateIntegrated,
            ParentItemCode = bomRecord.ParentItemCode,
            ComponentItemCode = bomRecord.ComponentItemCode
        };
    }
}
