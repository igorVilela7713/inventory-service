using Inventory.Domain.Entities;

namespace Inventory.Application.Interfaces;

public interface IReservationRepository
{
    Task<Reservation?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Reservation>> GetByOrderIdAsync(string orderId, CancellationToken ct = default);
    Task<Reservation> CreateAsync(Reservation reservation, CancellationToken ct = default);
    Task UpdateAsync(Reservation reservation, CancellationToken ct = default);
}
