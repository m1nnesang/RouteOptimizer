using System.Windows;
using RouteOptimizer.Dispatcher.Wpf.ViewModels;
using RouteOptimizer.Dispatcher.Wpf.Views;

namespace RouteOptimizer.Dispatcher.Wpf;

public partial class MainWindow : Window
{
    private const string MaximizeGlyph = "";
    private const string RestoreGlyph = "";

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
        if (MaximizeButton is not null)
            MaximizeButton.Content = WindowState == WindowState.Maximized ? RestoreGlyph : MaximizeGlyph;
    }

    private void OnLogoutRequested()
    {
        var loginView = _loginViewFactory();
        Application.Current.MainWindow = loginView;
        loginView.Show();
        Close();
    }
}
