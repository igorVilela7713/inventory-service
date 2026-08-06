using FluentValidation;
using Inventory.Application.Services;
using Inventory.Application.Validators;

namespace Inventory.Api.Endpoints;

public static class ReservationEndpoints
{
    public static void MapReservationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/reservations")
            .WithTags("Reservations")
            .WithOpenApi();

        group.MapPost("/", async (CreateReservationRequest request, IValidator<CreateReservationRequest> validator, InventoryService svc) =>
        {
            var validation = await validator.ValidateAsync(request);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            var reservation = await svc.ReserveAsync(request.OrderId, request.ProductId, request.Quantity);
            return Results.Created($"/api/v1/reservations/{reservation.Id}", reservation);
        })
        .Produces<Inventory.Domain.Entities.Reservation>(201)
        .ProducesValidationProblem();

        group.MapDelete("/{id:guid}", async (Guid id, InventoryService svc) =>
        {
            await svc.CancelReservationAsync(id);
            return Results.NoContent();
        })
        .Produces(204)
        .Produces(404);
    }
}
