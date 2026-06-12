using FluentAssertions;
using Moq;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Application.DeliveryPhotos;
using RouteOptimizer.Application.DeliveryPhotos.GetPhotoUrl;
using RouteOptimizer.Domain.Common;

namespace RouteOptimizer.Application.Tests.DeliveryPhotos;

public class GetDeliveryPhotoUrlQueryHandlerTests
{
    private readonly Mock<IFileStorageService> _fileStorage = new();
    private readonly GetDeliveryPhotoUrlQueryHandler _handler;

    public GetDeliveryPhotoUrlQueryHandlerTests()
    {
        _handler = new GetDeliveryPhotoUrlQueryHandler(_fileStorage.Object);
    }

    [Fact]
    public async Task Handle_StorageSucceeds_ReturnsPresignedUrl()
    {
        const string photoKey = "abc123.jpg";
        const string url = "http://localhost:9000/delivery-photos/abc123.jpg";
        _fileStorage
            .Setup(x => x.GetPresignedUrlAsync(
                DeliveryPhotoStorage.BucketName,
                photoKey,
                DeliveryPhotoStorage.DownloadUrlExpirySeconds,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<string>.Success(url));

        var result = await _handler.Handle(new GetDeliveryPhotoUrlQuery(photoKey), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(url);
    }

    [Fact]
    public async Task Handle_StorageFails_ReturnsFailure()
    {
        _fileStorage
            .Setup(x => x.GetPresignedUrlAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<string>.Failure("Presigned URL failed: object not found"));

        var result = await _handler.Handle(new GetDeliveryPhotoUrlQuery("missing.jpg"), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Presigned URL failed");
    }
}
