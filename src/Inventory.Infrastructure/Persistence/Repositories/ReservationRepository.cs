using Inventory.Application.Interfaces;
using Inventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Persistence.Repositories;

public class ReservationRepository : IReservationRepository
{
    private readonly InventoryDbContext _db;

    public ReservationRepository(InventoryDbContext db) => _db = db;

    public async Task<Reservation?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.Reservations.FindAsync([id], ct);

    public async Task<IReadOnlyList<Reservation>> GetByOrderIdAsync(string orderId, CancellationToken ct = default)
        => await _db.Reservations
            .Where(x => x.OrderId == orderId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);

    public async Task<Reservation> CreateAsync(Reservation reservation, CancellationToken ct = default)
    {
        await _db.Reservations.AddAsync(reservation, ct);
        return reservation;
    }

    public Task UpdateAsync(Reservation reservation, CancellationToken ct = default)
    {
        _db.Reservations.Update(reservation);
        return Task.CompletedTask;
    }
}
