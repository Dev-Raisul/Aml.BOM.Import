using Aml.BOM.Import.Domain.Entities;
using Aml.BOM.Import.Domain.Enums;

namespace Aml.BOM.Import.Tests.Domain;

public class NewMakeItemTests
{
    [Fact]
    public void NewMakeItem_ShouldInitializeWithDefaultValues()
    {
        // Arrange & Act
        var makeItem = new NewMakeItem();

        // Assert
        Assert.NotNull(makeItem);
        Assert.Equal(string.Empty, makeItem.ItemCode);
        Assert.Equal(ItemIntegrationStatus.Pending, makeItem.Status);
    }

    [Fact]
    public void NewMakeItem_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var makeItem = new NewMakeItem
        {
            ItemCode = "MAKE-001",
            Description = "Test Make Item",
            UnitOfMeasure = "EA",
            DrawingNumber = "DWG-001",
            ProductGroup = "Group A",
            Status = ItemIntegrationStatus.New
        };

        // Assert
        Assert.Equal("MAKE-001", makeItem.ItemCode);
        Assert.Equal("Test Make Item", makeItem.Description);
        Assert.Equal("EA", makeItem.UnitOfMeasure);
        Assert.Equal("DWG-001", makeItem.DrawingNumber);
        Assert.Equal("Group A", makeItem.ProductGroup);
        Assert.Equal(ItemIntegrationStatus.New, makeItem.Status);
    }
}
