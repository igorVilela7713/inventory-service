using FluentValidation;
using Inventory.Application.Services;
using Inventory.Application.Validators;

namespace Inventory.Api.Endpoints;

public static class StockEndpoints
{
    public static void MapStockEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/stock")
            .WithTags("Stock");

        group.MapGet("/", async Task<IResult> (InventoryService svc, int page = 0, int size = 20) =>
        {
            var items = await svc.ListStockAsync(page, size);
            return Results.Ok(items);
        })
        .Produces<List<Inventory.Domain.Entities.StockItem>>();

        group.MapGet("/{productId}", async Task<IResult> (string productId, InventoryService svc) =>
        {
            var item = await svc.GetStockAsync(productId);
            return item is not null ? Results.Ok(item) : Results.NotFound();
        })
        .Produces<Inventory.Domain.Entities.StockItem>()
        .Produces(404);

        group.MapPost("/", async Task<IResult> (CreateStockItemRequest request, IValidator<CreateStockItemRequest> validator, InventoryService svc) =>
        {
            var validation = await validator.ValidateAsync(request);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            var item = await svc.CreateStockAsync(request.ProductId, request.ProductName, request.Quantity);
            return Results.Created($"/api/v1/stock/{item.ProductId}", item);
        })
        .Produces<Inventory.Domain.Entities.StockItem>(201)
        .ProducesValidationProblem();

        group.MapPost("/{productId}/adjust", async Task<IResult> (string productId, StockAdjustRequest request, InventoryService svc) =>
        {
            var item = await svc.AdjustStockAsync(productId, request.Delta);
            return Results.Ok(item);
        })
        .Produces<Inventory.Domain.Entities.StockItem>();
    }
}

public record StockAdjustRequest(int Delta);
