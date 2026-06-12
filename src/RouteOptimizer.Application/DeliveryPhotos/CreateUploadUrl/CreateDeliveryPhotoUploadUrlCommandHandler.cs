using MediatR;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Domain.Common;

namespace RouteOptimizer.Application.DeliveryPhotos.CreateUploadUrl;

public class CreateDeliveryPhotoUploadUrlCommandHandler : IRequestHandler<CreateDeliveryPhotoUploadUrlCommand, Result<DeliveryPhotoUploadDto>>
{
    private readonly IFileStorageService _fileStorage;

    public CreateDeliveryPhotoUploadUrlCommandHandler(IFileStorageService fileStorage) => _fileStorage = fileStorage;

    public async Task<Result<DeliveryPhotoUploadDto>> Handle(CreateDeliveryPhotoUploadUrlCommand request, CancellationToken ct)
    {
        var photoKey = $"{Guid.NewGuid():N}.jpg";

        var urlResult = await _fileStorage.GetPresignedUploadUrlAsync(
            DeliveryPhotoStorage.BucketName, photoKey, DeliveryPhotoStorage.UploadUrlExpirySeconds, ct);

        if (urlResult.IsFailure)
            return Result<DeliveryPhotoUploadDto>.Failure(urlResult.Error!);

        return Result<DeliveryPhotoUploadDto>.Success(new DeliveryPhotoUploadDto(photoKey, urlResult.Value!));
    }
}
