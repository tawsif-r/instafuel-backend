using Instafuel.Api.Modules.Suppliers;

namespace Instafuel.Api.Tests;

public class SupplierWorkflowTests
{
    [Theory]
    [InlineData("pending", "accepted", true)]
    [InlineData("pending", "rejected", true)]
    [InlineData("accepted", "preparing", true)]
    [InlineData("preparing", "ready", true)]
    [InlineData("pending", "ready", false)]
    [InlineData("accepted", "rejected", false)]
    [InlineData("ready", "dispatched", false)]
    [InlineData("rejected", "accepted", false)]
    public void SupplierOrderTransitionRequiresTheExpectedCurrentState(string current, string next, bool expected)
    {
        Assert.Equal(expected, SupplierOrderWorkflow.CanTransition(current, next));
    }
}
