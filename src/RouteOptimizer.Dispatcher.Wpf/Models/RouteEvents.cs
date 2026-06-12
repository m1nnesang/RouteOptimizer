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
