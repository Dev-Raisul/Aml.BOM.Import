using System.Runtime.InteropServices;
using Aml.BOM.Import.Application.Models;
using Aml.BOM.Import.Shared.Interfaces;

namespace Aml.BOM.Import.Infrastructure.Services;

/// <summary>
/// Manages Sage 100 ProvideX session using COM interop
/// Follows the exact initialization sequence from the VBS reference script
/// </summary>
public class SageSessionService : IDisposable
{
    private readonly ILoggerService _logger;
    private readonly SageSettings _settings;
    private dynamic? _providex;
    private dynamic? _session;
    private bool _disposed;
    private bool _isInitialized;

    public SageSessionService(SageSettings settings, ILoggerService logger)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets the active Sage session object
    /// </summary>
    public dynamic Session
    {
        get
        {
            if (!_isInitialized || _session == null)
                throw new InvalidOperationException("Sage session is not initialized. Call InitializeSession() first.");
            return _session;
        }
    }

    /// <summary>
    /// Gets the ProvideX script object
    /// </summary>
    public dynamic ProvideX
    {
        get
        {
            if (_providex == null)
                throw new InvalidOperationException("ProvideX is not initialized.");
            return _providex;
        }
    }

    /// <summary>
    /// Initializes the Sage 100 session following the exact VBS script sequence
    /// </summary>
    public void InitializeSession()
    {
        if (_isInitialized)
        {
            _logger.LogWarning("Sage session already initialized");
            return;
        }

        try
        {
            _logger.LogInformation("=== Starting Sage 100 Session Initialization ===");

            // STEP 1: Create ProvideX.Script object
            _logger.LogInformation("[STEP 1] Creating ProvideX.Script COM object");
            Type? providexType = Type.GetTypeFromProgID("ProvideX.Script");
            if (providexType == null)
            {
                throw new InvalidOperationException(
                    "Failed to create ProvideX.Script COM object. " +
                    "Ensure Sage 100 is installed and ProvideX is registered.");
            }

            _providex = Activator.CreateInstance(providexType);
            if (_providex == null)
            {
                throw new InvalidOperationException("Failed to instantiate ProvideX.Script object.");
            }
            _logger.LogInformation("[STEP 1] ProvideX.Script created successfully");

            // STEP 2: Initialize ProvideX with Sage path
            _logger.LogInformation("[STEP 2] Initializing ProvideX with path: {0}", _settings.SagePath);
            if (string.IsNullOrWhiteSpace(_settings.SagePath))
            {
                throw new InvalidOperationException("Sage path is not configured in settings.");
            }

            if (!Directory.Exists(_settings.SagePath))
            {
                throw new DirectoryNotFoundException(
                    $"Sage path not found: {_settings.SagePath}. " +
                    $"Please verify Sage 100 installation path in settings.");
            }

            _providex.Init(_settings.SagePath);
            _logger.LogInformation("[STEP 2] ProvideX initialized successfully");

            // STEP 3: Create SY_Session object
            _logger.LogInformation("[STEP 3] Creating SY_Session object");
            _session = _providex.NewObject("SY_Session");
            if (_session == null)
            {
                throw new InvalidOperationException("Failed to create SY_Session object.");
            }
            _logger.LogInformation("[STEP 3] SY_Session created successfully");

            // STEP 4: Set User
            _logger.LogInformation("[STEP 4] Setting user: {0}", _settings.Username);
            if (string.IsNullOrWhiteSpace(_settings.Username))
            {
                throw new InvalidOperationException("Sage username is not configured in settings.");
            }

            int retVal = _session.nSetUser(_settings.Username, _settings.Password ?? "");
            if (retVal == 0)
            {
                string errorMsg = _session.sLastErrorMsg ?? "Unknown error";
                throw new InvalidOperationException($"nSetUser failed: {errorMsg}");
            }
            _logger.LogInformation("[STEP 4] User set successfully (retVal={0})", retVal);

            // STEP 5: Set Company
            _logger.LogInformation("[STEP 5] Setting company: {0}", _settings.CompanyCode);
            if (string.IsNullOrWhiteSpace(_settings.CompanyCode))
            {
                throw new InvalidOperationException("Sage company code is not configured in settings.");
            }

            retVal = _session.nSetCompany(_settings.CompanyCode);
            if (retVal == 0)
            {
                string errorMsg = _session.sLastErrorMsg ?? "Unknown error";
                throw new InvalidOperationException($"nSetCompany failed: {errorMsg}");
            }
            _logger.LogInformation("[STEP 5] Company set successfully (retVal={0})", retVal);

            // STEP 6: Set Date (format: YYYYMMDD)
            string today = DateTime.Today.ToString("yyyyMMdd");
            _logger.LogInformation("[STEP 6] Setting date for I/M module: {0}", today);
            retVal = _session.nSetDate("I/M", today);
            if (retVal == 0)
            {
                string errorMsg = _session.sLastErrorMsg ?? "Unknown error";
                throw new InvalidOperationException($"nSetDate failed: {errorMsg}");
            }
            _logger.LogInformation("[STEP 6] Date set successfully (retVal={0})", retVal);

            // STEP 7: Set Module
            _logger.LogInformation("[STEP 7] Setting module: I/M");
            retVal = _session.nSetModule("I/M");
            if (retVal == 0)
            {
                string errorMsg = _session.sLastErrorMsg ?? "Unknown error";
                throw new InvalidOperationException($"nSetModule failed: {errorMsg}");
            }
            _logger.LogInformation("[STEP 7] Module set successfully (retVal={0})", retVal);

            _isInitialized = true;
            _logger.LogInformation("=== Sage 100 Session Initialized Successfully ===");
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to initialize Sage session", ex);
            Cleanup();
            throw;
        }
    }

