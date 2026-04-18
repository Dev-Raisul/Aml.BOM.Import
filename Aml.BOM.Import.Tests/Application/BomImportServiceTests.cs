using Aml.BOM.Import.Application.Services;
using Aml.BOM.Import.Shared.Interfaces;
using Moq;

namespace Aml.BOM.Import.Tests.Application;

public class BomImportServiceTests
{
    private readonly Mock<IBomImportRepository> _mockBomImportRepository;
    private readonly Mock<IFileImportService> _mockFileImportService;
    private readonly Mock<IBomValidationService> _mockBomValidationService;
    private readonly BomImportService _bomImportService;

    public BomImportServiceTests()
    {
        _mockBomImportRepository = new Mock<IBomImportRepository>();
        _mockFileImportService = new Mock<IFileImportService>();
        _mockBomValidationService = new Mock<IBomValidationService>();

        _bomImportService = new BomImportService(
            _mockBomImportRepository.Object,
            _mockFileImportService.Object,
            _mockBomValidationService.Object
        );
    }

    [Fact]
    public async Task GetAllBomsAsync_ShouldReturnBoms()
    {
        // Arrange
        var expectedBoms = new List<object> { new object(), new object() };
        _mockBomImportRepository
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(expectedBoms);

        // Act
        var result = await _bomImportService.GetAllBomsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        _mockBomImportRepository.Verify(x => x.GetAllAsync(), Times.Once);
    }
}
