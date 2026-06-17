using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Minio;
using Moq;
using Npgsql;
using Respawn;
using Respawn.Graph;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Application.Models;
using RouteOptimizer.Domain.Common;
using RouteOptimizer.Domain.ValueObjects;
using Testcontainers.PostgreSql;

namespace RouteOptimizer.Integration.Tests;

public sealed class IntegrationTestFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    private static readonly Dictionary<string, string?> StaticConfig = new()
    {
        ["Jwt__SecretKey"] = "integration-test-secret-key-which-is-long-enough",
        ["Jwt__Issuer"] = "test-issuer",
        ["Jwt__Audience"] = "test-audience",
        ["Smtp__Host"] = "localhost",
        ["Osrm__BaseUrl"] = "http://localhost",
        ["Minio__Endpoint"] = "localhost:9000",
        ["Minio__AccessKey"] = "test",
        ["Minio__SecretKey"] = "test"
    };

    private WebApplicationFactory<Program> _factory = null!;
    private Respawner _respawner = null!;

    public IServiceProvider Services => _factory.Services;

    public HttpClient CreateClient() => _factory.CreateClient();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        foreach (var (key, value) in StaticConfig)
            Environment.SetEnvironmentVariable(key, value);

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");

                builder.ConfigureAppConfiguration((_, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:DefaultConnection"] = _postgres.GetConnectionString()
                    });
                });

                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<IMinioClient>();
                    services.AddSingleton(Mock.Of<IMinioClient>());

                    services.RemoveAll<IMailService>();
                    services.AddScoped<IMailService, FakeMailService>();

                    services.RemoveAll<IDistanceMatrixProvider>();
                    services.AddScoped<IDistanceMatrixProvider, FakeDistanceMatrixProvider>();

                    services.RemoveAll<IGeocodingService>();
                    services.AddScoped<IGeocodingService, FakeGeocodingService>();

                    services.RemoveAll<IFileStorageService>();
                    services.AddScoped<IFileStorageService, FakeFileStorageService>();
                });
            });

        _ = _factory.CreateClient();

        await using var connection = new NpgsqlConnection(_postgres.GetConnectionString());
        await connection.OpenAsync();
        _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = new[] { "public" },
            TablesToIgnore = new Table[] { "__EFMigrationsHistory" }
        });
    }

    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    public async Task ResetDatabaseAsync()
    {
        await using var connection = new NpgsqlConnection(_postgres.GetConnectionString());
        await connection.OpenAsync();
        await _respawner.ResetAsync(connection);
    }

    private sealed class FakeMailService : IMailService
    {
        public Task<Result> SendAsync(MailMessage message, CancellationToken ct = default) =>
            Task.FromResult(Result.Success());
    }

    private sealed class FakeDistanceMatrixProvider : IDistanceMatrixProvider
    {
        public Task<DistanceMatrix> GetMatrixAsync(
            (double Lat, double Lon) warehouse,
            IReadOnlyList<(double Lat, double Lon)> input,
            CancellationToken ct = default)
        {
            var size = input.Count + 1;
            return Task.FromResult(new DistanceMatrix(new double[size, size]));
        }
    }

    private sealed class FakeGeocodingService : IGeocodingService
    {
        public Task<Result<GeoCoordinate>> GeocodeAsync(Address address, CancellationToken ct) =>
            Task.FromResult(GeoCoordinate.Create(52.5200, 13.4050));
    }

    private sealed class FakeFileStorageService : IFileStorageService
    {
        public Task<Result<string>> UploadAsync(string bucketName, string objectName, Stream data, string contentType, CancellationToken ct) =>
            Task.FromResult(Result<string>.Success(objectName));

        public Task<Result<string>> GetPresignedUrlAsync(string bucketName, string objectName, int expirySeconds, CancellationToken ct) =>
            Task.FromResult(Result<string>.Success($"https://fake-storage/{bucketName}/{objectName}"));

        public Task<Result<string>> GetPresignedUploadUrlAsync(string bucketName, string objectName, int expirySeconds, CancellationToken ct) =>
            Task.FromResult(Result<string>.Success($"https://fake-storage/upload/{objectName}"));

        public Task<Result> DeleteAsync(string bucketName, string objectName, CancellationToken ct) =>
            Task.FromResult(Result.Success());
    }
}

[CollectionDefinition(Name)]
public sealed class IntegrationCollection : ICollectionFixture<IntegrationTestFixture>
{
    public const string Name = "Integration";
}
