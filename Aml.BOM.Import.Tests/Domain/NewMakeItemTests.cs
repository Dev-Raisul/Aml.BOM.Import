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
        Assert.Equal("F", makeItem.ProductType);
        Assert.Equal("M", makeItem.Procurement);
        Assert.Equal("EACH", makeItem.StandardUnitOfMeasure);
        Assert.False(makeItem.StagedItem);
        Assert.False(makeItem.Coated);
        Assert.False(makeItem.GoldenStandard);
    }

    [Fact]
    public void NewMakeItem_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var makeItem = new NewMakeItem
        {
            ItemCode = "MAKE-001",
            ItemDescription = "Test Make Item",
            ProductLine = "PL-001",
            StandardUnitOfMeasure = "EA",
            DrawingNumber = "DWG-001",
            ProductGroup = "Group A",
            StagedItem = true,
            Coated = false,
            GoldenStandard = true
        };

        // Assert
        Assert.Equal("MAKE-001", makeItem.ItemCode);
        Assert.Equal("Test Make Item", makeItem.ItemDescription);
        Assert.Equal("PL-001", makeItem.ProductLine);
        Assert.Equal("EA", makeItem.StandardUnitOfMeasure);
        Assert.Equal("DWG-001", makeItem.DrawingNumber);
        Assert.Equal("Group A", makeItem.ProductGroup);
        Assert.True(makeItem.StagedItem);
        Assert.False(makeItem.Coated);
        Assert.True(makeItem.GoldenStandard);
    }

    [Fact]
    public void NewMakeItem_ShouldTrackEditedStatus()
    {
        // Arrange
        var makeItem = new NewMakeItem();
        Assert.False(makeItem.IsEdited);

        // Act
        makeItem.ProductLine = "PL-001";

        // Assert
        Assert.True(makeItem.IsEdited);
    }

    [Fact]
    public void NewMakeItem_ShouldTrackIntegrationStatus()
    {
        // Arrange
        var makeItem = new NewMakeItem();
        Assert.False(makeItem.IsIntegrated);
        Assert.Null(makeItem.IntegratedDate);
        Assert.Null(makeItem.IntegratedBy);

        // Act
        makeItem.IsIntegrated = true;
        makeItem.IntegratedDate = DateTime.Now;
        makeItem.IntegratedBy = "TestUser";

        // Assert
        Assert.True(makeItem.IsIntegrated);
        Assert.NotNull(makeItem.IntegratedDate);
        Assert.Equal("TestUser", makeItem.IntegratedBy);
    }
}
