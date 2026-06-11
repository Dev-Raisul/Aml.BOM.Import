using System.Collections.ObjectModel;
using System.IO;
using Aml.BOM.Import.Shared.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Aml.BOM.Import.UI.ViewModels;

public partial class LogsViewModel : ObservableObject
{
    private readonly ILoggerService _logger;
    private System.Timers.Timer? _refreshTimer;

    [ObservableProperty]
    private string _logContent = string.Empty;

    [ObservableProperty]
    private ObservableCollection<string> _logFiles = new();

    private string? _selectedLogFile;
    public string? SelectedLogFile
    {
        get => _selectedLogFile;
        set
        {
            if (SetProperty(ref _selectedLogFile, value))
            {
                _ = LoadLogContent();
            }
        }
    }

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _loadingMessage = "Loading...";

    private bool _autoRefresh;
    public bool AutoRefresh
    {
        get => _autoRefresh;
        set
        {
            if (SetProperty(ref _autoRefresh, value))
            {
                if (value)
                    StartAutoRefresh();
                else
                    StopAutoRefresh();
            }
        }
    }

    [ObservableProperty]
    private string _logDirectory = string.Empty;

    public LogsViewModel(ILoggerService logger)
    {
        _logger = logger;
        _ = LoadLogFiles();
    }

    [RelayCommand]
    private async Task LoadLogFiles()
    {
        IsLoading = true;
        LoadingMessage = "Loading log files...";
        try
        {
            LogDirectory = _logger.GetLogDirectory();
            var files = _logger.GetLogFiles().ToList();
            
            LogFiles.Clear();
            foreach (var file in files)
            {
                LogFiles.Add(Path.GetFileName(file));
            }

            // Select the most recent log file by default
            if (LogFiles.Any())
            {
                SelectedLogFile = LogFiles.First();
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Refresh()
    {
        await LoadLogFiles();
    }

    [RelayCommand]
    private async Task OpenLogDirectory()
    {
        try
        {
            if (Directory.Exists(LogDirectory))
            {
                System.Diagnostics.Process.Start("explorer.exe", LogDirectory);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to open log directory", ex);
        }
        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task ClearLogs()
    {
        var result = System.Windows.MessageBox.Show(
            "Are you sure you want to delete all log files?",
            "Confirm Delete",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);

        if (result == System.Windows.MessageBoxResult.Yes)
        {
            try
            {
                var files = Directory.GetFiles(LogDirectory, "*.log");
                foreach (var file in files)
                {
                    File.Delete(file);
                }

                LogContent = string.Empty;
                await LoadLogFiles();
                
                System.Windows.MessageBox.Show(
                    "All log files have been deleted.",
                    "Logs Cleared",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to clear log files", ex);
                System.Windows.MessageBox.Show(
                    $"Failed to clear logs: {ex.Message}",
                    "Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }
    }

    private async Task LoadLogContent()
    {
        if (string.IsNullOrEmpty(SelectedLogFile))
            return;

        try
        {
            var filePath = Path.Combine(LogDirectory, SelectedLogFile);
            if (File.Exists(filePath))
            {
                LogContent = await File.ReadAllTextAsync(filePath);
            }
        }
        catch (Exception ex)
        {
            LogContent = $"Error reading log file: {ex.Message}";
        }
    }

    private void StartAutoRefresh()
    {
        _refreshTimer = new System.Timers.Timer(5000); // Refresh every 5 seconds
        _refreshTimer.Elapsed += async (s, e) => await LoadLogContent();
        _refreshTimer.Start();
    }

    private void StopAutoRefresh()
    {
        _refreshTimer?.Stop();
        _refreshTimer?.Dispose();
        _refreshTimer = null;
    }
}

