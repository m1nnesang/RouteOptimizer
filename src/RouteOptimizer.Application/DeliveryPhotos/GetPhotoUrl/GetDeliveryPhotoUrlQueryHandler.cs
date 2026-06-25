using MediatR;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Domain.Common;

namespace RouteOptimizer.Application.DeliveryPhotos.GetPhotoUrl;

public class GetDeliveryPhotoUrlQueryHandler : IRequestHandler<GetDeliveryPhotoUrlQuery, Result<string>>
{
    private readonly IFileStorageService _fileStorage;
    private readonly IDeliveryAttemptRepository _attemptRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly ICurrentUser _currentUser;

    public GetDeliveryPhotoUrlQueryHandler(
        IFileStorageService fileStorage,
        IDeliveryAttemptRepository attemptRepository,
        IOrderRepository orderRepository,
        ICurrentUser currentUser)
    {
        _fileStorage = fileStorage;
        _attemptRepository = attemptRepository;
        _orderRepository = orderRepository;
        _currentUser = currentUser;
    }

    public async Task<Result<string>> Handle(GetDeliveryPhotoUrlQuery request, CancellationToken ct)
    {
        var attempt = await _attemptRepository.GetByPhotoKeyAsync(request.PhotoKey, ct);

        if (attempt is null)
            return Result<string>.Failure("Photo not found");

        if (!await IsAccessibleAsync(attempt, ct))
            return Result<string>.Failure("Photo not found");

        return await _fileStorage.GetPresignedUrlAsync(
            DeliveryPhotoStorage.BucketName, request.PhotoKey, DeliveryPhotoStorage.DownloadUrlExpirySeconds, ct);
    }

    private async Task<bool> IsAccessibleAsync(Domain.Entities.DeliveryAttempt attempt, CancellationToken ct)
    {
        if (attempt.DriverId == _currentUser.UserId)
            return true;

        if (_currentUser.WarehouseId is not { } warehouseId)
            return true;

        var order = await _orderRepository.GetByIdAsync(attempt.OrderId, ct);

        return order is not null && order.WarehouseId == warehouseId;
    }
}
