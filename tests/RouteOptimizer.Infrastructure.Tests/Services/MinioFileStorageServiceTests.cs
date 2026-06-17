using FluentAssertions;
using Minio;
using Minio.DataModel.Args;
using Moq;
using RouteOptimizer.Infrastructure.Services;

namespace RouteOptimizer.Infrastructure.Tests.Services;

public class MinioFileStorageServiceTests
{
    private readonly Mock<IMinioClient> _minio = new();
    private readonly MinioFileStorageService _sut;

    public MinioFileStorageServiceTests() =>
        _sut = new MinioFileStorageService(_minio.Object);

    [Fact]
    public async Task GetPresignedUrlAsync_ReturnsSuccessWithUrl()
    {
        _minio
            .Setup(c => c.PresignedGetObjectAsync(It.IsAny<PresignedGetObjectArgs>()))
            .ReturnsAsync("https://minio/bucket/object?token=abc");

        var result = await _sut.GetPresignedUrlAsync("bucket", "object", 3600, default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("https://minio/bucket/object?token=abc");
    }

    [Fact]
    public async Task GetPresignedUrlAsync_MinioThrows_ReturnsFailure()
    {
        _minio
            .Setup(c => c.PresignedGetObjectAsync(It.IsAny<PresignedGetObjectArgs>()))
            .ThrowsAsync(new Exception("connection refused"));

        var result = await _sut.GetPresignedUrlAsync("bucket", "object", 3600, default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("connection refused");
    }

    [Fact]
    public async Task GetPresignedUploadUrlAsync_BucketNotExists_CreatesBucketFirst()
    {
        _minio.Setup(c => c.BucketExistsAsync(It.IsAny<BucketExistsArgs>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _minio.Setup(c => c.MakeBucketAsync(It.IsAny<MakeBucketArgs>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _minio.Setup(c => c.PresignedPutObjectAsync(It.IsAny<PresignedPutObjectArgs>()))
            .ReturnsAsync("https://minio/upload-url");

        var result = await _sut.GetPresignedUploadUrlAsync("bucket", "photo.jpg", 3600, default);

        result.IsSuccess.Should().BeTrue();
        _minio.Verify(c => c.MakeBucketAsync(It.IsAny<MakeBucketArgs>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetPresignedUploadUrlAsync_BucketExists_SkipsMakeBucket()
    {
        _minio.Setup(c => c.BucketExistsAsync(It.IsAny<BucketExistsArgs>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _minio.Setup(c => c.PresignedPutObjectAsync(It.IsAny<PresignedPutObjectArgs>()))
            .ReturnsAsync("https://minio/upload-url");

        await _sut.GetPresignedUploadUrlAsync("bucket", "photo.jpg", 3600, default);

        _minio.Verify(c => c.MakeBucketAsync(It.IsAny<MakeBucketArgs>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_CallsRemoveObject_ReturnsSuccess()
    {
        _minio.Setup(c => c.RemoveObjectAsync(It.IsAny<RemoveObjectArgs>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.DeleteAsync("bucket", "object", default);

        result.IsSuccess.Should().BeTrue();
        _minio.Verify(c => c.RemoveObjectAsync(It.IsAny<RemoveObjectArgs>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_MinioThrows_ReturnsFailure()
    {
        _minio.Setup(c => c.RemoveObjectAsync(It.IsAny<RemoveObjectArgs>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("network error"));

        var result = await _sut.DeleteAsync("bucket", "object", default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("network error");
    }
}
