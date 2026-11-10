using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Instafuel.Api.Models;

// Development and production-readiness data seeder.
// Seeds fuel catalog, corporate accounts, logistics bowser fleet, and certified drivers.
public static class DevSeeder
{
    private static readonly PasswordHasher<User> _hasher = new();

    public static async Task SeedFuelCatalogAsync(PgDbContext context)
    {
        // 1. Seed Fuel Catalog (if not present)
        if (!await context.Products.AnyAsync(p => p.ProductType == "fuel"))
        {
            var category = await context.Categories.FirstOrDefaultAsync(c => c.Slug == "fuel")
                ?? new Category { Name = "Fuel", Slug = "fuel" };

            var supplier = new Supplier
            {
                LegalName = "Instafuel Depot Ltd.",
                DisplayName = "Instafuel Central Depot",
                Status = "active"
            };
            supplier.Locations.Add(new SupplierLocation
            {
                Name = "Tejgaon Fuel Depot",
                AddressLine = "Tejgaon Industrial Area, Plot 14",
                City = "Dhaka"
            });

            foreach (var (grade, name, price) in new[]
            {
                ("octane", "Octane 95 (Premium)", 135m),
                ("petrol", "Petrol (Regular)", 130m),
                ("diesel", "Diesel (Ultra-Low Sulfur)", 114m)
            })
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
        }

        // 2. Seed Corporate Partner (Northwind Logistics)
        if (!await context.Companies.AnyAsync())
        {
            context.Companies.Add(new Company
            {
                Name = "Northwind Logistics Ltd.",
                Code = "NORTHWIND",
                ContactEmail = "corporate@northwind.com",
                ContactPhone = "01700000001",
                CreditLimit = 100000m,
                UsedCredit = 0m,
                IsActive = true
            });
        }

        // 3. Seed Mobile Fuel Bowser Truck Fleet (SOP-OPS-001 Certified)
        if (!await context.Vehicles.AnyAsync())
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            context.Vehicles.AddRange(
                new Vehicle
                {
                    RegistrationNumber = "DHAKA METRO-SHA 11-2045",
                    VehicleType = "bowser",
                    CapacityLitres = 3000.00m,
                    CompartmentCount = 2,
                    FitnessCertificateExpiry = today.AddYears(1),
                    LastInspectionDate = today.AddDays(-14),
                    Status = "active",
                    IsAvailable = true
                },
                new Vehicle
                {
                    RegistrationNumber = "DHAKA METRO-SHA 11-4090",
                    VehicleType = "bowser",
                    CapacityLitres = 5000.00m,
                    CompartmentCount = 3,
                    FitnessCertificateExpiry = today.AddYears(1),
                    LastInspectionDate = today.AddDays(-7),
                    Status = "active",
                    IsAvailable = true
                }
            );
        }

        // 4. Seed HAZMAT-Certified Fleet Drivers (SOP §5.2 / §10.3)
        if (!await context.Drivers.AnyAsync())
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            var driver1User = new User
            {
                FullName = "Kamal Hossain",
                Email = "kamal.driver@instafuel.com",
                Phone = "01811223344",
                Role = "driver",
                AccountType = "individual",
                IsActive = true
            };
            driver1User.PasswordHash = _hasher.HashPassword(driver1User, "Driver123!");

            var driver2User = new User
            {
                FullName = "Tariqul Islam",
                Email = "tariqul.driver@instafuel.com",
                Phone = "01899887766",
                Role = "driver",
                AccountType = "individual",
                IsActive = true
            };
            driver2User.PasswordHash = _hasher.HashPassword(driver2User, "Driver123!");

            context.Users.AddRange(driver1User, driver2User);

            context.Drivers.AddRange(
                new Driver
                {
                    User = driver1User,
                    LicenseNumber = "DL-DH-884920",
                    LicenseExpiry = today.AddYears(3),
                    DangerousGoodsCertified = true,
                    VerificationStatus = "verified",
                    EmploymentStatus = "active",
                    IsAvailable = true
                },
                new Driver
                {
                    User = driver2User,
                    LicenseNumber = "DL-DH-662914",
                    LicenseExpiry = today.AddYears(2),
                    DangerousGoodsCertified = true,
                    VerificationStatus = "verified",
                    EmploymentStatus = "active",
                    IsAvailable = true
                }
            );
        }

        await context.SaveChangesAsync();
    }
}
