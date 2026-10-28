using Instafuel.Api.Modules.Riders;
using Instafuel.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Instafuel.Api.Tests;

public class RiderWorkflowTests
{
    [Theory]
    [InlineData("assigned", "picked_up", true)]
    [InlineData("assigned", "failed", true)]
    [InlineData("picked_up", "in_transit", true)]
    [InlineData("picked_up", "failed", true)]
    [InlineData("in_transit", "delivered", true)]
    [InlineData("in_transit", "failed", true)]
    [InlineData("pending", "delivered", false)]
    [InlineData("assigned", "delivered", false)]
    [InlineData("delivered", "in_transit", false)]
    [InlineData("failed", "delivered", false)]
    public void DeliveryWorkflow_OnlyAllowsValidTransitions(string current, string next, bool expected)
    {
        Assert.Equal(expected, DeliveryWorkflow.CanTransition(current, next));
    }

    [Fact]
    public void PickupSafetyChecklist_RequiresEveryItem()
    {
        var complete = new PickupSafetyChecklist(true, true, true, true, true, null);
        var incomplete = new PickupSafetyChecklist(true, true, false, true, true, null);

        Assert.True(complete.IsComplete);
        Assert.False(incomplete.IsComplete);
    }

    [Fact]
    public void RiderProfileAndDeliveryFields_AreMapped()
    {
        var options = new DbContextOptionsBuilder<PgDbContext>()
            .UseNpgsql("Host=localhost;Database=instafuel_model_only;Username=test;Password=test")
            .Options;
        using var context = new PgDbContext(options);

        var profile = context.Model.FindEntityType(typeof(RiderProfile))!;
        Assert.Equal("rider_profiles", profile.GetTableName());
        Assert.True(profile.FindProperty(nameof(RiderProfile.UserId))!.IsNullable == false);
        Assert.Contains(profile.GetIndexes(), index => index.IsUnique
            && index.Properties.Single().Name == nameof(RiderProfile.UserId));

        var delivery = context.Model.FindEntityType(typeof(Delivery))!;
        var table = StoreObjectIdentifier.Table("deliveries", null);
        Assert.Equal("proof_of_delivery_reference",
            delivery.FindProperty(nameof(Delivery.ProofOfDeliveryReference))!.GetColumnName(table));
        Assert.Equal("rider_latitude",
            delivery.FindProperty(nameof(Delivery.RiderLatitude))!.GetColumnName(table));
    }
}
