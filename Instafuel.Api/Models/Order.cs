using Microsoft.EntityFrameworkCore;

namespace Instafuel.Api.Models;

public class Order
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public string CheckoutIdempotencyKey { get; set; } = string.Empty;
    public string CheckoutRequestFingerprint { get; set; } = string.Empty;
    public string Status { get; set; } = "pending"; // pending | confirmed | processing | partially_fulfilled | completed | cancelled | failed
    public string Currency { get; set; } = "BDT";

    [Precision(14, 2)]
    public decimal Subtotal { get; set; }

    [Precision(14, 2)]
    public decimal DeliveryFee { get; set; }

    [Precision(14, 2)]
    public decimal DiscountAmount { get; set; }

    [Precision(14, 2)]
    public decimal TotalAmount { get; set; }

    public string RecipientName { get; set; } = string.Empty;
    public string RecipientPhone { get; set; } = string.Empty;
    public string DeliveryAddress { get; set; } = string.Empty;
    public string DeliveryCity { get; set; } = string.Empty;

    [Precision(9, 6)]
    public decimal? DeliveryLatitude { get; set; }

    [Precision(9, 6)]
    public decimal? DeliveryLongitude { get; set; }

    public string? CustomerNotes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User Customer { get; set; } = null!;
    public ICollection<SupplierOrder> SupplierOrders { get; set; } = new List<SupplierOrder>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
