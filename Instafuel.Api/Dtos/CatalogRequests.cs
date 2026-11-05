using System.ComponentModel.DataAnnotations;

namespace Instafuel.Api.Dtos;

// Used for both create (POST) and update (PUT).
public record SaveCategoryRequest(
    [Required, MaxLength(100)] string Name,
    [Required, MaxLength(100)] string Slug,
    Guid? ParentId,
    bool IsActive = true);

public record SaveProductRequest(
    Guid CategoryId,
    [Required, MaxLength(150)] string Name,
    string? Description,
    [Required, AllowedValues("fuel", "general")] string ProductType,
    [Required, MaxLength(10)] string UnitCode,
    [MaxLength(50)] string? FuelGrade,
    bool IsActive = true);
