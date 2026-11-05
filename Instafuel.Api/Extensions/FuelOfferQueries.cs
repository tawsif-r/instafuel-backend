using Instafuel.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Instafuel.Api.Extensions;

public static class FuelOfferQueries
{
    // Fuel types a customer can order. Same values as CustomerVehicle.FuelType and Product.FuelGrade.
    public static readonly string[] FuelTypes = ["octane", "petrol", "diesel"];

    // The cheapest offer that can be ordered right now for each fuel type, keyed by fuel type.
    // A fuel type with no orderable offer is missing from the result.
    // Includes Product and Supplier.Locations, which order creation needs.
    public static async Task<Dictionary<string, SupplierOffer>> CheapestFuelOffersAsync(this PgDbContext context)
    {
        var offers = await context.SupplierOffers.AsNoTracking()
            .Include(o => o.Product)
            .Include(o => o.Supplier).ThenInclude(s => s.Locations)
            .Where(o => o.Status == "active"
                && o.Supplier.Status == "active"
                && o.Supplier.Locations.Any(l => l.IsActive)
                && o.Product.IsActive
                && o.Product.ProductType == "fuel"
                && FuelTypes.Contains(o.Product.FuelGrade!))
            .ToListAsync();

        return offers
            .GroupBy(o => o.Product.FuelGrade!)
            .ToDictionary(g => g.Key, g => g.MinBy(o => o.UnitPrice)!);
    }
}
