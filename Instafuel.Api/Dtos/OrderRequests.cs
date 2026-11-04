using System.ComponentModel.DataAnnotations;

namespace Instafuel.Api.Dtos;

public record CreateOrderRequest(
    Guid CustomerId,
    [Required, MaxLength(150)] string RecipientName,
    [Required, MaxLength(20)] string RecipientPhone,
    [Required] string DeliveryAddress,
    [Required, MaxLength(100)] string DeliveryCity,
    decimal? DeliveryLatitude,
    decimal? DeliveryLongitude,
    string? CustomerNotes,
    [Required, MinLength(1)] List<OrderItemRequest> Items);

// Price is NOT sent by the client; it is read from the SupplierOffer.
public record OrderItemRequest(
    Guid SupplierOfferId,
    [Range(0.001, 1_000_000)] decimal Quantity);

public record UpdateOrderStatusRequest(
    [Required, AllowedValues("pending", "confirmed", "processing", "partially_fulfilled", "completed", "cancelled", "failed")]
    string Status);
