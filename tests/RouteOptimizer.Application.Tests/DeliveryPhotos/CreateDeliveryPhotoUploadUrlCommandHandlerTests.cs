using FluentAssertions;
using Moq;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Application.DeliveryPhotos;
using RouteOptimizer.Application.DeliveryPhotos.CreateUploadUrl;
using RouteOptimizer.Domain.Common;

namespace RouteOptimizer.Application.Tests.DeliveryPhotos;

public class CreateDeliveryPhotoUploadUrlCommandHandlerTests
{
    private readonly Mock<IFileStorageService> _fileStorage = new();
    private readonly CreateDeliveryPhotoUploadUrlCommandHandler _handler;

    public CreateDeliveryPhotoUploadUrlCommandHandlerTests()
    {
        _handler = new CreateDeliveryPhotoUploadUrlCommandHandler(_fileStorage.Object);
    }

    [Fact]
    public async Task Handle_StorageSucceeds_ReturnsKeyAndUploadUrl()
    {
        const string uploadUrl = "http://localhost:9000/delivery-photos/upload";
        _fileStorage
            .Setup(x => x.GetPresignedUploadUrlAsync(
                DeliveryPhotoStorage.BucketName,
                It.IsAny<string>(),
                DeliveryPhotoStorage.UploadUrlExpirySeconds,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<string>.Success(uploadUrl));

        var result = await _handler.Handle(new CreateDeliveryPhotoUploadUrlCommand(), default);

        result.IsSuccess.Should().BeTrue();
        result.Value!.UploadUrl.Should().Be(uploadUrl);
        result.Value.PhotoKey.Should().NotBeNullOrWhiteSpace();
        result.Value.PhotoKey.Should().EndWith(".jpg");
    }

    [Fact]
    public async Task Handle_GeneratesUniqueKeyPerRequest()
    {
        _fileStorage
            .Setup(x => x.GetPresignedUploadUrlAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<string>.Success("http://localhost:9000/url"));

        var first = await _handler.Handle(new CreateDeliveryPhotoUploadUrlCommand(), default);
        var second = await _handler.Handle(new CreateDeliveryPhotoUploadUrlCommand(), default);

        first.Value!.PhotoKey.Should().NotBe(second.Value!.PhotoKey);
    }

    [Fact]
    public async Task Handle_StorageFails_ReturnsFailure()
    {
        _fileStorage
            .Setup(x => x.GetPresignedUploadUrlAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<string>.Failure("Presigned upload URL failed: connection refused"));

        var result = await _handler.Handle(new CreateDeliveryPhotoUploadUrlCommand(), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Presigned upload URL failed");
    }
}
