namespace RouteOptimizer.Application.Models;

public sealed record StopInput(
    Guid StopId,
    double Latitude,
    double Longitude,
    TimeWindow? TimeWindow
);

public sealed record TimeWindow(
    TimeOnly EarliestArrival,
    TimeOnly LatestArrival
);
