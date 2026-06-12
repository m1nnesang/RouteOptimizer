using Minio;
using Minio.DataModel.Args;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Domain.Common;

namespace RouteOptimizer.Infrastructure.Services;

public sealed class MinioFileStorageService : IFileStorageService
  {
      private readonly IMinioClient _client;

      public MinioFileStorageService(IMinioClient client) => _client = client;

      public async Task<Result<string>> UploadAsync(string bucketName, string objectName, Stream data, string contentType, CancellationToken ct)
      {
          try
          {
              var bucketExists = await _client.BucketExistsAsync(new BucketExistsArgs().WithBucket(bucketName), ct);
              if (!bucketExists)
                  await _client.MakeBucketAsync(new MakeBucketArgs().WithBucket(bucketName), ct);

              await _client.PutObjectAsync(new PutObjectArgs()
                  .WithBucket(bucketName)
                  .WithObject(objectName)
                  .WithStreamData(data)
                  .WithObjectSize(data.Length)
                  .WithContentType(contentType), ct);

              return Result<string>.Success(objectName);
          }
          catch (Exception ex)
          {
              return Result<string>.Failure($"Upload failed: {ex.Message}");
          }
      }

      public async Task<Result<string>> GetPresignedUrlAsync(string bucketName, string objectName, int expirySeconds, CancellationToken ct)
      {
          try
          {
              var url = await _client.PresignedGetObjectAsync(new PresignedGetObjectArgs()
                  .WithBucket(bucketName)
                  .WithObject(objectName)
                  .WithExpiry(expirySeconds));

              return Result<string>.Success(url);
          }
          catch (Exception ex)
          {
              return Result<string>.Failure($"Presigned URL failed: {ex.Message}");
          }
      }

      public async Task<Result<string>> GetPresignedUploadUrlAsync(string bucketName, string objectName, int expirySeconds, CancellationToken ct)
      {
          try
          {
              var bucketExists = await _client.BucketExistsAsync(new BucketExistsArgs().WithBucket(bucketName), ct);
              if (!bucketExists)
                  await _client.MakeBucketAsync(new MakeBucketArgs().WithBucket(bucketName), ct);

              var url = await _client.PresignedPutObjectAsync(new PresignedPutObjectArgs()
                  .WithBucket(bucketName)
                  .WithObject(objectName)
                  .WithExpiry(expirySeconds));

              return Result<string>.Success(url);
          }
          catch (Exception ex)
          {
              return Result<string>.Failure($"Presigned upload URL failed: {ex.Message}");
          }
      }

      public async Task<Result> DeleteAsync(string bucketName, string objectName, CancellationToken ct)
      {
          try
          {
              await _client.RemoveObjectAsync(new RemoveObjectArgs()
                  .WithBucket(bucketName)
                  .WithObject(objectName), ct);

              return Result.Success();
          }
          catch (Exception ex)
          {
              return Result.Failure($"Delete failed: {ex.Message}");
          }
      }
  }
