namespace Instafuel.Api.Models;

public static class RoleNames
{
    public const string Customer = "customer";
    public const string Rider = "rider";
    public const string SupplierMember = "supplier_member";
    public const string SectorSupervisor = "sector_supervisor";
    public const string Admin = "admin";
    public const string SuperAdmin = "super_admin";

    public static readonly IReadOnlyList<string> All =
    [
        Customer,
        Rider,
        SupplierMember,
        SectorSupervisor,
        Admin,
        SuperAdmin
    ];
}

