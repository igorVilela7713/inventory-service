using Inventory.Application.Interfaces;
using Inventory.Domain.Entities;
using Inventory.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Inventory.Application.Services;

public class InventoryService
{
    private readonly IStockRepository _stockRepo;
    private readonly IReservationRepository _reservationRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<InventoryService> _logger;

    public InventoryService(
        IStockRepository stockRepo,
        IReservationRepository reservationRepo,
        IUnitOfWork unitOfWork,
        ILogger<InventoryService> logger)
    {
        _stockRepo = stockRepo;
        _reservationRepo = reservationRepo;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<StockItem?> GetStockAsync(string productId, CancellationToken ct = default)
        => await _stockRepo.GetByProductIdAsync(productId, ct);

    public async Task<IReadOnlyList<StockItem>> ListStockAsync(int page, int size, CancellationToken ct = default)
        => await _stockRepo.GetAllAsync(page, size, ct);

    public async Task<StockItem> CreateStockAsync(string productId, string productName, int quantity, CancellationToken ct = default)
    {
        var item = new StockItem
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            ProductName = productName,
            Quantity = quantity
        };
        var created = await _stockRepo.CreateAsync(item, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        _logger.LogInformation("Stock created for {ProductId}: {Quantity}", productId, quantity);
        return created;
    }

    public async Task<StockItem> AdjustStockAsync(string productId, int delta, CancellationToken ct = default)
    {
        var item = await _stockRepo.GetByProductIdAsync(productId, ct)
            ?? throw new InvalidOperationException($"Product {productId} not found");
        item.Adjust(delta);
        await _stockRepo.UpdateAsync(item, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        _logger.LogInformation("Stock adjusted for {ProductId}: {Delta} (new: {Quantity})", productId, delta, item.Quantity);
        return item;
    }

    public async Task<Reservation> ReserveAsync(string orderId, string productId, int quantity, CancellationToken ct = default)
    {
        var stock = await _stockRepo.GetByProductIdAsync(productId, ct)
            ?? throw new InvalidOperationException($"Product {productId} not found");

        if (!stock.CanReserve(quantity))
        {
            _logger.LogWarning("Insufficient stock for {ProductId}: available={Available}, requested={Requested}",
                productId, stock.AvailableQuantity, quantity);
            throw new InvalidOperationException($"Insufficient stock for {productId}: available={stock.AvailableQuantity}");
        }

        stock.Reserve(quantity);
        await _stockRepo.UpdateAsync(stock, ct);

        var reservation = new Reservation
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            ProductId = productId,
            Quantity = quantity,
            Status = ReservationStatus.Confirmed,
            ConfirmedAt = DateTime.UtcNow
        };
        await _reservationRepo.CreateAsync(reservation, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Reservation {ReservationId} confirmed for order {OrderId}, product {ProductId}, qty={Quantity}",
            reservation.Id, orderId, productId, quantity);
        return reservation;
    }

    public async Task CancelReservationAsync(Guid reservationId, CancellationToken ct = default)
    {
        var reservation = await _reservationRepo.GetByIdAsync(reservationId, ct)
            ?? throw new InvalidOperationException($"Reservation {reservationId} not found");

        if (reservation.Status != ReservationStatus.Confirmed)
            throw new InvalidOperationException($"Cannot cancel reservation in status {reservation.Status}");

        var stock = await _stockRepo.GetByProductIdAsync(reservation.ProductId, ct);
        stock?.Release(reservation.Quantity);
        if (stock is not null)
            await _stockRepo.UpdateAsync(stock, ct);

        reservation.Status = ReservationStatus.Cancelled;
        reservation.CancelledAt = DateTime.UtcNow;
        await _reservationRepo.UpdateAsync(reservation, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Reservation {ReservationId} cancelled", reservationId);
    }
}
