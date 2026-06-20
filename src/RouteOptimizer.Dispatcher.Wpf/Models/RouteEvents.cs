namespace RouteOptimizer.Dispatcher.Wpf.Models;

public class RouteStartedEvent
{
    public Guid RouteId { get; set; }
}

public class StopEvent
{
    public Guid RouteId { get; set; }
    public Guid StopId { get; set; }
    public Guid? NextStopId { get; set; }
}

public class RouteChangedEvent
{
    public Guid RouteId { get; set; }
}

public class DriverLocationEvent
{
    public Guid RouteId { get; set; }
    public Guid DriverId { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}
