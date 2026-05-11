using System.Transactions;
using RouteOptimizer.Domain.Common;

namespace RouteOptimizer.Domain.ValueObjects;

public class GeoCoordinate : ValueObject
{
    double Latitude { get; }
    double Longitude { get; }
    
    private GeoCoordinate(double latitude, double longitude)
    {
        Latitude = latitude;
        Longitude = longitude;
    }
    
    private static double ToRad(double degrees) => degrees * Math.PI / 180;

    public double DistanceTo(GeoCoordinate other)
    {
        const double r = 6371;
        
        var dLat = ToRad(other.Latitude - Latitude);
        var dLon = ToRad(other.Longitude - Longitude);
        
        var a = Math.Sin(dLat / 2) * Math.Sin((dLat / 2)) +  
                Math.Cos(ToRad(Latitude)) * Math.Cos(ToRad(other.Latitude)) *
            Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return r * c;
    }

    public static Result<GeoCoordinate> Create(double latitude, double longitude)
    {
        if (latitude < -90 || latitude > 90 || longitude < -180 || longitude > 180)
            return Result<GeoCoordinate>.Failure("Invalid coordinates");
        
        return Result<GeoCoordinate>.Success(new GeoCoordinate(latitude, longitude));
    }
    
    
    
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Latitude;
        yield return Longitude;
    }
}