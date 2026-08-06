using Inventory.Application.Services;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Inventory.Worker.Consumers;

public class OrderEventConsumer : IConsumer<OrderCreatedEvent>
{
    private readonly InventoryService _inventoryService;
    private readonly ILogger<OrderEventConsumer> _logger;

    public OrderEventConsumer(InventoryService inventoryService, ILogger<OrderEventConsumer> logger)
    {
        _inventoryService = inventoryService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderCreatedEvent> context)
    {
        var message = context.Message;
        _logger.LogInformation("Received ORDER_CREATED for OrderId={OrderId}, ProductId={ProductId}, Quantity={Quantity}",
            message.OrderId, message.ProductId, message.Quantity);

        try
        {
            await _inventoryService.ReserveAsync(message.OrderId, message.ProductId, message.Quantity, context.CancellationToken);
            _logger.LogInformation("Stock reserved for OrderId={OrderId}", message.OrderId);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Insufficient stock for OrderId={OrderId}, ProductId={ProductId}", message.OrderId, message.ProductId);
        }
    }
}

public record OrderCreatedEvent(string OrderId, string ProductId, int Quantity, DateTime Timestamp);
