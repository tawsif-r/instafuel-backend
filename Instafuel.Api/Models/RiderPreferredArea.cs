namespace Instafuel.Api.Models;

public class RiderPreferredArea
{
    public Guid Id { get; set; }
    public Guid RiderProfileId { get; set; }
    public string City { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public RiderProfile RiderProfile { get; set; } = null!;
}
