using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<IDeliveryAttemptRepository , DeliveryAttemptRepository>();
        services.AddHttpClient("Nominatim", client =>
        {
            client.BaseAddress = new Uri("https://nominatim.openstreetmap.org/");
            client.DefaultRequestHeaders.UserAgent.ParseAdd("RouteOptimizer/1.0");
        });

        services.AddScoped<IGeocodingService, NominatimGeocodingService>();
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

        services.AddHostedService<IdempotencyCleanupService>();
        services.AddHostedService<RefreshTokenCleanupService>();

        return services;
    }
}
