using Instafuel.Api.Dtos;
using Instafuel.Api.Extensions;
using Instafuel.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Instafuel.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/me/corporate")]
public class MyCorporateController : ControllerBase
{
    private readonly PgDbContext _context;
    private readonly ILogger<MyCorporateController> _logger;

    public MyCorporateController(PgDbContext context, ILogger<MyCorporateController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CorporateAffiliationResponse>>> GetMyAffiliations()
    {
        try
        {
            var userId = User.GetUserId();
            var memberships = await _context.CompanyMembers
                .AsNoTracking()
                .Include(m => m.Company)
                .Where(m => m.UserId == userId && m.Company.IsActive)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();

            var response = memberships.Select(m => new CorporateAffiliationResponse(
                m.Id,
                m.Company.Name,
                m.Role,
                m.Status,
                m.CreditLimit,
                m.UsedCredit
            ));

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve corporate affiliations. Operation: {Operation}", nameof(GetMyAffiliations));
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving company links." });
        }
    }

    [HttpPost("join")]
    public async Task<ActionResult<CorporateAffiliationResponse>> JoinCompany(JoinCompanyRequest request)
    {
        try
        {
            var userId = User.GetUserId();
            var code = request.CompanyCode.Trim().ToUpperInvariant();

            var company = await _context.Companies
                .FirstOrDefaultAsync(c => c.Code.ToUpper() == code && c.IsActive);

            if (company is null)
            {
                // If code starts with DEMO, auto-create a demo company so local testing is easy
                if (code.StartsWith("DEMO"))
                {
                    company = new Company
                    {
                        Name = $"Demo Company {code}",
                        Code = code,
                        CreditLimit = 50000m,
                        UsedCredit = 0m,
                        IsActive = true
                    };
                    _context.Companies.Add(company);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    return NotFound(new { message = "No active company found with this code." });
                }
            }

            var existing = await _context.CompanyMembers
                .Include(m => m.Company)
                .FirstOrDefaultAsync(m => m.CompanyId == company.Id && m.UserId == userId);

            if (existing is not null)
            {
                return Ok(new CorporateAffiliationResponse(
                    existing.Id,
                    existing.Company.Name,
                    existing.Role,
                    existing.Status,
                    existing.CreditLimit,
                    existing.UsedCredit
                ));
            }

            // Demo codes or auto-approval: codes starting with DEMO or NORTHWIND are active immediately
            var isInstant = code.StartsWith("DEMO") || code == "NORTHWIND";
            var member = new CompanyMember
            {
                CompanyId = company.Id,
                UserId = userId,
                Role = "member",
                Status = isInstant ? "active" : "pending",
                CreditLimit = isInstant ? 25000m : 0m,
                UsedCredit = 0m
            };

            _context.CompanyMembers.Add(member);
            await _context.SaveChangesAsync();

            return Ok(new CorporateAffiliationResponse(
                member.Id,
                company.Name,
                member.Role,
                member.Status,
                member.CreditLimit,
                member.UsedCredit
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to join company with code {CompanyCode}. Operation: {Operation}", request.CompanyCode, nameof(JoinCompany));
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while joining the company." });
        }
    }
}

