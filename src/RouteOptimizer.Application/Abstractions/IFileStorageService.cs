using RouteOptimizer.Domain.Common;

namespace RouteOptimizer.Application.Abstractions;

public interface IFileStorageService
{
    Task<Result<string>> UploadAsync(string bucketName, string objectName, Stream data, string contentType, CancellationToken ct);
    Task<Result<string>> GetPresignedUrlAsync(string bucketName, string objectName, int expirySeconds, CancellationToken ct);
    Task<Result<string>> GetPresignedUploadUrlAsync(string bucketName, string objectName, int expirySeconds, CancellationToken ct);
    Task<Result> DeleteAsync(string bucketName, string objectName, CancellationToken ct);
}
