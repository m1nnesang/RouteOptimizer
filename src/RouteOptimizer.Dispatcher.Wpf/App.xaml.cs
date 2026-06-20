using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RouteOptimizer.Dispatcher.Wpf.Services;
using RouteOptimizer.Dispatcher.Wpf.Services.Interfaces;
using RouteOptimizer.Dispatcher.Wpf.ViewModels;
using RouteOptimizer.Dispatcher.Wpf.Views;
using RouteOptimizer.Dispatcher.Wpf.Views.Dialogs;

namespace RouteOptimizer.Dispatcher.Wpf;


public partial class App : Application
{
    private IServiceProvider? _serviceProvider;

    public static DispatcherSettings Settings { get; private set; } = new();

    protected override void OnStartup(StartupEventArgs e)
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;

        var settings = LoadSettings();
        Settings = settings;

        var services = new ServiceCollection();

        services.AddSingleton(settings);
        services.AddSingleton<TokenStorage>();
        services.AddSingleton<ISessionNotifier, SessionNotifier>();
        services.AddSingleton<IRouteHubService, RouteHubService>();
        services.AddSingleton<IAuthService>(sp =>
        {
            var httpClient = new HttpClient { BaseAddress = new Uri(settings.ApiBaseUrl) };
            return new AuthService(httpClient,
                sp.GetRequiredService<TokenStorage>());
        });

        services.AddSingleton<IDialogService, WpfDialogService>();
        services.AddTransient<LoginViewModel>();
        services.AddTransient<LoginView>(sp => new LoginView(
            sp.GetRequiredService<LoginViewModel>(),
            () => sp.GetRequiredService<MainWindow>()
        ));
        services.AddTransient<Func<LoginView>>(sp => () => sp.GetRequiredService<LoginView>());
        services.AddTransient<MainWindow>();
        services.AddTransient<MainViewModel>();
        services.AddHttpClient<IApiHttpClient, ApiHttpClient>(client =>
            client.BaseAddress = new Uri(settings.ApiBaseUrl));


        _serviceProvider = services.BuildServiceProvider();

        var loginView = _serviceProvider.GetRequiredService<LoginView>();
        loginView.Show();
    }

    private static DispatcherSettings LoadSettings()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables("ROUTEOPTIMIZER_")
            .Build();

        return new DispatcherSettings
        {
            ApiBaseUrl = configuration["Api:BaseUrl"] ?? new DispatcherSettings().ApiBaseUrl,
            OsrmUrl = configuration["Map:OsrmUrl"] ?? new DispatcherSettings().OsrmUrl
        };
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        if (e.Exception is SessionExpiredException)
        {
            e.Handled = true;
            return;
        }

        MessageBox.Show(
            e.Exception.Message,
            "Unexpected error",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        e.Handled = true;
    }
}
