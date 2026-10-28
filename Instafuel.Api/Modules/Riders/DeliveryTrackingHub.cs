using System.Security.Claims;
using Instafuel.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Instafuel.Api.Modules.Riders;

[Authorize]
public sealed class DeliveryTrackingHub(PgDbContext dbContext) : Hub
{
    public async Task JoinOrder(Guid orderId)
    {
        var user = Context.User;
        var userIdValue = user?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdValue, out var userId))
            throw new HubException("A valid user identity is required.");

        var isCustomer = user!.IsInRole(RoleNames.Customer)
            && await dbContext.Orders.AnyAsync(order => order.Id == orderId && order.CustomerId == userId,
                Context.ConnectionAborted);
        var isAssignedRider = user.IsInRole(RoleNames.Rider)
            && await dbContext.Deliveries.AnyAsync(delivery => delivery.SupplierOrder.OrderId == orderId
                && delivery.DeliveryAgentId == userId, Context.ConnectionAborted);
        var isAdmin = user.IsInRole(RoleNames.Admin) || user.IsInRole(RoleNames.SuperAdmin);
        if (!isCustomer && !isAssignedRider && !isAdmin)
            throw new HubException("You are not allowed to track this order.");

        await Groups.AddToGroupAsync(Context.ConnectionId, OrderGroup(orderId), Context.ConnectionAborted);
    }

    public static string OrderGroup(Guid orderId) => $"order:{orderId:N}";
}
