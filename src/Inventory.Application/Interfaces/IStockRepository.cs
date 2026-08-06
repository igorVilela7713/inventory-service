using Inventory.Domain.Entities;

namespace Inventory.Application.Interfaces;

public interface IStockRepository
{
    Task<StockItem?> GetByProductIdAsync(string productId, CancellationToken ct = default);
    Task<IReadOnlyList<StockItem>> GetAllAsync(int page, int size, CancellationToken ct = default);
    Task<StockItem> CreateAsync(StockItem item, CancellationToken ct = default);
    Task UpdateAsync(StockItem item, CancellationToken ct = default);
}
