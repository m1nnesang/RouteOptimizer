using CommunityToolkit.Mvvm.ComponentModel;

namespace RouteOptimizer.Dispatcher.Wpf.Models;

public partial class DeliveryAttemptItem : ObservableObject
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid DriverId { get; set; }
    public DateTime AttemptedAt { get; set; }
    public string Outcome { get; set; } = string.Empty;
    public string? FailureReason { get; set; }
    public string? Notes { get; set; }
    public string? PhotoKey { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }

    [ObservableProperty]
    public partial string? PhotoUrl { get; set; }

    public bool HasPhoto => !string.IsNullOrWhiteSpace(PhotoKey);

    public string AttemptedAtDisplay => AttemptedAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm");

    public string OutcomeDisplay => Outcome;

    public bool IsFailed => Outcome == "Failed";

    public string FailureReasonDisplay => string.IsNullOrWhiteSpace(FailureReason) ? "—" : FailureReason;

    public string NotesDisplay => string.IsNullOrWhiteSpace(Notes) ? "—" : Notes!;
}
