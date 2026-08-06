namespace Inventory.Domain.Events;

public record StockReserved(
    string OrderId,
    string ProductId,
    int Quantity,
    DateTime Timestamp);
