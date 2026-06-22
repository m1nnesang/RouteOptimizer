namespace RouteOptimizer.Driver.Pwa.Common;

public static class StatusLabels
{
    public static string Route(string status) => status switch
    {
        "Draft" => "Draft",
        "Optimized" => "Optimized",
        "Assigned" => "Assigned",
        "InProgress" => "In transit",
        "Completed" => "Completed",
        "Interrupted" => "Interrupted",
        "Cancelled" => "Cancelled",
        _ => status
    };

    public static string Stop(string status) => status switch
    {
        "Pending" => "Pending",
        "InProgress" => "In progress",
        "Completed" => "Delivered",
        "PartiallyCompleted" => "Partial",
        "Skipped" => "Skipped",
        "Failed" => "Failed",
        _ => status
    };

    public static string Order(string status) => status switch
    {
        "Created" => "Created",
        "AssignedToRoute" => "Assigned",
        "InTransit" => "In transit",
        "Delivered" => "Delivered",
        "Failed" => "Not delivered",
        "Cancelled" => "Cancelled",
        _ => status
    };

    public static string OrderCss(string status) => status switch
    {
        "Delivered" => "is-done",
        "Failed" => "is-failed",
        "Cancelled" => "is-skipped",
        _ => "is-active"
    };

    public static string Css(string status) => status switch
    {
        "Completed" => "is-done",
        "InProgress" => "is-active",
        "Failed" => "is-failed",
        "Skipped" => "is-skipped",
        "PartiallyCompleted" => "is-partial",
        _ => "is-pending"
    };

    public static string PinCss(string status) => status switch
    {
        "Completed" => "pin-done",
        "InProgress" => "pin-current",
        "Failed" => "pin-failed",
        "Skipped" => "pin-skipped",
        "PartiallyCompleted" => "pin-skipped",
        _ => "pin-upcoming"
    };
}
