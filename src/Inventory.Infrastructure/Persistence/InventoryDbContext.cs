using Inventory.Domain.Entities;
using Inventory.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Persistence;

public class InventoryDbContext : DbContext
{
    public InventoryDbContext(DbContextOptions<InventoryDbContext> options) : base(options) { }

    public DbSet<StockItem> StockItems => Set<StockItem>();
    public DbSet<Reservation> Reservations => Set<Reservation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StockItem>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.ProductId).IsUnique();
            e.Property(x => x.ProductId).HasMaxLength(100);
            e.Property(x => x.ProductName).HasMaxLength(200);
        });

        modelBuilder.Entity<Reservation>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.OrderId);
            e.Property(x => x.OrderId).HasMaxLength(100);
            e.Property(x => x.ProductId).HasMaxLength(100);
            e.Property(x => x.Status).HasConversion(
                v => v.ToString(),
                v => Enum.Parse<ReservationStatus>(v));
        });
    }
}
