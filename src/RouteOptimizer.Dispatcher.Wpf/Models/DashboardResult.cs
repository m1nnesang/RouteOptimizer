using System;
using System.Collections.Generic;

namespace RouteOptimizer.Dispatcher.Wpf.Models;

public class DashboardResult
{
    public DateOnly Date { get; set; }
    public StopStatusBreakdown Stops { get; set; } = new();
    public List<FailureReasonCount> FailureBreakdown { get; set; } = [];
    public List<DriverPerformance> DriverPerformance { get; set; } = [];
}

public class StopStatusBreakdown
{
    public int Total { get; set; }
    public int Pending { get; set; }
    public int InProgress { get; set; }
    public int Completed { get; set; }
    public int PartiallyCompleted { get; set; }
    public int Skipped { get; set; }
    public int Failed { get; set; }
}

public class FailureReasonCount
{
    public string Reason { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class DriverPerformance
{
    public Guid DriverId { get; set; }
    public string DriverName { get; set; } = string.Empty;
    public int Delivered { get; set; }
    public int Failed { get; set; }
}
