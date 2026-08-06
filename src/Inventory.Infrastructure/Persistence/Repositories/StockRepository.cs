using Inventory.Application.Interfaces;
using Inventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Persistence.Repositories;

public class StockRepository : IStockRepository
{
    private readonly InventoryDbContext _db;

    public StockRepository(InventoryDbContext db) => _db = db;

    public async Task<StockItem?> GetByProductIdAsync(string productId, CancellationToken ct = default)
        => await _db.StockItems.FirstOrDefaultAsync(x => x.ProductId == productId, ct);

    public async Task<IReadOnlyList<StockItem>> GetAllAsync(int page, int size, CancellationToken ct = default)
        => await _db.StockItems
            .OrderBy(x => x.ProductId)
            .Skip(page * size)
            .Take(size)
            .ToListAsync(ct);

    public async Task<StockItem> CreateAsync(StockItem item, CancellationToken ct = default)
    {
        await _db.StockItems.AddAsync(item, ct);
        return item;
    }

    public Task UpdateAsync(StockItem item, CancellationToken ct = default)
    {
        _db.StockItems.Update(item);
        return Task.CompletedTask;
    }
}
