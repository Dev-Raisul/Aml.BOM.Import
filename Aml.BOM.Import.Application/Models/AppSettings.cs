namespace Aml.BOM.Import.Application.Models;

public class AppSettings
{
    public string DatabaseConnectionString { get; set; } = string.Empty;
    public SageSettings SageSettings { get; set; } = new();
    public ReportSettings ReportSettings { get; set; } = new();
}

public class SageSettings
{
    public string ServerUrl { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string CompanyCode { get; set; } = string.Empty;
}

public class ReportSettings
{
    public string OutputDirectory { get; set; } = string.Empty;
    public bool AutoGenerateReports { get; set; }
}
