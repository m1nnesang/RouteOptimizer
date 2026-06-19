using System.Windows;
using RouteOptimizer.Dispatcher.Wpf.VIewModels;

namespace RouteOptimizer.Dispatcher.Wpf.Views;

public partial class LoginView : Window
{
    private readonly Func<MainWindow> _mainWindowFactory;
    private void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        if (!((LoginViewModel)DataContext).IsNotLoading) return;
        var vm = (LoginViewModel)DataContext;
        vm.Password = PasswordBox.Password;
        vm.LoginCommand.Execute(null);
    }

    private void OnLoginSucceeded()
    {
        var mainWindow = _mainWindowFactory();
        Application.Current.MainWindow = mainWindow;
        mainWindow.Show();
        Close();
    }

    public LoginView(LoginViewModel model, Func<MainWindow> mainWindowFactory)
    {
        _mainWindowFactory = mainWindowFactory;
        DataContext = model;
        model.LoginSucceeded += OnLoginSucceeded;
        InitializeComponent();
    }
}
