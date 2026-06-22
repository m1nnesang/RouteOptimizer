using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace RouteOptimizer.Dispatcher.Wpf.Converters;

/// <summary>
/// Maps a status string to the shared dark-navy chip palette
/// (see DISPATCHER_HANDOFF.md §1). Returns a background or foreground brush.
/// </summary>
public sealed class StatusToChipBrushConverter : IValueConverter
{
    public bool Foreground { get; set; }

    private static readonly Color DeliveredBg = (Color)ColorConverter.ConvertFromString("#15331F");
    private static readonly Color DeliveredFg = (Color)ColorConverter.ConvertFromString("#4AD991");
    private static readonly Color ProgressBg = (Color)ColorConverter.ConvertFromString("#1B2748");
    private static readonly Color ProgressFg = (Color)ColorConverter.ConvertFromString("#7AA3FF");
    private static readonly Color FailedBg = (Color)ColorConverter.ConvertFromString("#3A1F20");
    private static readonly Color FailedFg = (Color)ColorConverter.ConvertFromString("#FF8178");
    private static readonly Color WarnBg = (Color)ColorConverter.ConvertFromString("#352A16");
    private static readonly Color WarnFg = (Color)ColorConverter.ConvertFromString("#F0C269");
    private static readonly Color NeutralBg = (Color)ColorConverter.ConvertFromString("#1E2740");
    private static readonly Color NeutralFg = (Color)ColorConverter.ConvertFromString("#8893AD");

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var (bg, fg) = Resolve(value as string);
        return new SolidColorBrush(Foreground ? fg : bg);
    }

    private static (Color Bg, Color Fg) Resolve(string? status)
    {
        var key = (status ?? string.Empty).Replace(" ", string.Empty).ToLowerInvariant();
        return key switch
        {
            "delivered" or "completed" or "active" or "done" => (DeliveredBg, DeliveredFg),
            "intransit" or "inprogress" or "started" => (ProgressBg, ProgressFg),
            "failed" or "notdelivered" or "cancelled" or "canceled" or "interrupted" => (FailedBg, FailedFg),
            "assignedtoroute" or "assigned" or "skipped" => (WarnBg, WarnFg),
            _ => (NeutralBg, NeutralFg),
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}

/// <summary>
/// One-way equality test used to drive the "active" state of the navigation
/// pill buttons by comparing the bound value to the ConverterParameter.
/// </summary>
public sealed class EqualsConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => string.Equals(value as string, parameter as string, StringComparison.Ordinal);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}
