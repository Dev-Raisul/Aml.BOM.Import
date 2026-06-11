using System.Runtime.InteropServices;
using Aml.BOM.Import.Application.Models;
using Aml.BOM.Import.Shared.Interfaces;

namespace Aml.BOM.Import.Infrastructure.Services;

/// <summary>
/// Singleton shared Sage 100 session service that maintains a single session
/// for the entire application lifetime. Reused across all integration operations.
/// Thread-safe with lock-based synchronization.
/// </summary>
public class SharedSageSessionService : IDisposable
{
    private readonly ILoggerService _logger;
    private readonly ISettingsService _settingsService;
    private readonly object _lock = new object();
    
    private dynamic? _providex;
    private dynamic? _session;
    private SageSettings? _currentSettings;
    private bool _disposed;
    private bool _isInitialized;
    private string _currentModule = "I/M"; // Track current module

    public SharedSageSessionService(ISettingsService settingsService, ILoggerService logger)
    {
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets whether the session is currently initialized and ready to use
    /// </summary>
    public bool IsInitialized
    {
        get
        {
            lock (_lock)
            {
                return _isInitialized && _session != null && _providex != null;
            }
        }
    }

    /// <summary>
    /// Gets the active Sage session object (thread-safe)
    /// </summary>
    public dynamic GetSession()
    {
        lock (_lock)
        {
            EnsureInitialized();
            return _session!;
        }
    }

    /// <summary>
    /// Gets the ProvideX script object (thread-safe)
    /// </summary>
    public dynamic GetProvideX()
    {
        lock (_lock)
        {
            EnsureInitialized();
            return _providex!;
        }
    }

    /// <summary>
    /// Ensures the session is initialized. If settings changed, reinitializes.
    /// Thread-safe and idempotent.
    /// </summary>
    public void EnsureInitialized()
    {
        lock (_lock)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(SharedSageSessionService));

            // Load current settings synchronously using GetAwaiter().GetResult()
            var settings = (_settingsService.GetSettingsAsync().GetAwaiter().GetResult() as Application.Models.AppSettings)?.SageSettings;

            if (settings == null)
            {
                throw new InvalidOperationException("Sage settings are not configured. Please configure Sage settings first.");
            }

            // Check if we need to reinitialize (settings changed or first time)
            bool needsReinit = !_isInitialized || 
                               _currentSettings == null ||
                               !SettingsEqual(_currentSettings, settings);

            if (needsReinit)
            {
                _logger.LogInformation("Initializing/Reinitializing shared Sage session");
                
                // Cleanup old session if exists
                if (_isInitialized)
                {
                    CleanupInternal();
                }

                // Initialize new session
                InitializeInternal(settings);
                _currentSettings = settings;
            }
        }
    }

    /// <summary>
    /// Switches the current module if different from the target module (thread-safe)
    /// </summary>
    /// <param name="module">Target module (I/M or B/M)</param>
    public void SwitchModule(string module)
    {
        lock (_lock)
        {
            EnsureInitialized();

            if (_currentModule == module)
            {
                _logger.LogDebug("Already on module {0}, no switch needed", module);
                return;
            }

            _logger.LogInformation("Switching shared session module from {0} to {1}", _currentModule, module);

            try
            {
                // Set Date for the new module
                string today = DateTime.Today.ToString("yyyyMMdd");
                int retVal = _session!.nSetDate(module, today);
                if (retVal == 0)
                {
                    string errorMsg = _session.sLastErrorMsg ?? "Unknown error";
                    throw new InvalidOperationException($"nSetDate failed for module {module}: {errorMsg}");
                }

                // Set the new module
                retVal = _session.nSetModule(module);
                if (retVal == 0)
                {
                    string errorMsg = _session.sLastErrorMsg ?? "Unknown error";
                    throw new InvalidOperationException($"nSetModule failed: {errorMsg}");
                }

                _currentModule = module;
                _logger.LogInformation("Successfully switched shared session to module: {0}", module);
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to switch module to {0}", ex, module);
                throw;
            }
        }
    }

    /// <summary>
    /// Creates a new Sage business object using the shared session (thread-safe)
    /// </summary>
    public dynamic CreateBusinessObject(string objectName)
    {
        lock (_lock)
        {
            EnsureInitialized();

            _logger.LogDebug("Creating business object: {0}", objectName);
            
            try
            {
                dynamic obj = _providex!.NewObject(objectName, _session);
                if (obj == null)
                {
                    throw new InvalidOperationException($"Failed to create business object: {objectName}");
                }
                
                _logger.LogDebug("Business object created: {0}", objectName);
                return obj;
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to create business object: {0}", ex, objectName);
                throw;
            }
        }
    }

    /// <summary>
    /// Sets the program context for a specific task (thread-safe)
    /// </summary>
    public void SetProgramContext(string taskName)
    {
        lock (_lock)
        {
            EnsureInitialized();

            _logger.LogDebug("Setting program context for task: {0}", taskName);
            
            try
            {
                int taskId = _session!.nLookupTask(taskName);
                _session.nSetProgram(taskId);
                _logger.LogDebug("Program context set for task: {0} (ID={1})", taskName, taskId);
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to set program context for task: {0}", ex, taskName);
                throw;
            }
        }
    }

    /// <summary>
    /// Manually forces session reinitialization (useful if connection lost)
    /// </summary>
    public void Reinitialize()
    {
        lock (_lock)
        {
            _logger.LogInformation("Manual reinitialization requested");
            CleanupInternal();
            _isInitialized = false;
            _currentSettings = null;
            EnsureInitialized();
        }
    }

    /// <summary>
    /// Internal initialization logic (must be called within lock)
    /// </summary>
    private void InitializeInternal(SageSettings settings)
    {
        try
        {
            _logger.LogInformation("=== Starting Shared Sage 100 Session Initialization ===");

            // STEP 1: Create ProvideX.Script object
            _logger.LogInformation("[STEP 1] Creating ProvideX.Script COM object");
            Type? providexType = Type.GetTypeFromProgID("ProvideX.Script.1");
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
            _logger.LogInformation("[STEP 2] Initializing ProvideX with path: {0}", settings.SagePath);
            if (string.IsNullOrWhiteSpace(settings.SagePath))
            {
                throw new InvalidOperationException("Sage path is not configured in settings.");
            }

            if (!Directory.Exists(settings.SagePath))
            {
                throw new DirectoryNotFoundException(
                    $"Sage path not found: {settings.SagePath}. " +
                    $"Please verify Sage 100 installation path in settings.");
            }

            _providex.Init(settings.SagePath);
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
            _logger.LogInformation("[STEP 4] Setting user: {0}", settings.Username);
            if (string.IsNullOrWhiteSpace(settings.Username))
            {
                throw new InvalidOperationException("Sage username is not configured in settings.");
            }

            int retVal = _session.nSetUser(settings.Username, settings.Password ?? "");
            if (retVal == 0)
            {
                string errorMsg = _session.sLastErrorMsg ?? "Unknown error";
                throw new InvalidOperationException($"nSetUser failed: {errorMsg}");
            }
            _logger.LogInformation("[STEP 4] User set successfully (retVal={0})", retVal);

            // STEP 5: Set Company
            _logger.LogInformation("[STEP 5] Setting company: {0}", settings.CompanyCode);
            if (string.IsNullOrWhiteSpace(settings.CompanyCode))
            {
                throw new InvalidOperationException("Sage company code is not configured in settings.");
            }

            retVal = _session.nSetCompany(settings.CompanyCode);
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

            _currentModule = "I/M"; // Default to I/M module
            _isInitialized = true;
            _logger.LogInformation("=== Shared Sage 100 Session Initialized Successfully (Module: I/M) ===");
            _logger.LogInformation("Session will remain active for application lifetime");
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to initialize shared Sage session", ex);
            CleanupInternal();
            throw;
        }
    }

    /// <summary>
    /// Compare two SageSettings objects for equality
    /// </summary>
    private bool SettingsEqual(SageSettings s1, SageSettings s2)
    {
        return s1.SagePath == s2.SagePath &&
               s1.Username == s2.Username &&
               s1.Password == s2.Password &&
               s1.CompanyCode == s2.CompanyCode;
    }

    /// <summary>
    /// Internal cleanup logic (must be called within lock)
    /// </summary>
    private void CleanupInternal()
    {
        _logger.LogInformation("Cleaning up shared Sage session");

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

                try
                {
                    Marshal.ReleaseComObject(_session);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Error releasing session COM object: {0}", ex.Message);
                }

                _session = null;
            }

            if (_providex != null)
            {
                try
                {
                    Marshal.ReleaseComObject(_providex);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Error releasing ProvideX COM object: {0}", ex.Message);
                }

                _providex = null;
            }

            _isInitialized = false;
            _logger.LogInformation("Shared Sage session cleaned up");
        }
        catch (Exception ex)
        {
            _logger.LogError("Error during cleanup", ex);
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            if (_disposed) return;

            _logger.LogInformation("Disposing shared Sage session service (application shutdown)");
            CleanupInternal();
            _disposed = true;
        }

        GC.SuppressFinalize(this);
    }

    ~SharedSageSessionService()
    {
        Dispose();
    }
}
