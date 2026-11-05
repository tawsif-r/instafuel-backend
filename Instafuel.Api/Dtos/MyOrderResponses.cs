using Instafuel.Api.Models;

namespace Instafuel.Api.Dtos;

// What the customer app sees of an order. Unlike most endpoints this is not the
// entity itself: the app needs a flat view (one delivery/payment status, driver
// name, timeline) and must not see supplier or driver internals.
public record MyOrderResponse(
    Guid Id,
    string OrderNumber,
    string Status,
    string DeliveryStatus,
    string PaymentStatus,  // unpaid | pending | paid | failed
    string? PaymentMethod, // method of the latest payment
    DateTime RequestedDeliveryAt,
    DateTime CreatedAt,
    string DeliveryAddress,
    string? CustomerNotes,
    decimal Subtotal,
    decimal DeliveryFee,
    decimal TotalAmount,
    string? DriverName,
    string? TruckRegistration,
    List<MyOrderLineResponse> Lines,
    List<OrderEventResponse> Timeline)
{
    private static readonly Dictionary<string, string> MethodLabels = new()
    {
        ["cash"] = "Cash on delivery",
        ["card"] = "Card",
        ["mobile_banking"] = "Mobile banking",
        ["bank_transfer"] = "Bank transfer",
    };

    private static readonly Dictionary<string, string> DeliveryEvents = new()
    {
        ["assigned"] = "Driver assigned",
        ["dispatched"] = "Dispatched from depot",
        ["in_transit"] = "On the way",
        ["delivered"] = "Delivered",
        ["failed"] = "Delivery failed",
    };

    // Needs SupplierOrders.Items, SupplierOrders.Deliveries (Driver.User, Vehicle, StatusHistory) and Payments loaded.
    public static MyOrderResponse From(Order order)
    {
        var delivery = order.SupplierOrders.SelectMany(s => s.Deliveries).MaxBy(d => d.CreatedAt);
        var payment = order.Payments.MaxBy(p => p.CreatedAt);

        var deliveryStatus = order.Status == "cancelled" ? "cancelled" : delivery?.Status ?? "pending";
        var paymentStatus = payment?.Status switch
        {
            null or "cancelled" => "unpaid",
            "pending" => "pending",
            "successful" => "paid",
            _ => "failed",
        };

        var timeline = new List<OrderEventResponse> { new("Order placed", order.CreatedAt) };
        foreach (var p in order.Payments)
        {
            var label = MethodLabels.GetValueOrDefault(p.Method, p.Method);
            timeline.Add(p.Status switch
            {
                "successful" => new($"Payment received ({label})", p.PaidAt ?? p.CreatedAt),
                "pending" or "cancelled" => new($"Payment on delivery ({label})", p.CreatedAt),
                _ => new("Payment failed", p.CreatedAt),
            });
        }
        foreach (var h in order.SupplierOrders.SelectMany(s => s.Deliveries).SelectMany(d => d.StatusHistory))
        {
            if (DeliveryEvents.TryGetValue(h.Status, out var title))
                timeline.Add(new(title, h.CreatedAt));
        }
        if (order.Status == "cancelled")
            timeline.Add(new("Order cancelled", order.UpdatedAt));

        return new MyOrderResponse(
            order.Id,
            order.OrderNumber,
            order.Status,
            deliveryStatus,
            paymentStatus,
            payment?.Method,
            order.RequestedDeliveryAt,
            order.CreatedAt,
            order.DeliveryAddress,
            order.CustomerNotes,
            order.Subtotal,
            order.DeliveryFee,
            order.TotalAmount,
            delivery?.Driver?.User.FullName,
            delivery?.Vehicle?.RegistrationNumber,
            order.SupplierOrders.SelectMany(s => s.Items).OrderBy(i => i.CreatedAt).Select(i => new MyOrderLineResponse(
                i.CustomerVehicleId,
                i.VehicleName ?? i.ProductName,
                i.VehicleRegistration ?? string.Empty,
                i.FuelType,
                i.Quantity,
                i.UnitPrice,
                i.LineTotal,
                i.PlateVerifiedAt)).ToList(),
            timeline.OrderBy(e => e.At).ToList());
    }
}

public record MyOrderLineResponse(
    Guid? CustomerVehicleId,
    string VehicleName,
    string RegistrationNumber,
    string? FuelType,
    decimal Quantity,
    decimal UnitPrice,
    decimal LineTotal,
    DateTime? PlateVerifiedAt);

public record OrderEventResponse(string Title, DateTime At);

public record FuelProductResponse(string FuelType, decimal PricePerLitre);

public record OrderQuoteResponse(decimal Subtotal, decimal DeliveryFee, decimal Total);

public record PayOrderResponse(Guid OrderId, string Status, string Method, decimal Amount, string? Reference, string? Message);
