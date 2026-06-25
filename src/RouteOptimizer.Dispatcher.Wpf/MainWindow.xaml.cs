using System.Windows;
using System.Windows.Media;
using RouteOptimizer.Dispatcher.Wpf.ViewModels;
using RouteOptimizer.Dispatcher.Wpf.Views;

namespace RouteOptimizer.Dispatcher.Wpf;

public partial class MainWindow : Window
{
    private const string MaximizeIcon = "M0.5,0.5 H9.5 V9.5 H0.5 Z";
    private const string RestoreIcon = "M0.5,2.5 H7.5 V9.5 H0.5 Z M2.5,2.5 V0.5 H9.5 V7.5 H7.5";

    private readonly Func<LoginView> _loginViewFactory;

    public MainWindow(MainViewModel viewModel, Func<LoginView> loginViewFactory)
    {
        InitializeComponent();
        DataContext = viewModel;
        _loginViewFactory = loginViewFactory;
        viewModel.LogoutRequested += OnLogoutRequested;
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e) =>
        WindowState = WindowState.Minimized;

    private void MaximizeButton_Click(object sender, RoutedEventArgs e) =>
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

    private void Window_StateChanged(object? sender, EventArgs e)
    {
        // Compensate for the chrome clipping that WindowStyle=None introduces when maximized.
        RootBorder.Padding = WindowState == WindowState.Maximized
            ? new Thickness(7)
            : new Thickness(0);
        if (MaxIcon is not null)
            MaxIcon.Data = Geometry.Parse(
                WindowState == WindowState.Maximized ? RestoreIcon : MaximizeIcon);
    }

    private void OnLogoutRequested()
    {
        var loginView = _loginViewFactory();
        Application.Current.MainWindow = loginView;
        loginView.Show();
        Close();
    }
}
