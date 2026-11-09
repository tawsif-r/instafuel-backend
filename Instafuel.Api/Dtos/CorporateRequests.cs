using System.ComponentModel.DataAnnotations;

namespace Instafuel.Api.Dtos;

public record JoinCompanyRequest(
    [Required, MinLength(4)] string CompanyCode
);

public record CorporateAffiliationResponse(
    Guid Id,
    string CompanyName,
    string Role,
    string Status,
    decimal CreditLimit,
    decimal UsedCredit
);

