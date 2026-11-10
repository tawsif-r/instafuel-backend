namespace Instafuel.Api.Extensions;

// Where uploaded customer documents (vehicle papers, plate photos) are kept.
// Controllers only talk to this interface, so moving to S3 / Azure Blob later
// means writing one new implementation and changing the DI registration.
public interface IDocumentStorage
{
    Task<string> SaveAsync(Stream content, string extension, string folder, CancellationToken ct = default);
    Stream? OpenRead(string storageKey);
    void Delete(string storageKey);
}

// Stores files on the API server's disk under Storage:UploadsPath
// (default "uploads" next to the app). Fine for development and a single server.
public class LocalDocumentStorage : IDocumentStorage
{
    private readonly string _root;

    public LocalDocumentStorage(IConfiguration config, IWebHostEnvironment env)
    {
        var path = config.GetValue("Storage:UploadsPath", "uploads")!;
        _root = Path.GetFullPath(Path.IsPathRooted(path) ? path : Path.Combine(env.ContentRootPath, path));
        Directory.CreateDirectory(_root);
    }

    public async Task<string> SaveAsync(Stream content, string extension, string folder, CancellationToken ct = default)
    {
        // Random name: never trust the client's file name for paths.
        var key = $"{folder}/{Guid.NewGuid():N}{extension}";
        var fullPath = Resolve(key);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await using var file = File.Create(fullPath);
        await content.CopyToAsync(file, ct);
        return key;
    }

    public Stream? OpenRead(string storageKey)
    {
        var fullPath = Resolve(storageKey);
        return File.Exists(fullPath) ? File.OpenRead(fullPath) : null;
    }

    public void Delete(string storageKey)
    {
        var fullPath = Resolve(storageKey);
        if (File.Exists(fullPath)) File.Delete(fullPath);
    }

    // Keeps every key inside the uploads root (blocks "../" tricks).
    private string Resolve(string storageKey)
    {
        var fullPath = Path.GetFullPath(Path.Combine(_root, storageKey));
        if (!fullPath.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            throw new InvalidOperationException("Invalid storage key.");
        return fullPath;
    }
}

// PRODUCTION (later): S3-backed storage with private bucket + server-side encryption.
// public class S3DocumentStorage : IDocumentStorage
// {
//     private readonly IAmazonS3 _s3;
//     private readonly string _bucket;
//     public S3DocumentStorage(IAmazonS3 s3, IConfiguration config)
//     {
//         _s3 = s3;
//         _bucket = config["Storage:S3Bucket"]!;
//     }
//     public async Task<string> SaveAsync(Stream content, string extension, string folder, CancellationToken ct = default)
//     {
//         var key = $"{folder}/{Guid.NewGuid():N}{extension}";
//         await _s3.PutObjectAsync(new PutObjectRequest
//         {
//             BucketName = _bucket, Key = key, InputStream = content,
//             ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256
//         }, ct);
//         return key;
//     }
//     // OpenRead -> return a short-lived pre-signed URL instead of streaming through the API.
//     // Delete   -> _s3.DeleteObjectAsync(_bucket, key)
// }
