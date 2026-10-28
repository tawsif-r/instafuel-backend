namespace Instafuel.Api.Modules.Suppliers;

public static class SupplierOrderWorkflow
{
    public static bool CanTransition(string current, string next) => (current, next) switch
    {
        ("pending", "accepted") => true,
        ("pending", "rejected") => true,
        ("accepted", "preparing") => true,
        ("preparing", "ready") => true,
        _ => false
    };
}
