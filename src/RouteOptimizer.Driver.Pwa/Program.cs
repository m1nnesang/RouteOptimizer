using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using RouteOptimizer.Driver.Pwa;
using RouteOptimizer.Driver.Pwa.Auth;
using RouteOptimizer.Driver.Pwa.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBaseUrl = builder.Configuration["Api:BaseUrl"];
if (string.IsNullOrWhiteSpace(apiBaseUrl))
    apiBaseUrl = builder.HostEnvironment.BaseAddress;

builder.Services.AddBlazoredLocalStorage();
builder.Services.AddAuthorizationCore();

builder.Services.AddScoped<ITokenStore, LocalStorageTokenStore>();

builder.Services.AddHttpClient("ApiAuth", client => client.BaseAddress = new Uri(apiBaseUrl));
builder.Services.AddScoped<IAuthService>(sp => new AuthService(
    sp.GetRequiredService<IHttpClientFactory>().CreateClient("ApiAuth"),
    sp.GetRequiredService<ITokenStore>()));

builder.Services.AddScoped<JwtAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(
    sp => sp.GetRequiredService<JwtAuthenticationStateProvider>());

builder.Services.AddTransient<JwtAuthorizationMessageHandler>();
builder.Services.AddHttpClient("Api", client => client.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<JwtAuthorizationMessageHandler>();
builder.Services.AddScoped(
    sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("Api"));

builder.Services.AddHttpClient("Upload");

builder.Services.AddScoped<IRouteApi, RouteApiClient>();
builder.Services.AddScoped<IGeolocation, GeolocationService>();

await builder.Build().RunAsync();
