namespace RouteOptimizer.Driver.Pwa.Common;

public static class FailureReasons
{
    public static readonly IReadOnlyList<(string Value, string Label)> All =
    [
        ("IncorrectAddress", "Неверный адрес"),
        ("FirmClosed", "Фирма закрыта"),
        ("CustomerNotAtHome", "Клиента нет дома"),
        ("CustomerRefuseDelivery", "Клиент отказался"),
        ("VehicleProblem", "Проблема с машиной"),
        ("Other", "Другое"),
    ];
}
