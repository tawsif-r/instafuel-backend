using Microsoft.EntityFrameworkCore;

namespace Instafuel.Api.Models;

// Development data so the customer app can order without an admin setting up
// suppliers first. Runs on startup when Seed:DevFuelCatalog is true (Development only).
// Does nothing once any fuel product exists.
public static class DevSeeder
{
    public static async Task SeedFuelCatalogAsync(PgDbContext context)
    {
        if (await context.Products.AnyAsync(p => p.ProductType == "fuel")) return;

        var category = await context.Categories.FirstOrDefaultAsync(c => c.Slug == "fuel")
            ?? new Category { Name = "Fuel", Slug = "fuel" };

        var supplier = new Supplier
        {
            LegalName = "Instafuel Depot Ltd.",
            DisplayName = "Instafuel Depot",
            Status = "active"
        };
        supplier.Locations.Add(new SupplierLocation
        {
            Name = "Tejgaon depot",
            AddressLine = "Tejgaon Industrial Area",
            City = "Dhaka"
        });

        // Same demo prices as the app's mock data.
        foreach (var (grade, name, price) in new[] { ("octane", "Octane", 135m), ("petrol", "Petrol", 130m), ("diesel", "Diesel", 114m) })
        {
            var product = new Product
            {
                Category = category,
                Name = name,
                ProductType = "fuel",
                UnitCode = "L",
                FuelGrade = grade
            };
            supplier.Offers.Add(new SupplierOffer { Product = product, UnitPrice = price });
        }

        context.Suppliers.Add(supplier);
        await context.SaveChangesAsync();
    }
}
