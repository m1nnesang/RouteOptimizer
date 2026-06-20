using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using RouteOptimizer.Dispatcher.Wpf.Models;

namespace RouteOptimizer.Dispatcher.Wpf.Views;

public partial class DeliveryHistoryPanel : UserControl
{
    public DeliveryHistoryPanel()
    {
        InitializeComponent();
    }

    private void Photo_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: DeliveryAttemptItem { PhotoUrl: { } url } }
            || string.IsNullOrWhiteSpace(url))
            return;

        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception)
        {
        }
    }
}
