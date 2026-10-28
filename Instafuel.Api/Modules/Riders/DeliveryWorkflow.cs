namespace Instafuel.Api.Modules.Riders;

public static class DeliveryWorkflow
{
    public static bool CanTransition(string current, string next) => (current, next) switch
    {
        ("assigned", "picked_up") => true,
        ("assigned", "failed") => true,
        ("picked_up", "in_transit") => true,
        ("picked_up", "failed") => true,
        ("in_transit", "delivered") => true,
        ("in_transit", "failed") => true,
        _ => false
    };
}
