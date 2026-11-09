using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
namespace SaveBite.Backend.Audit.Archives;

public sealed class ArchiveStorage : IDisposable
{
    private readonly AmazonS3Client client;
    private readonly string bucket;
    public ArchiveStorage(IConfiguration configuration)
    {
        var settings = configuration.GetSection("AuditArchive");
        bucket = settings["Bucket"] ?? "savebite-audit-archives";
        client = new AmazonS3Client(new BasicAWSCredentials(settings["AccessKey"] ?? "savebite_minio", settings["SecretKey"] ?? "savebite_minio_local_password"),
            new AmazonS3Config { ServiceURL = settings["Endpoint"] ?? "http://localhost:9000", ForcePathStyle = true, AuthenticationRegion = "us-east-1" });
    }
    public async Task UploadAsync(string key, string file, CancellationToken ct)
    {
        try { await client.PutBucketAsync(new PutBucketRequest { BucketName = bucket, UseClientRegion = true }, ct); }
        catch (AmazonS3Exception e) when (e.ErrorCode is "BucketAlreadyOwnedByYou" or "BucketAlreadyExists") { }
        await client.PutObjectAsync(new PutObjectRequest { BucketName = bucket, Key = key, FilePath = file, ContentType = key.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) ? "application/zip" : "application/gzip" }, ct);
    }
    public async Task DownloadAsync(string key, string file, CancellationToken ct)
    {
        using var response = await client.GetObjectAsync(bucket, key, ct);
        await using var target = File.Create(file);
        await response.ResponseStream.CopyToAsync(target, ct);
    }
    public static bool IsMissing(AmazonS3Exception error) =>
        error.StatusCode == System.Net.HttpStatusCode.NotFound || error.ErrorCode is "NoSuchKey" or "NoSuchBucket";

    public async Task<bool> ExistsAsync(string key, CancellationToken ct)
    {
        try
        {
            await client.GetObjectMetadataAsync(bucket, key, ct);
            return true;
        }
        catch (AmazonS3Exception error) when (IsMissing(error)) { return false; }
    }
    public Task DeleteAsync(string key, CancellationToken ct) => client.DeleteObjectAsync(bucket, key, ct);
    public void Dispose() => client.Dispose();
}
