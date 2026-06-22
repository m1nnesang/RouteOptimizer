namespace RouteOptimizer.Infrastructure.Settings;

public sealed class GeocodingSettings
{
    public string CountryCodes { get; set; } = "pl";
    public double BiasLatitude { get; set; } = 52.0;
    public double BiasLongitude { get; set; } = 19.0;
}
