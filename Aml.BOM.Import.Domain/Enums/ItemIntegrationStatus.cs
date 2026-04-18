namespace Aml.BOM.Import.Domain.Enums;

public enum ItemIntegrationStatus
{
    Pending = 0,
    New = 1,
    ExistsInSage = 2,
    Integrated = 3,
    Failed = 4
}
