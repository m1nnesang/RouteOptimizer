using MediatR;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Domain.Common;

namespace RouteOptimizer.Application.DeliveryPhotos.GetPhotoUrl;

public class GetDeliveryPhotoUrlQueryHandler : IRequestHandler<GetDeliveryPhotoUrlQuery, Result<string>>
{
    private readonly IFileStorageService _fileStorage;

    public GetDeliveryPhotoUrlQueryHandler(IFileStorageService fileStorage) => _fileStorage = fileStorage;

    public async Task<Result<string>> Handle(GetDeliveryPhotoUrlQuery request, CancellationToken ct) =>
        await _fileStorage.GetPresignedUrlAsync(
            DeliveryPhotoStorage.BucketName, request.PhotoKey, DeliveryPhotoStorage.DownloadUrlExpirySeconds, ct);
}
