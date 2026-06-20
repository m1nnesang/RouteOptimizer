using System.Windows;
using RouteOptimizer.Dispatcher.Wpf.ViewModels;
using RouteOptimizer.Dispatcher.Wpf.Views;

namespace RouteOptimizer.Dispatcher.Wpf;

public partial class MainWindow : Window
{
    private readonly Func<LoginView> _loginViewFactory;

    public MainWindow(MainViewModel viewModel, Func<LoginView> loginViewFactory)
    {
        InitializeComponent();
        DataContext = viewModel;
        _loginViewFactory = loginViewFactory;
        viewModel.LogoutRequested += OnLogoutRequested;
    }

    private void OnLogoutRequested()
    {
        var loginView = _loginViewFactory();
        Application.Current.MainWindow = loginView;
        loginView.Show();
        Close();
    }
}
