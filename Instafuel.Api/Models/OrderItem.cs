using Microsoft.EntityFrameworkCore;

namespace Instafuel.Api.Models;

public class OrderItem
{
    public Guid Id { get; set; }
    public Guid SupplierOrderId { get; set; }
    public Guid SupplierOfferId { get; set; }
    public Guid? CustomerVehicleId { get; set; } // the customer's vehicle this line fuels

    // Snapshot fields (copied at purchase time)
    public string ProductName { get; set; } = string.Empty;
    public string SellerName { get; set; } = string.Empty;
    public string? VehicleName { get; set; }
    public string? VehicleRegistration { get; set; } // the driver checks this against the plate
    public string? FuelType { get; set; }            // octane | petrol | diesel

    [Precision(14, 3)]
    public decimal Quantity { get; set; }

    public string UnitCode { get; set; } = string.Empty;

    [Precision(14, 2)]
    public decimal UnitPrice { get; set; }

    [Precision(14, 2)]
    public decimal LineTotal { get; set; }

    public DateTime? PlateVerifiedAt { get; set; } // set by the driver app when the plate matches
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public SupplierOrder SupplierOrder { get; set; } = null!;
    public SupplierOffer SupplierOffer { get; set; } = null!;
    public CustomerVehicle? CustomerVehicle { get; set; }
}
