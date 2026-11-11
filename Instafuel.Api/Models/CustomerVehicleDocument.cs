using System.Text.Json.Serialization;

namespace Instafuel.Api.Models;

// One document per kind per vehicle. The binary file lives in document storage
// (local disk for now, S3 later); this row keeps the metadata and storage key.
public class CustomerVehicleDocument
{
    public Guid Id { get; set; }
    public Guid CustomerVehicleId { get; set; }
    public string Kind { get; set; } = string.Empty; // registration_certificate | fitness_certificate | number_plate_photo

    // Name the customer sees (the original file name they picked).
    public string FileName { get; set; } = string.Empty;

    // Where the file is kept in document storage. Null for legacy rows that only had a name.
    [JsonIgnore] // internal path, never sent to clients
    public string? StorageKey { get; set; }

    public string? ContentType { get; set; } // application/pdf | image/jpeg | image/png ...
    public long? SizeBytes { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    // True once an actual file has been uploaded (not just a name).
    public bool HasFile => StorageKey is not null;

    [JsonIgnore]
    public CustomerVehicle CustomerVehicle { get; set; } = null!;
}
