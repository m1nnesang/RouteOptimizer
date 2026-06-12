using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Domain.Common;

namespace RouteOptimizer.Application.DeliveryPhotos.CreateUploadUrl;

public record CreateDeliveryPhotoUploadUrlCommand : ICommand<Result<DeliveryPhotoUploadDto>>;
