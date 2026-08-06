namespace Inventory.Domain.Entities;

public class StockItem
{
    public Guid Id { get; set; }
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int ReservedQuantity { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public int AvailableQuantity => Quantity - ReservedQuantity;

    public bool CanReserve(int amount) => AvailableQuantity >= amount;

    public void Reserve(int amount)
    {
        if (!CanReserve(amount))
            throw new InvalidOperationException($"Insufficient stock for {ProductId}: available={AvailableQuantity}, requested={amount}");
        ReservedQuantity += amount;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Release(int amount)
    {
        ReservedQuantity = Math.Max(0, ReservedQuantity - amount);
        UpdatedAt = DateTime.UtcNow;
    }

    public void Adjust(int delta)
    {
        Quantity += delta;
        UpdatedAt = DateTime.UtcNow;
    }
}
