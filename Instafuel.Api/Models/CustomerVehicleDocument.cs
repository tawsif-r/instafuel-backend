using System.Text.Json.Serialization;

namespace Instafuel.Api.Models;

// One document per kind per vehicle. Only the file name is stored for now;
// once uploads (S3) exist, FileName becomes the storage key.
public class CustomerVehicleDocument
{
    public Guid Id { get; set; }
    public Guid CustomerVehicleId { get; set; }
    public string Kind { get; set; } = string.Empty; // registration_certificate | fitness_certificate | number_plate_photo
    public string FileName { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    [JsonIgnore]
    public CustomerVehicle CustomerVehicle { get; set; } = null!;
}
