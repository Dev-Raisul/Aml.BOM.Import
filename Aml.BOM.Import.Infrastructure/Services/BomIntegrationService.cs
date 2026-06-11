using System.Runtime.InteropServices;
using Aml.BOM.Import.Domain.Entities;
using Aml.BOM.Import.Shared.Interfaces;
using Microsoft.Data.SqlClient;

namespace Aml.BOM.Import.Infrastructure.Services;

public class BomIntegrationService : IBomIntegrationService
{
    private readonly INewMakeItemRepository _makeItemRepository;
    private readonly IBomImportBillRepository _bomBillRepository;
    private readonly ISettingsService _settingsService;
    private readonly ILoggerService _logger;
    private readonly SharedSageSessionService _sharedSession;

    public BomIntegrationService(
        INewMakeItemRepository makeItemRepository,
        IBomImportBillRepository bomBillRepository,
        ISettingsService settingsService,
        ILoggerService logger,
        SharedSageSessionService sharedSession)
    {
        _makeItemRepository = makeItemRepository;
        _bomBillRepository = bomBillRepository;
        _settingsService = settingsService;
        _logger = logger;
        _sharedSession = sharedSession;
    }

    /// <summary>
    /// Integrates new make items into Sage 100 using CI_ItemCode_bus with shared Sage session
    /// </summary>
    public async Task<bool> IntegrateNewItemsAsync(IEnumerable<object> items)
    {
        return await Task.Run(async () =>
        {
            var itemsList = items.Cast<NewMakeItem>().ToList();
            _logger.LogInformation("Starting integration of {0} new make items using shared Sage session", itemsList.Count);

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

            dynamic? itemBus = null;
            int successCount = 0;
            int failedCount = 0;
            var errors = new List<string>();

            try
            {
                // Ensure shared session is initialized
                _logger.LogInformation("Ensuring shared Sage session is initialized for item integration");
                _sharedSession.EnsureInitialized();

                // Set program context for Item Maintenance
                _sharedSession.SetProgramContext("CI_ItemCode_ui");

                // Create CI_ItemCode_bus object ONCE for all items using shared session
                _logger.LogInformation("Creating shared CI_ItemCode_bus object for batch integration");
                itemBus = _sharedSession.CreateBusinessObject("CI_ItemCode_bus");

                _logger.LogInformation("Shared Sage session and Item bus initialized successfully - processing {0} items", itemsList.Count);

                // Process each item using the SHARED item bus object
                foreach (var item in itemsList)
                {
                    try
                    {
                        _logger.LogInformation("Integrating item {0} of {1}: {2} - {3}", 
                            successCount + failedCount + 1, itemsList.Count, item.ItemCode, item.ItemDescription);

                        // Validate required fields
                        if (string.IsNullOrWhiteSpace(item.ProductLine))
                        {
                            errors.Add($"{item.ItemCode}: Product Line is required");
                            failedCount++;
                            continue;
                        }

                        // Create item in Sage using shared item bus and current in-memory values
                        bool success = await IntegrateSingleItemWithSharedBusAsync(itemBus, item);
                        
                        if (success)
                        {
                            // Mark as integrated in database
                            await _makeItemRepository.MarkAsIntegratedAsync(item.ItemCode, item.ImportFileName);
                            item.IsIntegrated = true;
                            successCount++;
                            _logger.LogInformation("Item integrated successfully: {0} (Total: {1}/{2})", 
                                item.ItemCode, successCount, itemsList.Count);
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

                _logger.LogInformation("Item integration complete: {0} succeeded, {1} failed out of {2} total", 
                    successCount, failedCount, itemsList.Count);

                if (errors.Any())
                {
                    _logger.LogWarning("Integration errors: {0}", string.Join("; ", errors));
                }

                // Update CI_Item UDF fields for all successfully integrated items
                if (successCount > 0)
                {
                    _logger.LogInformation("Updating CI_Item UDF fields for {0} successfully integrated items", successCount);
                    var successfulItems = itemsList.Where(i => i.IsIntegrated).ToList();
                    await UpdateCIItemUDFFieldsBatchAsync(successfulItems, settings.DatabaseConnectionString);
                    
                    // Mark newly created items as Validated in isBOMImportBills
                    _logger.LogInformation("Marking {0} newly created items as Validated in isBOMImportBills", successCount);
                    await MarkNewItemsAsValidatedAsync(successfulItems, settings.DatabaseConnectionString);
                    
                    // Run Ready-to-Integrate check for affected BOMs
                    _logger.LogInformation("Running Ready-to-Integrate check for BOMs containing newly created items");
                    await UpdateReadyStatusForAffectedBomsAsync(successfulItems, settings.DatabaseConnectionString);
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
                // Dispose item bus ONLY (NOT the shared session!)
                if (itemBus != null)
                {
                    try
                    {
                        _logger.LogInformation("Disposing CI_ItemCode_bus object (shared session remains active)");
                        itemBus.DropObject();
                        Marshal.ReleaseComObject(itemBus);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("Error releasing shared item bus object: {0}", ex.Message);
                    }
                }

                // DO NOT dispose shared session - it stays alive for application lifetime
                _logger.LogInformation("Shared Sage session remains active for future operations");
            }
        });
    }

    /// <summary>
    /// Integrates a single make item into Sage (legacy method - creates its own bus object)
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
    /// Integrates a single make item into Sage using a shared CI_ItemCode_bus object (for batch operations)
    /// </summary>
    private async Task<bool> IntegrateSingleItemWithSharedBusAsync(dynamic itemBus, NewMakeItem item)
    {
        try
        {
            _logger.LogInformation("Integrating item using shared bus: {0} - {1}", item.ItemCode, item.ItemDescription);

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

            _logger.LogInformation("Item written to Sage successfully: {0} (item bus reused)", item.ItemCode);
            await Task.CompletedTask;
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error integrating item {item.ItemCode}", ex);
            return false;
        }
        // DO NOT dispose itemBus here - it's shared and will be disposed by the caller
    }

    /// <summary>
    /// Updates CI_Item UDF fields via SQL for all successfully integrated items (batch operation)
    /// Uses a single UPDATE with JOIN to update all items at once
    /// Maps NewMakeItem boolean fields to Sage UDF Y/N string fields
    /// </summary>
    private async Task UpdateCIItemUDFFieldsBatchAsync(List<NewMakeItem> items, string connectionString)
    {
        if (!items.Any())
        {
            _logger.LogInformation("No items to update UDF fields for");
            return;
        }

        try
        {
            _logger.LogInformation("Updating CI_Item UDF fields for {0} items using batch JOIN update", items.Count);

            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            // Single UPDATE statement with JOIN - updates all items at once
            const string sql = @"
                UPDATE ci
                SET ci.UDF_COATED = CASE WHEN nmi.Coated = 1 THEN 'Y' ELSE 'N' END,
                    ci.UDF_STAGED_ITEM = CASE WHEN nmi.StagedItem = 1 THEN 'Y' ELSE 'N' END,
                    ci.UDF_SUB_PRODUCT_FAMILY = nmi.SubProductFamily
                FROM CI_Item ci
                INNER JOIN isBOMImport_NewMakeItems nmi ON ci.ItemCode = nmi.ItemCode
                WHERE nmi.IsIntegrated = 1";

            using var command = new SqlCommand(sql, connection);
            command.CommandTimeout = 300; // 5 minutes timeout for large batches

            int rowsAffected = await command.ExecuteNonQueryAsync();
            
            _logger.LogInformation("CI_Item UDF fields batch update complete: {0} rows updated", rowsAffected);

            if (rowsAffected != items.Count)
            {
                _logger.LogWarning("Expected to update {0} items but updated {1} items. Some items may not exist in CI_Item table.",
                    items.Count, rowsAffected);
            }
        }
        catch (Exception ex)
        {
            // Log error but don't fail the integration - items were already created successfully
            _logger.LogError("Fatal error updating CI_Item UDF fields batch", ex);
        }
    }

    /// <summary>
    /// Marks newly created items as Validated in isBOMImportBills table
    /// Updates all records where ComponentItemCode matches the newly created items
    /// </summary>
    private async Task MarkNewItemsAsValidatedAsync(List<NewMakeItem> items, string connectionString)
    {
        if (!items.Any())
        {
            _logger.LogInformation("No items to mark as Validated");
            return;
        }

        try
        {
            _logger.LogInformation("Marking {0} newly created items as Validated in isBOMImportBills", items.Count);

            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            // Update all BOM records where ComponentItemCode is one of the newly created items
            // Change Status from 'NewMakeItem' to 'Validated'
            const string sql = @"
                UPDATE ib
                SET ib.Status = 'Validated',
                    ib.DateValidated = GETDATE(),
                    ib.ValidationMessage = 'Item created and integrated to Sage successfully',
                    ib.ItemExists = 1
                FROM isBOMImportBills ib
                INNER JOIN isBOMImport_NewMakeItems nmi ON ib.ComponentItemCode = nmi.ItemCode
                WHERE nmi.IsIntegrated = 1
                  AND ib.Status = 'NewMakeItem'";

            using var command = new SqlCommand(sql, connection);
            command.CommandTimeout = 300;

            int rowsAffected = await command.ExecuteNonQueryAsync();
            
            _logger.LogInformation("Marked {0} BOM component records as Validated (newly created items now exist in Sage)", rowsAffected);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error marking new items as Validated in isBOMImportBills", ex);
        }
    }

    /// <summary>
    /// Updates Ready status for BOMs that contain the newly created items
    /// If all components of a BOM are now Validated, marks the entire BOM as Ready
    /// </summary>
    private async Task UpdateReadyStatusForAffectedBomsAsync(List<NewMakeItem> items, string connectionString)
    {
        if (!items.Any())
        {
            _logger.LogInformation("No items to check for Ready status updates");
            return;
        }

        try
        {
            _logger.LogInformation("Checking Ready-to-Integrate status for BOMs containing newly created items");

            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            // Step 1: Get all distinct parent items that contain the newly created components
            const string getParentsSql = @"
                SELECT DISTINCT ib.ParentItemCode
                FROM isBOMImportBills ib
                INNER JOIN isBOMImport_NewMakeItems nmi ON ib.ComponentItemCode = nmi.ItemCode
                WHERE nmi.IsIntegrated = 1
                  AND ib.ParentItemCode IS NOT NULL";

            var affectedParents = new List<string>();
            using (var getParentsCmd = new SqlCommand(getParentsSql, connection))
            {
                using var reader = await getParentsCmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    affectedParents.Add(reader.GetString(0));
                }
            }

            if (!affectedParents.Any())
            {
                _logger.LogInformation("No parent BOMs found containing the newly created items");
                return;
            }

            _logger.LogInformation("Found {0} parent BOMs potentially affected by newly created items", affectedParents.Count);

            // Step 2: Update Ready status for each affected parent BOM
            // A BOM is Ready if:
            // - Parent record Status = 'Validated' (parent exists in Sage)
            // - ALL component records Status = 'Validated' (all components exist in Sage)
            const string updateReadySql = @"
                -- Update parent and all components to Ready if ALL components are Validated
                UPDATE ib
                SET ib.Status = 'Ready',
                    ib.DateValidated = GETDATE(),
                    ib.ValidationMessage = 'BOM ready for integration - all components validated'
                FROM isBOMImportBills ib
                WHERE ib.ParentItemCode = @ParentItemCode
                  AND ib.Status = 'Validated'
                  AND NOT EXISTS (
                      -- Check if any component is NOT Validated
                      SELECT 1 
                      FROM isBOMImportBills component
                      WHERE component.ParentItemCode = @ParentItemCode
                        AND component.Status != 'Validated'
                        AND component.Status != 'Ready'
                        AND component.Status != 'Integrated'
                  )
                  -- Also check parent record is Validated
                  AND EXISTS (
                      SELECT 1
                      FROM isBOMImportBills parent
                      WHERE parent.ComponentItemCode = @ParentItemCode
                        AND parent.ParentItemCode IS NULL
                        AND parent.Status = 'Validated'
                  )";

            int totalUpdated = 0;
            foreach (var parentItemCode in affectedParents)
            {
                using var updateCmd = new SqlCommand(updateReadySql, connection);
                updateCmd.Parameters.AddWithValue("@ParentItemCode", parentItemCode);
                updateCmd.CommandTimeout = 60;

                int rowsAffected = await updateCmd.ExecuteNonQueryAsync();
                if (rowsAffected > 0)
                {
                    totalUpdated += rowsAffected;
                    _logger.LogInformation("BOM {0} marked as Ready: {1} records updated", parentItemCode, rowsAffected);
                }
            }

            _logger.LogInformation("Ready-to-Integrate status update complete: {0} total records marked as Ready across {1} BOMs", 
                totalUpdated, affectedParents.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error updating Ready status for affected BOMs", ex);
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

            try
            {
                // STEP 6: Ensure shared Sage session is initialized
                _logger.LogInformation("Ensuring shared Sage session is initialized for BOM integration");
                _sharedSession.EnsureInitialized();

                // Set program context for Bill of Materials
                _sharedSession.SetProgramContext("BM_Bill_ui");

                // STEP 7: Create BOM with all Ready components using shared session
                bool bomCreated = await IntegrateBomWithLinesUsingSharedSessionAsync(parentRecord, componentsList);
                
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
            // DO NOT dispose shared session - it stays alive for application lifetime
            // finally block removed - shared session remains active
        });
    }

    /// <summary>
    /// Integrates a complete BOM (header + lines) using shared Sage session
    /// </summary>
    private async Task<bool> IntegrateBomWithLinesUsingSharedSessionAsync(BomImportBill parentRecord, List<BomImportBill> bomLines)
    {
        dynamic? billBus = null;
        dynamic? oLines = null;

        try
        {
            // Get parent item code (from parent record's ComponentItemCode)
            string parentItemCode = parentRecord.ComponentItemCode;
            
            _logger.LogInformation("Creating BOM for parent: {0} with {1} lines using shared session", parentItemCode, bomLines.Count);

            // Create BM_Bill_bus object using shared session
            billBus = _sharedSession.CreateBusinessObject("BM_Bill_bus");

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
                // Split description into two parts (max 30 characters each for 60 total)
                string billDesc1 = bomDescription.Length <= 30 ? bomDescription : bomDescription.Substring(0, 30);
                string billDesc2 = bomDescription.Length > 30 ? bomDescription.Substring(30, Math.Min(30, bomDescription.Length - 30)) : string.Empty;

                retVal = billBus.nSetValue("BillDesc1$", billDesc1);
                if (retVal == 0)
                {
                    _logger.LogWarning("Failed to set BillDesc1$: {0}", billBus.sLastErrorMsg);
                }
                else
                {
                    _logger.LogInformation("Set BillDesc1$: {0}", billDesc1);
                }

                if (!string.IsNullOrWhiteSpace(billDesc2))
                {
                    retVal = billBus.nSetValue("BillDesc2$", billDesc2);
                    if (retVal == 0)
                    {
                        _logger.LogWarning("Failed to set BillDesc2$: {0}", billBus.sLastErrorMsg);
                    }
                    else
                    {
                        _logger.LogInformation("Set BillDesc2$: {0}", billDesc2);
                    }
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

            _logger.LogInformation("BOM written to Sage successfully: {0} with {1} lines (using shared session)", 
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

            // DO NOT dispose shared session - it stays alive for application lifetime
        }
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
                // Split description into two parts (max 30 characters each for 60 total)
                string billDesc1 = bomDescription.Length <= 30 ? bomDescription : bomDescription.Substring(0, 30);
                string billDesc2 = bomDescription.Length > 30 ? bomDescription.Substring(30, Math.Min(30, bomDescription.Length - 30)) : string.Empty;

                retVal = billBus.nSetValue("BillDesc1$", billDesc1);
                if (retVal == 0)
                {
                    _logger.LogWarning("Failed to set BillDesc1$: {0}", billBus.sLastErrorMsg);
                }
                else
                {
                    _logger.LogInformation("Set BillDesc1$: {0}", billDesc1);
                }

                if (!string.IsNullOrWhiteSpace(billDesc2))
                {
                    retVal = billBus.nSetValue("BillDesc2$", billDesc2);
                    if (retVal == 0)
                    {
                        _logger.LogWarning("Failed to set BillDesc2$: {0}", billBus.sLastErrorMsg);
                    }
                    else
                    {
                        _logger.LogInformation("Set BillDesc2$: {0}", billDesc2);
                    }
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
    /// Integrates multiple BOMs using shared Sage session for better performance
    /// </summary>
    public async Task<(int successCount, int failedCount, List<string> errors)> IntegrateBatchBomsAsync(IEnumerable<string> parentItemCodes)
    {
        return await Task.Run(async () =>
        {
            var parentList = parentItemCodes.ToList();
            _logger.LogInformation("Starting batch BOM integration for {0} parent items using shared Sage session", parentList.Count);

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

            dynamic? billBus = null;
            int successCount = 0;
            int failedCount = 0;
            var errors = new List<string>();

            try
            {
                // Ensure shared session is initialized
                _logger.LogInformation("Ensuring shared Sage session is initialized for batch BOM integration");
                _sharedSession.EnsureInitialized();

                // Switch to B/M module if needed
                _sharedSession.SwitchModule("B/M");

                // Set program context for Bill of Materials
                _sharedSession.SetProgramContext("BM_Bill_ui");

                // Create BM_Bill_bus object ONCE for all BOMs using shared session
                _logger.LogInformation("Creating shared BM_Bill_bus object for batch integration");
                billBus = _sharedSession.CreateBusinessObject("BM_Bill_bus");

                _logger.LogInformation("Shared Sage session and Bill bus initialized successfully - processing {0} BOMs", parentList.Count);

                // Process each BOM using the SHARED bill bus object
                foreach (var parentItemCode in parentList)
                {
                    try
                    {
                        _logger.LogInformation("Integrating BOM {0} of {1}: {2}", 
                            successCount + failedCount + 1, parentList.Count, parentItemCode);

                        // Integrate using shared bill bus
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
                // Dispose bill bus ONLY (NOT the shared session!)
                if (billBus != null)
                {
                    try
                    {
                        _logger.LogInformation("Disposing BM_Bill_bus object (shared session remains active)");
                        billBus.DropObject();
                        Marshal.ReleaseComObject(billBus);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("Error releasing shared bill bus object: {0}", ex.Message);
                    }
                }

                // DO NOT dispose shared session - it stays alive for application lifetime
                _logger.LogInformation("Shared Sage session remains active for future operations");
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
                // Split description into two parts (max 30 characters each for 60 total)
                string billDesc1 = bomDescription.Length <= 30 ? bomDescription : bomDescription.Substring(0, 30);
                string billDesc2 = bomDescription.Length > 30 ? bomDescription.Substring(30, Math.Min(30, bomDescription.Length - 30)) : string.Empty;

                retVal = billBus.nSetValue("BillDesc1$", billDesc1);
                if (retVal == 0)
                {
                    _logger.LogWarning("Failed to set BillDesc1$: {0}", billBus.sLastErrorMsg);
                }
                else
                {
                    _logger.LogInformation("Set BillDesc1$: {0}", billDesc1);
                }

                if (!string.IsNullOrWhiteSpace(billDesc2))
                {
                    retVal = billBus.nSetValue("BillDesc2$", billDesc2);
                    if (retVal == 0)
                    {
                        _logger.LogWarning("Failed to set BillDesc2$: {0}", billBus.sLastErrorMsg);
                    }
                    else
                    {
                        _logger.LogInformation("Set BillDesc2$: {0}", billDesc2);
                    }
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
