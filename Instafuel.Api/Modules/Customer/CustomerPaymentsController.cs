using System.ComponentModel.DataAnnotations;
using Instafuel.Api.Controllers;
using Instafuel.Api.Exceptions;
using Instafuel.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Instafuel.Api.Modules.Customer;

[Authorize(Roles = RoleNames.Customer)]
[Route("api/v1/customer")]
public sealed class CustomerPaymentsController(PgDbContext dbContext) : BaseApiController
{
    // Creates a cash-on-delivery payment intent. Online providers need provider-specific
    // checkout/callback integrations before they can be safely exposed here.
    [HttpPost("orders/{orderId:guid}/payments/cash")]
    public async Task<ActionResult<CustomerPaymentResponse>> CreateCashPayment(
        Guid orderId, CreateCashPaymentRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.IdempotencyKey) || request.IdempotencyKey.Length > 200)
            throw new AppValidationException(nameof(request.IdempotencyKey), "An idempotency key up to 200 characters is required.");

        var userId = CustomerControllerSupport.GetUserId(User);
        var idempotencyKey = request.IdempotencyKey.Trim();
        var order = await dbContext.Orders.SingleOrDefaultAsync(
            item => item.Id == orderId && item.CustomerId == userId, cancellationToken);
        if (order is null) return NotFound();
        if (order.Status is "cancelled" or "failed")
            throw new ConflictException("Payment cannot be created for a cancelled or failed order.");

        var existing = await dbContext.Payments.AsNoTracking()
            .SingleOrDefaultAsync(payment => payment.IdempotencyKey == idempotencyKey, cancellationToken);
        if (existing is not null)
        {
            if (existing.OrderId != order.Id || existing.Method != "cash")
                throw new ConflictException("This idempotency key has already been used.");
            return Ok(new CustomerPaymentResponse(existing.Id, existing.OrderId, existing.Amount,
                existing.Currency, existing.Method, existing.Status, existing.CreatedAt));
        }

        var activeCashPayment = await dbContext.Payments.AsNoTracking().SingleOrDefaultAsync(payment =>
            payment.OrderId == order.Id && payment.Method == "cash"
                && (payment.Status == "pending" || payment.Status == "successful"), cancellationToken);
        if (activeCashPayment is not null)
            return Ok(ToResponse(activeCashPayment));

        if (order.TotalAmount <= 0) throw new ConflictException("This order does not have an amount due.");
        var payment = new Payment
        {
            OrderId = order.Id,
            Amount = order.TotalAmount,
            Currency = order.Currency,
            Method = "cash",
            Status = "pending",
            IdempotencyKey = idempotencyKey
        };
        dbContext.Payments.Add(payment);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation
        })
        {
            dbContext.Entry(payment).State = EntityState.Detached;
            var concurrentPayment = await dbContext.Payments.AsNoTracking()
                .SingleOrDefaultAsync(item => item.IdempotencyKey == idempotencyKey, cancellationToken);
            if (concurrentPayment is null || concurrentPayment.OrderId != order.Id || concurrentPayment.Method != "cash")
                throw new ConflictException("This idempotency key has already been used.");
            return Ok(ToResponse(concurrentPayment));
        }
        return CreatedAtAction(nameof(GetOrderPayments), new { orderId },
            ToResponse(payment));
    }

    [HttpGet("orders/{orderId:guid}/payments")]
    public async Task<ActionResult<IReadOnlyList<CustomerPaymentResponse>>> GetOrderPayments(
        Guid orderId, CancellationToken cancellationToken)
    {
        var userId = CustomerControllerSupport.GetUserId(User);
        var ownsOrder = await dbContext.Orders.AnyAsync(
            order => order.Id == orderId && order.CustomerId == userId, cancellationToken);
        if (!ownsOrder) return NotFound();
        var payments = await dbContext.Payments.AsNoTracking()
            .Where(payment => payment.OrderId == orderId)
            .OrderByDescending(payment => payment.CreatedAt)
            .Select(payment => new CustomerPaymentResponse(payment.Id, payment.OrderId,
                payment.Amount, payment.Currency, payment.Method, payment.Status, payment.CreatedAt))
            .ToListAsync(cancellationToken);
        return Ok(payments);
    }

    private static CustomerPaymentResponse ToResponse(Payment payment) => new(
        payment.Id, payment.OrderId, payment.Amount, payment.Currency, payment.Method, payment.Status, payment.CreatedAt);
}

public sealed record CreateCashPaymentRequest([property: Required, StringLength(200, MinimumLength = 1)] string IdempotencyKey);
public sealed record CustomerPaymentResponse(Guid Id, Guid OrderId, decimal Amount, string Currency, string Method, string Status, DateTime CreatedAt);
