using Inventory.Application.Interfaces;

namespace Inventory.Infrastructure.Persistence.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly InventoryDbContext _db;

    public UnitOfWork(InventoryDbContext db) => _db = db;

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}
