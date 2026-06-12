using System.Net.Http;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using RouteOptimizer.Dispatcher.Wpf.Services;
using RouteOptimizer.Dispatcher.Wpf.Services.Interfaces;
using RouteOptimizer.Dispatcher.Wpf.VIewModels;
using RouteOptimizer.Dispatcher.Wpf.Views;

namespace RouteOptimizer.Dispatcher.Wpf;


public partial class App : Application
{
    private IServiceProvider? _serviceProvider;

    protected override void OnStartup(StartupEventArgs e)
    {
        var services = new ServiceCollection();

        services.AddSingleton<TokenStorage>();
        services.AddSingleton<IRouteHubService, RouteHubService>();
        services.AddSingleton<IAuthService>(sp =>
        {
            var httpClient = new HttpClient { BaseAddress = new
                Uri("http://localhost:8080") };
            return new AuthService(httpClient,
                sp.GetRequiredService<TokenStorage>());
        });

        services.AddTransient<LoginViewModel>();
        services.AddTransient<LoginView>(sp => new LoginView(
            sp.GetRequiredService<LoginViewModel>(),
            () => sp.GetRequiredService<MainWindow>()
        ));
        services.AddTransient<MainWindow>();
        services.AddTransient<MainViewModel>();
        services.AddHttpClient<IApiHttpClient, ApiHttpClient>(client =>
            client.BaseAddress = new Uri("http://localhost:8080"));


        _serviceProvider = services.BuildServiceProvider();

        var loginView = _serviceProvider.GetRequiredService<LoginView>();
        loginView.Show();
    }
}
