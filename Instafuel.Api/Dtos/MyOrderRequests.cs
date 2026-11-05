using System.ComponentModel.DataAnnotations;

namespace Instafuel.Api.Dtos;

// One of the customer's vehicles, the fuel to put in it and how many litres.
// The price is NOT sent; the server uses the cheapest active offer for that fuel.
public record OrderLineRequest(
    Guid CustomerVehicleId,
    [Required, AllowedValues("octane", "petrol", "diesel")] string FuelType,
    [Range(1, 10_000)] decimal Quantity);

public record QuoteOrderRequest(
    [Required, MinLength(1)] List<OrderLineRequest> Lines);

public record PlaceOrderRequest(
    [Required, MaxLength(300)] string DeliveryAddress,
    DateTime RequestedDeliveryAt,
    [MaxLength(500)] string? CustomerNotes,
    [Required, MinLength(1)] List<OrderLineRequest> Lines);

public record PayOrderRequest(
    [Required, AllowedValues("cash", "card", "mobile_banking", "bank_transfer", "corporate_credit")] string Method,
    [Required, MaxLength(100)] string IdempotencyKey); // client generates it; retrying with the same key won't pay twice
