using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using RouteOptimizer.Dispatcher.Wpf.VIewModels;

namespace RouteOptimizer.Dispatcher.Wpf;


public partial class App : Application
{
    private IServiceProvider? _serviceProvider;

    protected override void OnStartup(StartupEventArgs e)
    {
        var services = new ServiceCollection();

        services.AddHttpClient("Api", client => client.BaseAddress = new Uri("http://localhost:8080"));

        services.AddTransient<MainWindow>();
        services.AddTransient<MainViewModel>();

        _serviceProvider = services.BuildServiceProvider();

        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }
}
