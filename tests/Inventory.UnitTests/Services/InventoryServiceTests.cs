using FluentAssertions;
using Inventory.Application.Interfaces;
using Inventory.Application.Services;
using Inventory.Domain.Entities;
using Moq;
using Xunit;

namespace Inventory.UnitTests.Services;

public class InventoryServiceTests
{
    private readonly Mock<IStockRepository> _stockRepo = new();
    private readonly Mock<IReservationRepository> _reservationRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly InventoryService _sut;

    public InventoryServiceTests()
    {
        _sut = new InventoryService(
            _stockRepo.Object,
            _reservationRepo.Object,
            _unitOfWork.Object,
            new Microsoft.Extensions.Logging.Abstractions.NullLogger<InventoryService>());
    }

    [Fact]
    public async Task GetStockAsync_ReturnsItem_WhenExists()
    {
        // Arrange
        var item = new StockItem { Id = Guid.NewGuid(), ProductId = "PROD-001", Quantity = 10 };
        _stockRepo.Setup(r => r.GetByProductIdAsync("PROD-001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        // Act
        var result = await _sut.GetStockAsync("PROD-001");

        // Assert
        result.Should().NotBeNull();
        result!.ProductId.Should().Be("PROD-001");
    }

    [Fact]
    public async Task ReserveAsync_CreatesReservation_WhenStockAvailable()
    {
        // Arrange
        var stock = new StockItem { Id = Guid.NewGuid(), ProductId = "PROD-001", Quantity = 10 };
        _stockRepo.Setup(r => r.GetByProductIdAsync("PROD-001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(stock);

        // Act
        var result = await _sut.ReserveAsync("ORD-001", "PROD-001", 3);

        // Assert
        result.Should().NotBeNull();
        result.OrderId.Should().Be("ORD-001");
        stock.ReservedQuantity.Should().Be(3);
        stock.AvailableQuantity.Should().Be(7);
    }

    [Fact]
    public async Task ReserveAsync_Throws_WhenInsufficientStock()
    {
        // Arrange
        var stock = new StockItem { Id = Guid.NewGuid(), ProductId = "PROD-001", Quantity = 2 };
        _stockRepo.Setup(r => r.GetByProductIdAsync("PROD-001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(stock);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.ReserveAsync("ORD-001", "PROD-001", 5));
    }

    [Fact]
    public async Task CancelReservationAsync_ReleasesStock()
    {
        // Arrange
        var stock = new StockItem { Id = Guid.NewGuid(), ProductId = "PROD-001", Quantity = 10, ReservedQuantity = 3 };
        var reservation = new Reservation
        {
            Id = Guid.NewGuid(),
            OrderId = "ORD-001",
            ProductId = "PROD-001",
            Quantity = 3,
            Status = Domain.Enums.ReservationStatus.Confirmed
        };
        _reservationRepo.Setup(r => r.GetByIdAsync(reservation.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);
        _stockRepo.Setup(r => r.GetByProductIdAsync("PROD-001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(stock);

        // Act
        await _sut.CancelReservationAsync(reservation.Id);

        // Assert
        stock.ReservedQuantity.Should().Be(0);
        reservation.Status.Should().Be(Domain.Enums.ReservationStatus.Cancelled);
    }
}
