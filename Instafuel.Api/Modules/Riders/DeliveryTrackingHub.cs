using System.Security.Claims;
using Instafuel.Api.Authorization;
using Instafuel.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Instafuel.Api.Modules.Riders;

[Authorize]
public sealed class DeliveryTrackingHub(PgDbContext dbContext, IAuthorizationService authorizationService) : Hub
{
    public async Task JoinOrder(Guid orderId)
    {
        var user = Context.User;
        var userIdValue = user?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdValue, out var userId))
            throw new HubException("A valid user identity is required.");

        var isCustomer = (await authorizationService.AuthorizeAsync(user!, AuthorizationPolicies.Customer)).Succeeded
            && await dbContext.Orders.AnyAsync(order => order.Id == orderId && order.CustomerId == userId,
                Context.ConnectionAborted);
        var isAssignedRider = (await authorizationService.AuthorizeAsync(user!, AuthorizationPolicies.Rider)).Succeeded
            && await dbContext.Deliveries.AnyAsync(delivery => delivery.SupplierOrder.OrderId == orderId
                && delivery.DeliveryAgentId == userId, Context.ConnectionAborted);
        var isAdmin = (await authorizationService.AuthorizeAsync(user!, AuthorizationPolicies.Admin)).Succeeded;
        if (!isCustomer && !isAssignedRider && !isAdmin)
            throw new HubException("You are not allowed to track this order.");

        await Groups.AddToGroupAsync(Context.ConnectionId, OrderGroup(orderId), Context.ConnectionAborted);
    }

    public static string OrderGroup(Guid orderId) => $"order:{orderId:N}";
}
