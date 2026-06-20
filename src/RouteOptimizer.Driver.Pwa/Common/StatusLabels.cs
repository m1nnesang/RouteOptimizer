namespace RouteOptimizer.Driver.Pwa.Common;

public static class StatusLabels
{
    public static string Route(string status) => status switch
    {
        "Draft" => "Черновик",
        "Optimized" => "Оптимизирован",
        "Assigned" => "Назначен",
        "InProgress" => "В пути",
        "Completed" => "Завершён",
        "Interrupted" => "Прерван",
        "Cancelled" => "Отменён",
        _ => status
    };

    public static string Stop(string status) => status switch
    {
        "Pending" => "Ожидает",
        "InProgress" => "В процессе",
        "Completed" => "Доставлено",
        "PartiallyCompleted" => "Частично",
        "Skipped" => "Пропущено",
        "Failed" => "Неудача",
        _ => status
    };

    public static string Order(string status) => status switch
    {
        "Created" => "Создан",
        "AssignedToRoute" => "Назначен",
        "InTransit" => "В пути",
        "Delivered" => "Доставлен",
        "Failed" => "Не доставлен",
        "Cancelled" => "Отменён",
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
}
