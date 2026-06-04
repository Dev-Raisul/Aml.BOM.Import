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
                            item.IsIntegrated = true;
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

            // Determine parent item code
            string parentItemCode;
            if (string.IsNullOrWhiteSpace(bomRecord.ParentItemCode))
            {
                // This is a parent record itself (ParentItemCode is NULL)
                parentItemCode = bomRecord.ComponentItemCode;
            }
            else
            {
                // This is a component record
                parentItemCode = bomRecord.ParentItemCode;
            }

            _logger.LogInformation("Integrating BOM for parent: {0}", parentItemCode);

            // Integrate BOM by parent item code
            return await IntegrateBomByParentAsync(parentItemCode);
        });
    }

    /// <summary>
    /// Integrates a BOM by parent item code after verifying all components are Ready
    /// </summary>
    public async Task<bool> IntegrateBomByParentAsync(string parentItemCode)
    {
        return await Task.Run(async () =>
        {
            _logger.LogInformation("Starting BOM integration for parent: {0}", parentItemCode);

            if (string.IsNullOrWhiteSpace(parentItemCode))
            {
                throw new ArgumentException("Parent item code is required", nameof(parentItemCode));
            }

            // STEP 1: Get the parent record
            var allRecords = await _bomBillRepository.GetByComponentItemCodeAsync(parentItemCode);
            var parentRecord = allRecords.FirstOrDefault(r => r.ParentItemCode == null);

            if (parentRecord == null)
            {
                _logger.LogError("Parent record not found for: {0}", null, parentItemCode);
                throw new InvalidOperationException($"Parent record not found for: {parentItemCode}");
            }

            // STEP 2: Verify parent is Ready
            if (parentRecord.Status != "Ready")
            {
                _logger.LogError("Parent {0} is not Ready. Current status: {1}", null, parentItemCode, parentRecord.Status);
                throw new InvalidOperationException($"Parent {parentItemCode} is not Ready. Current status: {parentRecord.Status}");
            }

            _logger.LogInformation("Parent {0} verified as Ready", parentItemCode);

            // STEP 3: Get all components for this parent
            var allComponents = await _bomBillRepository.GetByParentItemCodeAsync(parentItemCode);
            var componentsList = allComponents.ToList();

            if (!componentsList.Any())
            {
                _logger.LogWarning("No components found for parent: {0}", parentItemCode);
                return false;
            }

            // STEP 4: Verify ALL components are Ready
            var notReadyComponents = componentsList.Where(c => c.Status != "Ready").ToList();
            if (notReadyComponents.Any())
            {
                var notReadyList = string.Join(", ", notReadyComponents.Select(c => $"{c.ComponentItemCode} ({c.Status})"));
                _logger.LogError("Not all components are Ready for parent {0}. Not ready: {1}", 
                    null, parentItemCode, notReadyList);
                throw new InvalidOperationException(
                    $"Not all components are Ready for parent {parentItemCode}. " +
                    $"Not ready components: {notReadyList}");
            }

            _logger.LogInformation("All {0} components verified as Ready for parent: {1}", 
                componentsList.Count, parentItemCode);

            // STEP 5: Load settings
            var settings = await _settingsService.GetSettingsAsync() as Application.Models.AppSettings;
            if (settings?.SageSettings == null)
            {
                throw new InvalidOperationException("Sage settings are not configured");
            }

            SageSessionService? session = null;

            try
            {
                // STEP 6: Initialize Sage session
                session = new SageSessionService(settings.SageSettings, _logger);
                session.InitializeSession();

                // Set program context for Bill of Materials
                session.SetProgramContext("BM_Bill_ui");

                // STEP 7: Create BOM with all Ready components
                bool bomCreated = await IntegrateBomWithLinesAsync(session, parentRecord, componentsList);
                
                if (!bomCreated)
                {
                    throw new InvalidOperationException($"Failed to create BOM for {parentItemCode}");
                }

                // STEP 8: Update integration status for parent and all components
                await _bomBillRepository.UpdateStatusAsync(
                    parentRecord.Id, 
                    "Integrated", 
                    DateTime.Now, 
                    DateTime.Now);

                foreach (var component in componentsList)
                {
                    await _bomBillRepository.UpdateStatusAsync(
                        component.Id, 
                        "Integrated", 
                        DateTime.Now, 
                        DateTime.Now);
                }

                _logger.LogInformation("BOM integration complete for parent: {0}, Components: {1}", 
                    parentItemCode, componentsList.Count);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError("Fatal error during BOM integration for parent: {0}", ex, parentItemCode);
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
    private async Task<bool> IntegrateBomWithLinesAsync(SageSessionService session, BomImportBill parentRecord, List<BomImportBill> bomLines)
    {
        dynamic? billBus = null;
        dynamic? oLines = null;

        try
        {
            // Get parent item code (from parent record's ComponentItemCode)
            string parentItemCode = parentRecord.ComponentItemCode;
            
            _logger.LogInformation("Creating BOM for parent: {0} with {1} lines", parentItemCode, bomLines.Count);

            // Create BM_Bill_bus object (combines header and detail lines)
            billBus = session.CreateBusinessObject("BM_Bill_bus");

            // STEP 1a: Set key value - BillNo (Parent Item Code)
            int retVal = billBus.nSetKeyValue("BillNo$", parentItemCode);
            if (retVal == 0)
            {
                string errorMsg = billBus.sLastErrorMsg ?? "Unknown error";
                throw new InvalidOperationException($"nSetKeyValue BillNo$ failed for BOM {parentItemCode}: {errorMsg}");
            }

            _logger.LogInformation("BOM key BillNo$ set for: {0}", parentItemCode);

            // STEP 1b: Set key value - Revision (default to "000")
            retVal = billBus.nSetKeyValue("Revision$", "000");
            if (retVal == 0)
            {
                string errorMsg = billBus.sLastErrorMsg ?? "Unknown error";
                throw new InvalidOperationException($"nSetKeyValue Revision$ failed for BOM {parentItemCode}: {errorMsg}");
            }

            _logger.LogInformation("BOM key Revision$ set to: 000");

            // STEP 1c: Set key (no parameters - finalizes the key)
            retVal = billBus.nSetKey();
            if (retVal == 0)
            {
                string errorMsg = billBus.sLastErrorMsg ?? "Unknown error";
                throw new InvalidOperationException($"nSetKey failed for BOM {parentItemCode}: {errorMsg}");
            }

            _logger.LogInformation("BOM key finalized for: {0}", parentItemCode);

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
            // Use parent's description (either from ParentDescription or ComponentDescription)
            string bomDescription = !string.IsNullOrWhiteSpace(parentRecord.ParentDescription) 
                ? parentRecord.ParentDescription 
                : parentRecord.ComponentDescription ?? string.Empty;
                
            if (!string.IsNullOrWhiteSpace(bomDescription))
            {
                retVal = billBus.nSetValue("BillDesc1$", bomDescription);
                if (retVal == 0)
                {
                    _logger.LogWarning("Failed to set BillDesc1$: {0}", billBus.sLastErrorMsg);
                }
            }

            // Determine BillType based on parent's ProductType
            // If parent is Phantom (ProductType = 'P'), set BillType to 'P', otherwise 'S' (Standard)
            string billType = "S"; // Default to Standard
            string parentProductType = parentRecord.ProductType?.Trim().ToUpper() ?? "";
            
            if (parentProductType == "P")
            {
                billType = "P"; // Phantom BOM
                _logger.LogInformation("Parent item {0} is Phantom type - setting BillType to 'P'", parentItemCode);
            }
            else
            {
                _logger.LogInformation("Parent item {0} is Standard type - setting BillType to 'S'", parentItemCode);
            }

            retVal = billBus.nSetValue("BillType$", billType);
            if (retVal == 0)
            {
                _logger.LogWarning("Failed to set BillType$ to '{0}': {1}", billType, billBus.sLastErrorMsg);
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

                    // Special handling for '/C' component items - use ComponentDescription for CommentText
                    if (line.ComponentItemCode == "/C")
                    {
                        if (!string.IsNullOrWhiteSpace(line.ComponentDescription))
                        {
                            retVal = oLines.nSetValue("CommentText$", line.ComponentDescription);
                            if (retVal == 0)
                            {
                                _logger.LogWarning("Failed to set CommentText$ from ComponentDescription for /C: {0}", 
                                    oLines.sLastErrorMsg);
                            }
                            else
                            {
                                _logger.LogInformation("Set CommentText$ from ComponentDescription for /C: {0}", 
                                    line.ComponentDescription);
                            }
                        }
                    }
                    else if (!string.IsNullOrWhiteSpace(line.Notes))
                    {
                        oLines.nSetValue("CommentText$", line.Notes);
                    }

                    // Write the line after all fields are set
                    retVal = oLines.nWrite();
                    if (retVal == 0)
                    {
                        string errorMsg = oLines.sLastErrorMsg ?? "Unknown error";
                        _logger.LogWarning("Failed to write BOM line for {0}: {1}", 
                            line.ComponentItemCode, errorMsg);
                        continue;
                    }

                    lineCount++;
                    _logger.LogInformation("Added BOM line {0}: {1} -> {2} (Qty: {3})", 
                        lineCount, parentItemCode, line.ComponentItemCode, line.Quantity);
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

            // STEP 6: Write/Save the BOM header (lines already written individually)
            // Write the main BOM object (this saves the header)
            retVal = billBus.nWrite();
            if (retVal == 0)
            {
                string errorMsg = billBus.sLastErrorMsg ?? "Unknown error";
                throw new InvalidOperationException($"nWrite failed for BOM {parentItemCode}: {errorMsg}");
            }

            _logger.LogInformation("BOM written to Sage successfully: {0} with {1} lines", 
                parentItemCode, lineCount);

            await Task.CompletedTask;
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error creating BOM for parent {parentRecord.ComponentItemCode}", ex);
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

    /// <summary>
    /// Integrates multiple BOMs using a shared Sage session and BM_Bill_bus object for better performance
    /// </summary>
    public async Task<(int successCount, int failedCount, List<string> errors)> IntegrateBatchBomsAsync(IEnumerable<string> parentItemCodes)
    {
        return await Task.Run(async () =>
        {
            var parentList = parentItemCodes.ToList();
            _logger.LogInformation("Starting batch BOM integration for {0} parent items", parentList.Count);

            if (!parentList.Any())
            {
                _logger.LogWarning("No parent items to integrate");
                return (0, 0, new List<string>());
            }

            // Load settings
            var settings = await _settingsService.GetSettingsAsync() as Application.Models.AppSettings;
            if (settings?.SageSettings == null)
            {
                _logger.LogError("Sage settings not configured", null);
                throw new InvalidOperationException("Sage settings are not configured. Please configure Sage settings first.");
            }

            SageSessionService? session = null;
            dynamic? billBus = null;
            int successCount = 0;
            int failedCount = 0;
            var errors = new List<string>();

            try
            {
                // Initialize Sage session ONCE for all BOMs
                _logger.LogInformation("Initializing shared Sage session for batch integration");
                session = new SageSessionService(settings.SageSettings, _logger);
                session.InitializeSession();

                // Set program context for Bill of Materials
                session.SetProgramContext("BM_Bill_ui");

                // Create BM_Bill_bus object ONCE for all BOMs
                _logger.LogInformation("Creating shared BM_Bill_bus object for batch integration");
                billBus = session.CreateBusinessObject("BM_Bill_bus");

                _logger.LogInformation("Sage session and Bill bus initialized successfully - processing {0} BOMs", parentList.Count);

                // Process each BOM using the SHARED session and bill bus
                foreach (var parentItemCode in parentList)
                {
                    try
                    {
                        _logger.LogInformation("Integrating BOM {0} of {1}: {2}", 
                            successCount + failedCount + 1, parentList.Count, parentItemCode);

                        // Integrate using shared session and bill bus
                        bool success = await IntegrateBomWithSharedBusAsync(billBus, parentItemCode);
                        
                        if (success)
                        {
                            successCount++;
                            _logger.LogInformation("BOM integrated successfully: {0} (Total: {1}/{2})", 
                                parentItemCode, successCount, parentList.Count);
                        }
                        else
                        {
                            failedCount++;
                            errors.Add($"{parentItemCode}: Integration failed");
                            _logger.LogWarning("BOM integration failed: {0}", parentItemCode);
                        }
                    }
                    catch (Exception ex)
                    {
                        failedCount++;
                        errors.Add($"{parentItemCode}: {ex.Message}");
                        _logger.LogError($"Failed to integrate BOM for parent {parentItemCode}", ex);
                    }
                }

                _logger.LogInformation("Batch BOM integration complete: {0} succeeded, {1} failed out of {2} total", 
                    successCount, failedCount, parentList.Count);

                return (successCount, failedCount, errors);
            }
            catch (Exception ex)
            {
                _logger.LogError("Fatal error during batch BOM integration", ex);
                throw;
            }
            finally
            {
                // Dispose bill bus ONCE after all integrations are complete
                if (billBus != null)
                {
                    try
                    {
                        _logger.LogInformation("Disposing shared BM_Bill_bus object after batch integration");
                        billBus.DropObject();
                        Marshal.ReleaseComObject(billBus);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("Error releasing shared bill bus object: {0}", ex.Message);
                    }
                }

                // Dispose session ONCE after all integrations are complete
                if (session != null)
                {
                    _logger.LogInformation("Disposing shared Sage session after batch integration");
                    session.Dispose();
                }
            }
        });
    }

    /// <summary>
    /// Integrates a BOM using an existing shared Sage session (for batch operations)
    /// </summary>
    private async Task<bool> IntegrateBomWithSharedSessionAsync(SageSessionService session, string parentItemCode)
    {
        try
        {
            _logger.LogInformation("Integrating BOM for parent: {0} using shared session", parentItemCode);

            if (string.IsNullOrWhiteSpace(parentItemCode))
            {
                throw new ArgumentException("Parent item code is required", nameof(parentItemCode));
            }

            // STEP 1: Get the parent record
            var allRecords = await _bomBillRepository.GetByComponentItemCodeAsync(parentItemCode);
            var parentRecord = allRecords.FirstOrDefault(r => r.ParentItemCode == null);

            if (parentRecord == null)
            {
                _logger.LogError("Parent record not found for: {0}", null, parentItemCode);
                throw new InvalidOperationException($"Parent record not found for: {parentItemCode}");
            }

            // STEP 2: Verify parent is Ready
            if (parentRecord.Status != "Ready")
            {
                _logger.LogError("Parent {0} is not Ready. Current status: {1}", null, parentItemCode, parentRecord.Status);
                throw new InvalidOperationException($"Parent {parentItemCode} is not Ready. Current status: {parentRecord.Status}");
            }

            // STEP 3: Get all components for this parent
            var allComponents = await _bomBillRepository.GetByParentItemCodeAsync(parentItemCode);
            var componentsList = allComponents.ToList();

            if (!componentsList.Any())
            {
                _logger.LogWarning("No components found for parent: {0}", parentItemCode);
                return false;
            }

            // STEP 4: Verify ALL components are Ready
            var notReadyComponents = componentsList.Where(c => c.Status != "Ready").ToList();
            if (notReadyComponents.Any())
            {
                var notReadyList = string.Join(", ", notReadyComponents.Select(c => $"{c.ComponentItemCode} ({c.Status})"));
                _logger.LogError("Not all components are Ready for parent {0}. Not ready: {1}", 
                    null, parentItemCode, notReadyList);
                throw new InvalidOperationException(
                    $"Not all components are Ready for parent {parentItemCode}. " +
                    $"Not ready components: {notReadyList}");
            }

            _logger.LogInformation("All {0} components verified as Ready for parent: {1}", 
                componentsList.Count, parentItemCode);

            // STEP 5: Create BOM using shared session (creates new billBus for this single BOM)
            bool bomCreated = await IntegrateBomWithLinesAsync(session, parentRecord, componentsList);
            
            if (!bomCreated)
            {
                throw new InvalidOperationException($"Failed to create BOM for {parentItemCode}");
            }

            // STEP 6: Update integration status for parent and all components
            await _bomBillRepository.UpdateStatusAsync(
                parentRecord.Id, 
                "Integrated", 
                DateTime.Now, 
                DateTime.Now);

            foreach (var component in componentsList)
            {
                await _bomBillRepository.UpdateStatusAsync(
                    component.Id, 
                    "Integrated", 
                    DateTime.Now, 
                    DateTime.Now);
            }

            _logger.LogInformation("BOM integration complete for parent: {0}, Components: {1}", 
                parentItemCode, componentsList.Count);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError("Error during BOM integration for parent: {0}", ex, parentItemCode);
            throw;
        }
    }

    /// <summary>
    /// Integrates a BOM using an existing shared BM_Bill_bus object (for batch operations with reused bus)
    /// </summary>
    private async Task<bool> IntegrateBomWithSharedBusAsync(dynamic billBus, string parentItemCode)
    {
        dynamic? oLines = null;

        try
        {
            _logger.LogInformation("Integrating BOM for parent: {0} using shared bill bus", parentItemCode);

            if (string.IsNullOrWhiteSpace(parentItemCode))
            {
                throw new ArgumentException("Parent item code is required", nameof(parentItemCode));
            }

            // STEP 1: Get the parent record
            var allRecords = await _bomBillRepository.GetByComponentItemCodeAsync(parentItemCode);
            var parentRecord = allRecords.FirstOrDefault(r => r.ParentItemCode == null);

            if (parentRecord == null)
            {
                _logger.LogError("Parent record not found for: {0}", null, parentItemCode);
                throw new InvalidOperationException($"Parent record not found for: {parentItemCode}");
            }

            // STEP 2: Verify parent is Ready
            if (parentRecord.Status != "Ready")
            {
                _logger.LogError("Parent {0} is not Ready. Current status: {1}", null, parentItemCode, parentRecord.Status);
                throw new InvalidOperationException($"Parent {parentItemCode} is not Ready. Current status: {parentRecord.Status}");
            }

            // STEP 3: Get all components for this parent
            var allComponents = await _bomBillRepository.GetByParentItemCodeAsync(parentItemCode);
            var componentsList = allComponents.ToList();

            if (!componentsList.Any())
            {
                _logger.LogWarning("No components found for parent: {0}", parentItemCode);
                return false;
            }

            // STEP 4: Verify ALL components are Ready
            var notReadyComponents = componentsList.Where(c => c.Status != "Ready").ToList();
            if (notReadyComponents.Any())
            {
                var notReadyList = string.Join(", ", notReadyComponents.Select(c => $"{c.ComponentItemCode} ({c.Status})"));
                _logger.LogError("Not all components are Ready for parent {0}. Not ready: {1}", 
                    null, parentItemCode, notReadyList);
                throw new InvalidOperationException(
                    $"Not all components are Ready for parent {parentItemCode}. " +
                    $"Not ready components: {notReadyList}");
            }

            _logger.LogInformation("All {0} components verified as Ready for parent: {1}", 
                componentsList.Count, parentItemCode);

            // STEP 5: Create BOM using shared bill bus object
            bool bomCreated = await IntegrateBomWithLinesUsingSharedBusAsync(billBus, parentRecord, componentsList);
            
            if (!bomCreated)
            {
                throw new InvalidOperationException($"Failed to create BOM for {parentItemCode}");
            }

            // STEP 6: Update integration status for parent and all components
            await _bomBillRepository.UpdateStatusAsync(
                parentRecord.Id, 
                "Integrated", 
                DateTime.Now, 
                DateTime.Now);

            foreach (var component in componentsList)
            {
                await _bomBillRepository.UpdateStatusAsync(
                    component.Id, 
                    "Integrated", 
                    DateTime.Now, 
                    DateTime.Now);
            }

            _logger.LogInformation("BOM integration complete for parent: {0}, Components: {1}", 
                parentItemCode, componentsList.Count);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError("Error during BOM integration for parent: {0}", ex, parentItemCode);
            return false;
        }
    }

    /// <summary>
    /// Integrates a complete BOM (header + lines) using a shared BM_Bill_bus object
    /// </summary>
    private async Task<bool> IntegrateBomWithLinesUsingSharedBusAsync(dynamic billBus, BomImportBill parentRecord, List<BomImportBill> bomLines)
    {
        dynamic? oLines = null;

        try
        {
            // Get parent item code (from parent record's ComponentItemCode)
            string parentItemCode = parentRecord.ComponentItemCode;
            
            _logger.LogInformation("Creating BOM for parent: {0} with {1} lines using shared bus", parentItemCode, bomLines.Count);

            // STEP 1a: Set key value - BillNo (Parent Item Code)
            int retVal = billBus.nSetKeyValue("BillNo$", parentItemCode);
            if (retVal == 0)
            {
                string errorMsg = billBus.sLastErrorMsg ?? "Unknown error";
                throw new InvalidOperationException($"nSetKeyValue BillNo$ failed for BOM {parentItemCode}: {errorMsg}");
            }

            _logger.LogInformation("BOM key BillNo$ set for: {0}", parentItemCode);

            // STEP 1b: Set key value - Revision (default to "000")
            retVal = billBus.nSetKeyValue("Revision$", "000");
            if (retVal == 0)
            {
                string errorMsg = billBus.sLastErrorMsg ?? "Unknown error";
                throw new InvalidOperationException($"nSetKeyValue Revision$ failed for BOM {parentItemCode}: {errorMsg}");
            }

            _logger.LogInformation("BOM key Revision$ set to: 000");

            // STEP 1c: Set key (no parameters - finalizes the key)
            retVal = billBus.nSetKey();
            if (retVal == 0)
            {
                string errorMsg = billBus.sLastErrorMsg ?? "Unknown error";
                throw new InvalidOperationException($"nSetKey failed for BOM {parentItemCode}: {errorMsg}");
            }

            _logger.LogInformation("BOM key finalized for: {0}", parentItemCode);

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
            // Use parent's description (either from ParentDescription or ComponentDescription)
            string bomDescription = !string.IsNullOrWhiteSpace(parentRecord.ParentDescription) 
                ? parentRecord.ParentDescription 
                : parentRecord.ComponentDescription ?? string.Empty;
                
            if (!string.IsNullOrWhiteSpace(bomDescription))
            {
                retVal = billBus.nSetValue("BillDesc1$", bomDescription);
                if (retVal == 0)
                {
                    _logger.LogWarning("Failed to set BillDesc1$: {0}", billBus.sLastErrorMsg);
                }
            }

            // Determine BillType based on parent's ProductType
            // If parent is Phantom (ProductType = 'P'), set BillType to 'P', otherwise 'S' (Standard)
            string billType = "S"; // Default to Standard
            string parentProductType = parentRecord.ProductType?.Trim().ToUpper() ?? "";
            
            if (parentProductType == "P")
            {
                billType = "P"; // Phantom BOM
                _logger.LogInformation("Parent item {0} is Phantom type - setting BillType to 'P'", parentItemCode);
            }
            else
            {
                _logger.LogInformation("Parent item {0} is Standard type - setting BillType to 'S'", parentItemCode);
            }

            retVal = billBus.nSetValue("BillType$", billType);
            if (retVal == 0)
            {
                _logger.LogWarning("Failed to set BillType$ to '{0}': {1}", billType, billBus.sLastErrorMsg);
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

                    // Special handling for '/C' component items - use ComponentDescription for CommentText
                    if (line.ComponentItemCode == "/C")
                    {
                        if (!string.IsNullOrWhiteSpace(line.ComponentDescription))
                        {
                            retVal = oLines.nSetValue("CommentText$", line.ComponentDescription);
                            if (retVal == 0)
                            {
                                _logger.LogWarning("Failed to set CommentText$ from ComponentDescription for /C: {0}", 
                                    oLines.sLastErrorMsg);
                            }
                            else
                            {
                                _logger.LogInformation("Set CommentText$ from ComponentDescription for /C: {0}", 
                                    line.ComponentDescription);
                            }
                        }
                    }
                    else if (!string.IsNullOrWhiteSpace(line.Notes))
                    {
                        oLines.nSetValue("CommentText$", line.Notes);
                    }

                    // Write the line after all fields are set
                    retVal = oLines.nWrite();
                    if (retVal == 0)
                    {
                        string errorMsg = oLines.sLastErrorMsg ?? "Unknown error";
                        _logger.LogWarning("Failed to write BOM line for {0}: {1}", 
                            line.ComponentItemCode, errorMsg);
                        continue;
                    }

                    lineCount++;
                    _logger.LogInformation("Added BOM line {0}: {1} -> {2} (Qty: {3})", 
                        lineCount, parentItemCode, line.ComponentItemCode, line.Quantity);
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

            // STEP 6: Write/Save the BOM header (lines already written individually)
            // Write the main BOM object (this saves the header)
            retVal = billBus.nWrite();
            if (retVal == 0)
            {
                string errorMsg = billBus.sLastErrorMsg ?? "Unknown error";
                throw new InvalidOperationException($"nWrite failed for BOM {parentItemCode}: {errorMsg}");
            }

            _logger.LogInformation("BOM written to Sage successfully: {0} with {1} lines (bill bus reused)", 
                parentItemCode, lineCount);

            await Task.CompletedTask;
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error creating BOM for parent {parentRecord.ComponentItemCode}", ex);
            return false;
        }
        finally
        {
            // Clean up Lines object only (NOT the billBus - it will be reused)
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

            // DO NOT dispose billBus here - it's shared and will be disposed by the caller
        }
    }
}