    /// <summary>
    /// Creates a new Sage business object
    /// </summary>
    public dynamic CreateBusinessObject(string objectName)
    {
        if (!_isInitialized)
            throw new InvalidOperationException("Session must be initialized before creating business objects.");

        _logger.LogInformation("Creating business object: {0}", objectName);
        
        try
        {
            dynamic obj = _providex.NewObject(objectName, _session);
            if (obj == null)
            {
                throw new InvalidOperationException($"Failed to create business object: {objectName}");
            }
            
            _logger.LogInformation("Business object created: {0}", objectName);
            return obj;
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to create business object: {0}", ex, objectName);
            throw;
        }
    }

    /// <summary>
    /// Sets the program context for a specific task
    /// </summary>
    public void SetProgramContext(string taskName)
    {
        if (!_isInitialized)
            throw new InvalidOperationException("Session must be initialized before setting program context.");

        _logger.LogInformation("Setting program context for task: {0}", taskName);
        
        try
        {
            int taskId = _session.nLookupTask(taskName);
            _session.nSetProgram(taskId);
            _logger.LogInformation("Program context set for task: {0} (ID={1})", taskName, taskId);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to set program context for task: {0}", ex, taskName);
            throw;
        }
    }

    /// <summary>
    /// Cleanup and release COM objects
    /// </summary>
    private void Cleanup()
    {
        _logger.LogInformation("Cleaning up Sage session");

        try
        {
            if (_session != null)
            {
                try
                {
                    _session.nCleanup();
                    _session.DropObject();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Error during session cleanup: {0}", ex.Message);
                }

                Marshal.ReleaseComObject(_session);
                _session = null;
            }

            if (_providex != null)
            {
                Marshal.ReleaseComObject(_providex);
                _providex = null;
            }

            _isInitialized = false;
            _logger.LogInformation("Sage session cleaned up successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError("Error during cleanup", ex);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        Cleanup();
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    ~SageSessionService()
    {
        Dispose();
    }
}
