using Aml.BOM.Import.Application.Services;
using Aml.BOM.Import.Shared.Interfaces;
using Moq;

namespace Aml.BOM.Import.Tests.Application;

public class NewItemServiceTests
{
    private readonly Mock<INewMakeItemRepository> _mockNewMakeItemRepository;
    private readonly Mock<INewBuyItemRepository> _mockNewBuyItemRepository;
    private readonly Mock<ISageItemRepository> _mockSageItemRepository;
    private readonly NewItemService _newItemService;

    public NewItemServiceTests()
    {
        _mockNewMakeItemRepository = new Mock<INewMakeItemRepository>();
        _mockNewBuyItemRepository = new Mock<INewBuyItemRepository>();
        _mockSageItemRepository = new Mock<ISageItemRepository>();

        _newItemService = new NewItemService(
            _mockNewMakeItemRepository.Object,
            _mockNewBuyItemRepository.Object,
            _mockSageItemRepository.Object
        );
    }

    [Fact]
    public async Task GetNewMakeItemsAsync_ShouldReturnMakeItems()
    {
        // Arrange
        var expectedItems = new List<object> { new object(), new object() };
        _mockNewMakeItemRepository
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(expectedItems);

        // Act
        var result = await _newItemService.GetNewMakeItemsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        _mockNewMakeItemRepository.Verify(x => x.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetNewBuyItemsAsync_ShouldReturnBuyItems()
    {
        // Arrange
        var expectedItems = new List<object> { new object() };
        _mockNewBuyItemRepository
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(expectedItems);

        // Act
        var result = await _newItemService.GetNewBuyItemsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        _mockNewBuyItemRepository.Verify(x => x.GetAllAsync(), Times.Once);
    }
}
