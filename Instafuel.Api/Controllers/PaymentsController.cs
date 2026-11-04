using Instafuel.Api.Dtos;
using Instafuel.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Instafuel.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly PgDbContext _context;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(PgDbContext context, ILogger<PaymentsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET api/payments?orderId=...
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Payment>>> GetPayments([FromQuery] Guid? orderId)
    {
        try
        {
            var query = _context.Payments.AsNoTracking();
            if (orderId is not null)
                query = query.Where(p => p.OrderId == orderId);

            var payments = await query.OrderByDescending(p => p.CreatedAt).ToListAsync();
            return Ok(payments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve payments. Operation: {Operation}", nameof(GetPayments));
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving payments." });
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Payment>> GetPayment(Guid id)
    {
        try
        {
            var payment = await _context.Payments.AsNoTracking()
                .Include(p => p.Refunds)
                .FirstOrDefaultAsync(p => p.Id == id);
            return payment is null ? NotFound() : Ok(payment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve payment {PaymentId}. Operation: {Operation}", id, nameof(GetPayment));
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving the payment." });
        }
    }

    [HttpPost]
    public async Task<ActionResult<Payment>> CreatePayment(CreatePaymentRequest request)
    {
        try
        {
            // Same idempotency key = client retried the same request, so return the original payment.
            var existing = await _context.Payments.AsNoTracking()
                .FirstOrDefaultAsync(p => p.IdempotencyKey == request.IdempotencyKey);
            if (existing is not null) return Ok(existing);

            var order = await _context.Orders.FindAsync(request.OrderId);
            if (order is null) return BadRequest(new { message = "Order not found." });
            if (order.Status == "cancelled") return BadRequest(new { message = "Order is cancelled." });

            var payment = new Payment
            {
                OrderId = order.Id,
                Amount = request.Amount,
                Currency = order.Currency,
                Method = request.Method,
                IdempotencyKey = request.IdempotencyKey,
                ProviderReference = request.ProviderReference
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetPayment), new { id = payment.Id }, payment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create payment. Operation: {Operation}", nameof(CreatePayment));
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while creating the payment." });
        }
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<Payment>> UpdatePaymentStatus(Guid id, UpdatePaymentStatusRequest request)
    {
        try
        {
            var payment = await _context.Payments.FindAsync(id);
            if (payment is null) return NotFound();

            if (payment.Status != "pending")
                return BadRequest(new { message = $"Payment is already {payment.Status}." });

            payment.Status = request.Status;
            payment.ProviderReference = request.ProviderReference ?? payment.ProviderReference;
            if (request.Status == "successful")
                payment.PaidAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(payment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update status of payment {PaymentId}. Operation: {Operation}", id, nameof(UpdatePaymentStatus));
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while updating the payment status." });
        }
    }
}
