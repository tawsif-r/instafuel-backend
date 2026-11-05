using System.ComponentModel.DataAnnotations;

namespace Instafuel.Api.Dtos;

public record SaveSupplierRequest(
    [Required, MaxLength(200)] string LegalName,
    [Required, MaxLength(200)] string DisplayName);

public record UpdateSupplierStatusRequest(
    [Required, AllowedValues("pending", "active", "suspended", "rejected")] string Status);
