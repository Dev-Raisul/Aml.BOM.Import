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
    /// Integrates a BOM into Sage 100 using BM_Bill_bus (combined header and lines)
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
            var bomLinesList = bomLines.Where(b => b.Status == "Validated").ToList();

            if (!bomLinesList.Any())
            {
                _logger.LogWarning("No validated BOM lines found for parent: {0}", bomRecord.ParentItemCode);
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

                // Set program context for Bill of Materials (use BM_Bill_ui based on VBS script)
                session.SetProgramContext("BM_Bill_ui");

                // Create BOM using BM_Bill_bus (includes header and lines)
                bool bomCreated = await IntegrateBomWithLinesAsync(session, bomRecord, bomLinesList);
                
                if (!bomCreated)
                {
                    throw new InvalidOperationException($"Failed to create BOM for {bomRecord.ParentItemCode}");
                }

                // Update integration status for all lines
                foreach (var line in bomLinesList)
                {
                    await _bomBillRepository.UpdateStatusAsync(
                        line.Id, 
                        "Integrated", 
                        DateTime.Now, 
                        DateTime.Now);
                }

                _logger.LogInformation("BOM integration complete for parent: {0}, Lines: {1}", 
                    bomRecord.ParentItemCode, bomLinesList.Count);

                return true;
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
    /// Integrates a complete BOM (header + lines) using BM_Bill_bus
    /// </summary>
    private async Task<bool> IntegrateBomWithLinesAsync(SageSessionService session, BomImportBill bomHeader, List<BomImportBill> bomLines)
    {
        dynamic? billBus = null;
        dynamic? oLines = null;

        try
        {
            _logger.LogInformation("Creating BOM for parent: {0} with {1} lines", bomHeader.ParentItemCode, bomLines.Count);

            // Create BM_Bill_bus object (combines header and detail lines)
            billBus = session.CreateBusinessObject("BM_Bill_bus");

            // STEP 1a: Set key value - BillNo (Parent Item Code)

            //bomHeader.ParentItemCode = "TEXASITEM"; //Dummy Item code for testing - replace with actual code from bomHeader.ParentItemCode when ready

            int retVal = billBus.nSetKeyValue("BillNo$", bomHeader.ParentItemCode);
            if (retVal == 0)
            {
                string errorMsg = billBus.sLastErrorMsg ?? "Unknown error";
                throw new InvalidOperationException($"nSetKeyValue BillNo$ failed for BOM {bomHeader.ParentItemCode}: {errorMsg}");
            }

            _logger.LogInformation("BOM key BillNo$ set for: {0}", bomHeader.ParentItemCode);

            // STEP 1b: Set key value - Revision (default to "000")
            retVal = billBus.nSetKeyValue("Revision$", "000");
            if (retVal == 0)
            {
                string errorMsg = billBus.sLastErrorMsg ?? "Unknown error";
                throw new InvalidOperationException($"nSetKeyValue Revision$ failed for BOM {bomHeader.ParentItemCode}: {errorMsg}");
            }

            _logger.LogInformation("BOM key Revision$ set to: 000");

            // STEP 1c: Set key (no parameters - finalizes the key)
            retVal = billBus.nSetKey();
            if (retVal == 0)
            {
                string errorMsg = billBus.sLastErrorMsg ?? "Unknown error";
                throw new InvalidOperationException($"nSetKey failed for BOM {bomHeader.ParentItemCode}: {errorMsg}");
            }

            _logger.LogInformation("BOM key finalized for: {0}", bomHeader.ParentItemCode);

            // STEP 2: Attempt to create new BOM (nNew)
            // This may not be supported on all versions, so we try and continue regardless
            try
            {
                retVal = billBus.nNew();
                _logger.LogInformation("nNew called: retVal={0}", retVal);
            }
            catch
            {
                _logger.LogInformation("nNew not supported or failed, continuing...");
            }

            // STEP 3: Set BOM header fields
            if (!string.IsNullOrWhiteSpace(bomHeader.ParentDescription))
            {
                retVal = billBus.nSetValue("BillDesc1$", bomHeader.ParentDescription);
                if (retVal == 0)
                {
                    _logger.LogWarning("Failed to set BillDesc1$: {0}", billBus.sLastErrorMsg);
                }
            }

            // Set BillType to Standard (S)
            retVal = billBus.nSetValue("BillType$", "S");
            if (retVal == 0)
            {
                _logger.LogWarning("Failed to set BillType$: {0}", billBus.sLastErrorMsg);
            }

            // STEP 4: Get Lines object from BM_Bill_bus
            // Try different possible property names
            try
            {
                oLines = billBus.oLines;
            }
            catch
            {
                try
                {
                    oLines = billBus.Lines;
                }
                catch
                {
                    _logger.LogWarning("Could not access Lines collection");
                }
            }

            if (oLines == null)
            {
                throw new InvalidOperationException("Could not get Lines collection from BM_Bill_bus");
            }

            _logger.LogInformation("Lines collection accessed successfully");

            // STEP 5: Add each BOM line
            int lineCount = 0;

            foreach (var line in bomLines)
            {
                try
                {
                    // Add new line - try different methods
                    int lineAdded = 0;
                    try
                    {
                        lineAdded = oLines.nAddLine();
                    }
                    catch
                    {
                        try
                        {
                            lineAdded = oLines.nNew();
                        }
                        catch
                        {
                            try
                            {
                                lineAdded = oLines.nNewLine();
                            }
                            catch
                            {
                                _logger.LogWarning("Could not add new line for component: {0}", line.ComponentItemCode);
                                continue;
                            }
                        }
                    }

                    if (lineAdded == 0)
                    {
                        _logger.LogWarning("Failed to add line for: {0}", line.ComponentItemCode);
                        continue;
                    }

                    // Set component item code
                    retVal = oLines.nSetValue("ComponentItemCode$", line.ComponentItemCode);
                    if (retVal == 0)
                    {
                        _logger.LogWarning("Failed to set ComponentItemCode$ for {0}: {1}", 
                            line.ComponentItemCode, oLines.sLastErrorMsg);
                        continue;
                    }

                    // Set quantity per bill
                    retVal = oLines.nSetValue("QuantityPerBill", line.Quantity);
                    if (retVal == 0)
                    {
                        _logger.LogWarning("Failed to set QuantityPerBill$ for {0}: {1}", 
                            line.ComponentItemCode, oLines.sLastErrorMsg);
                    }

                    // Set optional fields
                    if (!string.IsNullOrWhiteSpace(line.Reference))
                    {
                        oLines.nSetValue("Reference$", line.Reference);
                    }

                    if (!string.IsNullOrWhiteSpace(line.Notes))
                    {
                        oLines.nSetValue("CommentText$", line.Notes);
                    }

                    lineCount++;
                    _logger.LogInformation("Added BOM line {0}: {1} -> {2} (Qty: {3})", 
                        lineCount, bomHeader.ParentItemCode, line.ComponentItemCode, line.Quantity);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error adding BOM line for component {line.ComponentItemCode}", ex);
                }
            }

            if (lineCount == 0)
            {
                throw new InvalidOperationException("No BOM lines were successfully added");
            }

            // STEP 6: Write/Save the BOM (parent object writes all lines)
            // Try to write lines first (some systems require this)
            try
            {
                oLines.nWrite();
            }
            catch
            {
                _logger.LogInformation("oLines.nWrite not supported or failed, continuing...");
            }

            // Write the main BOM object (this saves header and all lines)
            retVal = billBus.nWrite();
            if (retVal == 0)
            {
                string errorMsg = billBus.sLastErrorMsg ?? "Unknown error";
                throw new InvalidOperationException($"nWrite failed for BOM {bomHeader.ParentItemCode}: {errorMsg}");
            }

            _logger.LogInformation("BOM written to Sage successfully: {0} with {1} lines", 
                bomHeader.ParentItemCode, lineCount);

            await Task.CompletedTask;
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error creating BOM for {bomHeader.ParentItemCode}", ex);
            return false;
        }
        finally
        {
            // Clean up objects
            if (oLines != null)
            {
                try
                {
                    oLines.DropObject();
                    Marshal.ReleaseComObject(oLines);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Error releasing Lines object: {0}", ex.Message);
                }
            }

            if (billBus != null)
            {
                try
                {
                    billBus.DropObject();
                    Marshal.ReleaseComObject(billBus);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Error releasing BOM business object: {0}", ex.Message);
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
