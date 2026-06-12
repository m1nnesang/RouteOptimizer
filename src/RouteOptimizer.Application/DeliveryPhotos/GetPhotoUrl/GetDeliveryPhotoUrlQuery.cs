using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Domain.Common;

namespace RouteOptimizer.Application.DeliveryPhotos.GetPhotoUrl;

public record GetDeliveryPhotoUrlQuery(string PhotoKey) : IQuery<Result<string>>;
