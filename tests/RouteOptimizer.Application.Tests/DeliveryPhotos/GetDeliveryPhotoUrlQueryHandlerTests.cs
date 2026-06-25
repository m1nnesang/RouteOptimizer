using FluentAssertions;
using Moq;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Application.DeliveryPhotos;
using RouteOptimizer.Application.DeliveryPhotos.GetPhotoUrl;
using RouteOptimizer.Domain.Common;
using RouteOptimizer.Domain.Entities;
using RouteOptimizer.Domain.Enums;
using RouteOptimizer.Domain.ValueObjects;

namespace RouteOptimizer.Application.Tests.DeliveryPhotos;

public class GetDeliveryPhotoUrlQueryHandlerTests
{
    private static readonly Guid DriverId = Guid.NewGuid();

    private readonly Mock<IFileStorageService> _fileStorage = new();
    private readonly Mock<IDeliveryAttemptRepository> _attemptRepository = new();
    private readonly Mock<IOrderRepository> _orderRepository = new();
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly GetDeliveryPhotoUrlQueryHandler _handler;

    public GetDeliveryPhotoUrlQueryHandlerTests()
    {
        _currentUser.SetupGet(x => x.UserId).Returns(DriverId);
        _handler = new GetDeliveryPhotoUrlQueryHandler(
            _fileStorage.Object, _attemptRepository.Object, _orderRepository.Object, _currentUser.Object);
    }

    [Fact]
    public async Task Handle_OwnAttemptAndStorageSucceeds_ReturnsPresignedUrl()
    {
        const string photoKey = "abc123.jpg";
        const string url = "http://localhost:9000/delivery-photos/abc123.jpg";
        SetupAttempt(photoKey, DriverId);
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
    public async Task Handle_PhotoNotFound_ReturnsFailure()
    {
        _attemptRepository
            .Setup(x => x.GetByPhotoKeyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DeliveryAttempt?)null);

        var result = await _handler.Handle(new GetDeliveryPhotoUrlQuery("missing.jpg"), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Photo not found");
    }

    [Fact]
    public async Task Handle_AttemptOfAnotherDriverInAnotherWarehouse_ReturnsFailure()
    {
        const string photoKey = "secret.jpg";
        var attempt = SetupAttempt(photoKey, Guid.NewGuid());
        _currentUser.SetupGet(x => x.WarehouseId).Returns(Guid.NewGuid());
        _orderRepository
            .Setup(x => x.GetByIdAsync(attempt.OrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Domain.Entities.Orders.Order?)null);

        var result = await _handler.Handle(new GetDeliveryPhotoUrlQuery(photoKey), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Photo not found");
        _fileStorage.Verify(x => x.GetPresignedUrlAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_StorageFails_ReturnsFailure()
    {
        const string photoKey = "abc123.jpg";
        SetupAttempt(photoKey, DriverId);
        _fileStorage
            .Setup(x => x.GetPresignedUrlAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<string>.Failure("Presigned URL failed: object not found"));

        var result = await _handler.Handle(new GetDeliveryPhotoUrlQuery(photoKey), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Presigned URL failed");
    }

    private DeliveryAttempt SetupAttempt(string photoKey, Guid driverId)
    {
        var location = GeoCoordinate.Create(52.2297, 21.0122).Value!;
        var attempt = DeliveryAttempt.Create(
            Guid.NewGuid(), driverId, Guid.NewGuid(), location,
            DeliveryOutcome.Failed, FailureReason.CustomerNotAtHome, null, photoKey).Value!;

        _attemptRepository
            .Setup(x => x.GetByPhotoKeyAsync(photoKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(attempt);

        return attempt;
    }
}
