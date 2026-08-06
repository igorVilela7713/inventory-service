namespace Inventory.Domain.Events;

public record StockInsufficient(
    string OrderId,
    string ProductId,
    int RequestedQuantity,
    int AvailableQuantity,
    DateTime Timestamp);
