using System.Text.RegularExpressions;
using Instafuel.Api.Dtos;
using Instafuel.Api.Extensions;
using Instafuel.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Instafuel.Api.Controllers;

// The logged-in customer's own vehicles. Every query is filtered by the user id
// from the token, so another customer's vehicle id simply returns 404.
[ApiController]
[Authorize]
[Route("api/me/vehicles")]
public class MyVehiclesController : ControllerBase
{
    // A vehicle needs all of these before it can be verified.
    private static readonly string[] RequiredDocumentKinds =
        ["registration_certificate", "fitness_certificate", "number_plate_photo"];

    private readonly PgDbContext _context;
    private readonly IConfiguration _config;
    private readonly ILogger<MyVehiclesController> _logger;
    private readonly IDocumentStorage _storage;

    public MyVehiclesController(PgDbContext context, IConfiguration config, ILogger<MyVehiclesController> logger, IDocumentStorage storage)
    {
        _context = context;
        _config = config;
        _logger = logger;
        _storage = storage;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CustomerVehicle>>> GetMyVehicles()
    {
        try
        {
            var userId = User.GetUserId();
            var vehicles = await _context.CustomerVehicles.AsNoTracking()
                .Include(v => v.Documents)
                .Where(v => v.UserId == userId && v.IsActive)
                .OrderBy(v => v.CreatedAt)
                .ToListAsync();
            return Ok(vehicles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve customer vehicles. Operation: {Operation}", nameof(GetMyVehicles));
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving your vehicles." });
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CustomerVehicle>> GetMyVehicle(Guid id)
    {
        try
        {
            var vehicle = await MyVehicles().AsNoTracking().FirstOrDefaultAsync(v => v.Id == id);
            return vehicle is null ? NotFound() : Ok(vehicle);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve customer vehicle {VehicleId}. Operation: {Operation}", id, nameof(GetMyVehicle));
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving the vehicle." });
        }
    }

    [HttpPost]
    public async Task<ActionResult<CustomerVehicle>> CreateMyVehicle(SaveCustomerVehicleRequest request)
    {
        try
        {
            var plate = NormalizePlate(request.RegistrationNumber);
            if (await PlateTaken(plate, null))
                return Conflict(new { message = "This number plate is already registered." });

            var vehicle = new CustomerVehicle
            {
                UserId = User.GetUserId(),
                Nickname = request.Nickname.Trim(),
                VehicleType = request.VehicleType,
                RegistrationNumber = plate,
                FuelType = request.FuelType
            };
            ApplyDocuments(vehicle, request.Documents);
            ResetVerification(vehicle);

            _context.CustomerVehicles.Add(vehicle);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetMyVehicle), new { id = vehicle.Id }, vehicle);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create customer vehicle. Operation: {Operation}", nameof(CreateMyVehicle));
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while saving the vehicle." });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CustomerVehicle>> UpdateMyVehicle(Guid id, SaveCustomerVehicleRequest request)
    {
        try
        {
            var vehicle = await MyVehicles().FirstOrDefaultAsync(v => v.Id == id);
            if (vehicle is null) return NotFound();

            var plate = NormalizePlate(request.RegistrationNumber);
            if (await PlateTaken(plate, id))
                return Conflict(new { message = "This number plate is already registered." });

            // A new plate or new documents need a fresh check. Nickname/fuel changes don't.
            var needsReview = vehicle.RegistrationNumber != plate;
            needsReview |= ApplyDocuments(vehicle, request.Documents);

            vehicle.Nickname = request.Nickname.Trim();
            vehicle.VehicleType = request.VehicleType;
            vehicle.RegistrationNumber = plate;
            vehicle.FuelType = request.FuelType;
            vehicle.UpdatedAt = DateTime.UtcNow;
            if (needsReview) ResetVerification(vehicle);

            await _context.SaveChangesAsync();
            return Ok(vehicle);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update customer vehicle {VehicleId}. Operation: {Operation}", id, nameof(UpdateMyVehicle));
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while saving the vehicle." });
        }
    }

    // Soft delete, so past orders can still show the vehicle.
    // TODO: block this while the vehicle is in an active order (once orders link to customer vehicles).
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteMyVehicle(Guid id)
    {
        try
        {
            var vehicle = await MyVehicles().FirstOrDefaultAsync(v => v.Id == id);
            if (vehicle is null) return NotFound();

            vehicle.IsActive = false;
            vehicle.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete customer vehicle {VehicleId}. Operation: {Operation}", id, nameof(DeleteMyVehicle));
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while deleting the vehicle." });
        }
    }

    // ---------------------------------------------------------------------
    // Document files (registration / fitness certificate, number plate photo)
    // ---------------------------------------------------------------------

    // Uploads (or replaces) one document of a vehicle. multipart/form-data, field "file".
    // A new document sends the vehicle back for verification.
    [HttpPost("{id:guid}/documents/{kind}")]
    [RequestSizeLimit(MaxDocumentBytes + 1024 * 1024)] // + room for the multipart envelope
    public async Task<ActionResult<CustomerVehicle>> UploadDocument(Guid id, string kind, IFormFile? file, CancellationToken ct)
    {
        try
        {
            if (!RequiredDocumentKinds.Contains(kind))
                return BadRequest(new { message = "Unknown document type." });

            var vehicle = await MyVehicles().FirstOrDefaultAsync(v => v.Id == id, ct);
            if (vehicle is null) return NotFound();

            var error = ValidateDocumentFile(kind, file);
            if (error is not null) return BadRequest(new { message = error });

            var extension = Path.GetExtension(file!.FileName).ToLowerInvariant();

            // PRODUCTION (later): real document checks before accepting the file.
            //  1. Magic-byte check, so a renamed .exe can't pass as .pdf:
            //       await using var head = file.OpenReadStream();
            //       if (!await FileSignatures.MatchesAsync(head, extension, ct))
            //           return BadRequest(new { message = "The file content does not match its type." });
            //  2. Antivirus scan (ClamAV / cloud scanner) before storing:
            //       if (!await _virusScanner.IsCleanAsync(file.OpenReadStream(), ct))
            //           return BadRequest(new { message = "The file failed the security scan." });
            //  3. Number plate photo: OCR the plate and compare with vehicle.RegistrationNumber:
            //       var plateText = await _plateOcr.ReadPlateAsync(file.OpenReadStream(), ct);
            //       if (NormalizePlate(plateText) != vehicle.RegistrationNumber)
            //           return BadRequest(new { message = "The plate in the photo does not match the registration number." });
            //  4. Registration certificate: confirm with the BRTA vehicle registry API:
            //       if (!await _brta.IsRegisteredAsync(vehicle.RegistrationNumber, ct))
            //           return BadRequest(new { message = "BRTA has no record of this registration number." });
            //  5. Fitness certificate: read the expiry date and reject expired certificates.

            var storageKey = await _storage.SaveAsync(file.OpenReadStream(), extension, $"vehicle-docs/{vehicle.UserId:N}/{vehicle.Id:N}", ct);

            var existing = vehicle.Documents.FirstOrDefault(d => d.Kind == kind);
            var oldKey = existing?.StorageKey;
            if (existing is null)
            {
                existing = new CustomerVehicleDocument { Kind = kind };
                vehicle.Documents.Add(existing);
            }
            existing.FileName = Path.GetFileName(file.FileName).Trim();
            existing.StorageKey = storageKey;
            existing.ContentType = ContentTypeFor(extension);
            existing.SizeBytes = file.Length;
            existing.UploadedAt = DateTime.UtcNow;

            vehicle.UpdatedAt = DateTime.UtcNow;
            ResetVerification(vehicle);

            await _context.SaveChangesAsync(ct);
            if (oldKey is not null) _storage.Delete(oldKey); // replaced: drop the old file
            return Ok(vehicle);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload {Kind} for customer vehicle {VehicleId}. Operation: {Operation}", kind, id, nameof(UploadDocument));
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while uploading the document." });
        }
    }

    // Streams the uploaded file back to its owner (for preview in the app).
    [HttpGet("{id:guid}/documents/{kind}/file")]
    public async Task<IActionResult> DownloadDocument(Guid id, string kind)
    {
        try
        {
            var vehicle = await MyVehicles().AsNoTracking().FirstOrDefaultAsync(v => v.Id == id);
            var doc = vehicle?.Documents.FirstOrDefault(d => d.Kind == kind);
            if (doc?.StorageKey is null) return NotFound();

            var stream = _storage.OpenRead(doc.StorageKey);
            if (stream is null) return NotFound(new { message = "The file is no longer available." });

            return File(stream, doc.ContentType ?? "application/octet-stream", doc.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download {Kind} of customer vehicle {VehicleId}. Operation: {Operation}", kind, id, nameof(DownloadDocument));
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while loading the document." });
        }
    }

    [HttpDelete("{id:guid}/documents/{kind}")]
    public async Task<ActionResult<CustomerVehicle>> DeleteDocument(Guid id, string kind)
    {
        try
        {
            var vehicle = await MyVehicles().FirstOrDefaultAsync(v => v.Id == id);
            if (vehicle is null) return NotFound();

            var doc = vehicle.Documents.FirstOrDefault(d => d.Kind == kind);
            if (doc is null) return Ok(vehicle);

            var key = doc.StorageKey;
            vehicle.Documents.Remove(doc);
            _context.CustomerVehicleDocuments.Remove(doc);
            vehicle.UpdatedAt = DateTime.UtcNow;
            ResetVerification(vehicle);

            await _context.SaveChangesAsync();
            if (key is not null) _storage.Delete(key);
            return Ok(vehicle);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete {Kind} of customer vehicle {VehicleId}. Operation: {Operation}", kind, id, nameof(DeleteDocument));
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while removing the document." });
        }
    }

    private const long MaxDocumentBytes = 15 * 1024 * 1024; // 15 MB

    private static readonly string[] PdfExtensions = [".pdf"];
    private static readonly string[] ImageExtensions = [".jpg", ".jpeg", ".png", ".webp", ".heic"];

    // TESTING MODE: any PDF or image is accepted for every document kind.
    private static string? ValidateDocumentFile(string kind, IFormFile? file)
    {
        if (file is null || file.Length == 0) return "Please choose a file to upload.";
        if (file.Length > MaxDocumentBytes) return "The file is too large (max 15 MB).";

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!PdfExtensions.Contains(extension) && !ImageExtensions.Contains(extension))
            return "Only PDF or image files (JPG, PNG, WEBP, HEIC) are allowed.";

        // PRODUCTION (later): enforce the right format per document kind.
        // if (kind == "number_plate_photo" && !ImageExtensions.Contains(extension))
        //     return "The number plate must be a photo (JPG or PNG).";
        // if (kind is "registration_certificate" or "fitness_certificate" && !PdfExtensions.Contains(extension))
        //     return "Certificates must be uploaded as PDF.";
        _ = kind;
        return null;
    }

    private static string ContentTypeFor(string extension) => extension switch
    {
        ".pdf" => "application/pdf",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png" => "image/png",
        ".webp" => "image/webp",
        ".heic" => "image/heic",
        _ => "application/octet-stream",
    };

    // Active vehicles of the logged-in user, with documents.
    private IQueryable<CustomerVehicle> MyVehicles()
    {
        var userId = User.GetUserId();
        return _context.CustomerVehicles.Include(v => v.Documents).Where(v => v.UserId == userId && v.IsActive);
    }

    // "dhaka metro-ga  12-3456 " -> "DHAKA METRO-GA 12-3456"
    private static string NormalizePlate(string plate) =>
        Regex.Replace(plate.Trim(), @"\s+", " ").ToUpperInvariant();

    // excludeVehicleId lets an update keep its own plate.
    private Task<bool> PlateTaken(string plate, Guid? excludeVehicleId) =>
        _context.CustomerVehicles.AnyAsync(v =>
            v.IsActive && v.RegistrationNumber == plate && v.Id != excludeVehicleId);

    // Makes the vehicle's documents match the request (one per kind).
    // Returns true when anything was added, replaced or removed.
    // Null = leave documents as they are (the app uploads files through the documents endpoints).
    private bool ApplyDocuments(CustomerVehicle vehicle, List<VehicleDocumentRequest>? documents)
    {
        if (documents is null) return false;
        var wanted = documents
            .GroupBy(d => d.Kind)
            .ToDictionary(g => g.Key, g => g.Last().FileName.Trim());
        var changed = false;

        foreach (var doc in vehicle.Documents.Where(d => !wanted.ContainsKey(d.Kind)).ToList())
        {
            vehicle.Documents.Remove(doc);
            _context.CustomerVehicleDocuments.Remove(doc);
            changed = true;
        }

        foreach (var (kind, fileName) in wanted)
        {
            var existing = vehicle.Documents.FirstOrDefault(d => d.Kind == kind);
            if (existing is null)
            {
                vehicle.Documents.Add(new CustomerVehicleDocument { Kind = kind, FileName = fileName });
                changed = true;
            }
            else if (existing.FileName != fileName)
            {
                existing.FileName = fileName;
                existing.UploadedAt = DateTime.UtcNow;
                changed = true;
            }
        }
        return changed;
    }

    // Back to "pending" for an admin to review. In Development,
    // CustomerVehicles:AutoVerifyWhenComplete verifies complete vehicles straight away
    // so the order flow can be tried without an admin.
    private void ResetVerification(CustomerVehicle vehicle)
    {
        var complete = RequiredDocumentKinds.All(k => vehicle.Documents.Any(d => d.Kind == k));
        var autoVerify = complete && _config.GetValue("CustomerVehicles:AutoVerifyWhenComplete", false);

        vehicle.VerificationStatus = autoVerify ? "verified" : "pending";
        vehicle.VerifiedAt = autoVerify ? DateTime.UtcNow : null;
        vehicle.RejectionReason = null;
    }
}
