using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Minio;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Infrastructure.Persistence;
using RouteOptimizer.Infrastructure.Persistence.Repositories;
using RouteOptimizer.Infrastructure.Services;
using RouteOptimizer.Infrastructure.Settings;

namespace RouteOptimizer.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IWarehouseRepository, WarehouseRepository>();
        services.AddScoped<IVehicleRepository, VehicleRepository>();
        services.AddScoped<IDriverShiftRepository, DriverShiftRepository>();
        services.AddScoped<IRouteRepository, RouteRepository>();
        services.AddScoped<IUserInvitationRepository, UserInvitationRepository>();
        services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();
        services.AddScoped<IAnalyticsRepository, AnalyticsRepository>();
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<IDeliveryAttemptRepository , DeliveryAttemptRepository>();
        services.AddHttpClient("Nominatim", client =>
        {
            client.BaseAddress = new Uri("https://nominatim.openstreetmap.org/");
            client.DefaultRequestHeaders.UserAgent.ParseAdd("RouteOptimizer/1.0");
        });

        services.Configure<GeocodingSettings>(configuration.GetSection("Geocoding"));
        services.Configure<ClientAppSettings>(configuration.GetSection("ClientApp"));
        services.AddSingleton<IClientUrlBuilder, ClientUrlBuilder>();

        var photonBaseUrl = configuration["Photon:BaseUrl"] ?? "https://photon.komoot.io/";
        services.AddHttpClient("Photon", client =>
        {
            client.BaseAddress = new Uri(photonBaseUrl);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("RouteOptimizer/1.0");
        });
        services.AddScoped<IAddressAutocompleteService, PhotonAddressAutocompleteService>();

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("Redis");
            options.InstanceName = "routeopt:";
        });

        services.AddScoped<IGeocodingService>(sp =>
        {
            var inner = ActivatorUtilities.CreateInstance<NominatimGeocodingService>(sp);
            return new CachedGeocodingService(
                inner,
                sp.GetRequiredService<IDistributedCache>(),
                sp.GetRequiredService<ILogger<CachedGeocodingService>>());
        });
        services.AddScoped<IPasswordHasher, Argon2PasswordHasher>();
        services.Configure<JwtSettings>(configuration.GetSection("Jwt"));
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

        #region minio
        var minioConfig = configuration.GetSection("Minio");
        services.AddSingleton<IMinioClient>(_ => new MinioClient()
            .WithEndpoint(minioConfig["Endpoint"])
            .WithCredentials(minioConfig["AccessKey"], minioConfig["SecretKey"])
            .WithSSL(minioConfig.GetValue<bool>("UseSSL"))
            .Build());

        services.AddScoped<IFileStorageService, MinioFileStorageService>();
        #endregion

        #region emailService
        services.Configure<SmtpSettings>(configuration.GetSection("Smtp"));
        services.AddScoped<IMailService, SmtpMailService>();
        #endregion

        services.AddHttpClient("Osrm", client =>
        {
            client.BaseAddress = new Uri(configuration["Osrm:BaseUrl"]!);
        });
        services.AddScoped<IDistanceMatrixProvider, OsrmDistanceMatrixProvider>();
        services.AddScoped<IRouteGeometryProvider, OsrmRouteGeometryProvider>();

        services.AddHostedService<IdempotencyCleanupService>();
        services.AddHostedService<RefreshTokenCleanupService>();
        services.AddHostedService<UserInvitationCleanupService>();

        return services;
    }
}
