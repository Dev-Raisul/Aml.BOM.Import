using Aml.BOM.Import.Domain.Entities;
using Aml.BOM.Import.Domain.Enums;

namespace Aml.BOM.Import.Tests.Domain;

public class BomImportRecordTests
{
    [Fact]
    public void BomImportRecord_ShouldInitializeWithDefaultValues()
    {
        // Arrange & Act
        var bomRecord = new BomImportRecord();

        // Assert
        Assert.NotNull(bomRecord);
        Assert.Equal(string.Empty, bomRecord.BomNumber);
        Assert.Equal(BomIntegrationStatus.Pending, bomRecord.Status);
        Assert.NotNull(bomRecord.Lines);
        Assert.Empty(bomRecord.Lines);
    }

    [Fact]
    public void BomImportRecord_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var bomRecord = new BomImportRecord
        {
            BomNumber = "BOM-001",
            Description = "Test BOM",
            FileName = "test.csv",
            ImportDate = DateTime.Now,
            ImportedBy = "TestUser",
            Status = BomIntegrationStatus.Validated
        };

        // Assert
        Assert.Equal("BOM-001", bomRecord.BomNumber);
        Assert.Equal("Test BOM", bomRecord.Description);
        Assert.Equal("test.csv", bomRecord.FileName);
        Assert.Equal("TestUser", bomRecord.ImportedBy);
        Assert.Equal(BomIntegrationStatus.Validated, bomRecord.Status);
    }
}
