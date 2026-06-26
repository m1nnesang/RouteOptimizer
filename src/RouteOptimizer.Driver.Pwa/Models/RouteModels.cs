namespace RouteOptimizer.Driver.Pwa.Models;

public sealed record RouteListItem(
    Guid Id,
    Guid WarehouseId,
    string Status,
    int StopsCount,
    Guid? AssignedShiftId,
    DateOnly? Date,
    string? DriverName,
    string WarehouseName);

public sealed record RouteDetail(
    Guid Id,
    Guid WarehouseId,
    Guid? AssignedShiftId,
    string Status,
    IReadOnlyList<RouteStop> Stops,
    IReadOnlyList<GeoPoint>? Geometry = null,
    GeoPoint? Warehouse = null);

public sealed record GeoPoint(double Latitude, double Longitude);

public sealed record RouteStop(
    Guid Id,
    int Sequence,
    string City,
    string Street,
    double Latitude,
    double Longitude,
    string Status,
    IReadOnlyList<Guid> OrderIds,
    IReadOnlyList<StopOrder> Orders);

public sealed record StopOrder(
    Guid OrderId,
    string Recipient,
    string? Apartment,
    string Type,
    string Phone,
    string Status);

public sealed record CompleteStopRequest(bool IsPartial);

public sealed record PushLocationRequest(double Latitude, double Longitude);

public sealed record FailDeliveryRequest(
    double Latitude,
    double Longitude,
    string FailureReason,
    string? Notes,
    string? PhotoKey);

public sealed record DeliveryPhotoUpload(string PhotoKey, string UploadUrl);

public sealed record ApiResult(bool Success, string? Error, int? StatusCode = null)
{
    public static readonly ApiResult Ok = new(true, null, 200);

    public static ApiResult Fail(string? error, int? statusCode = null) => new(false, error, statusCode);
}
